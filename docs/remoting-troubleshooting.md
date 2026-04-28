# Remoting Troubleshooting

> **Doc status**: draft living in the `Spe` repo. Will be migrated into the
> public book (https://doc.sitecorepowershell.com/, source at
> `SitecorePowerShell/Book`) once that book page exists. Keep this file in
> sync with the code until then; delete it after the book page lands.

This page explains how to diagnose failed remoting requests: 401 (auth),
403 (policy), 424 (script threw), 429 (throttled), 503 (cold start). It is
ordered by what helps most: start at "Operator-side first checks," then jump
to the section that matches your status code.

## Where to look

All audit events are written to the standard Sitecore log
(`$(dataFolder)/logs/log.*.txt`) by the CM that handles the request. Each line
tagged `[Remoting]` carries structured `key=value` fields. The important ones:

- `action=` identifies what happened at the handler level (see the reference
  table below).
- `rid=` is a per-request id. Use it to correlate audit, debug, and error
  lines for the same request.
- `ip=` is the caller's remote address after the handler's proxy-header
  resolution.

Most troubleshooting fields only appear at `DEBUG` level. If you only see the
audit lines, raise the `PowerShellLog` log level (see "Enabling debug logs"
below) and retry.

## Operator-side first checks

Before reading server logs, four checks resolve a large fraction of
remoting failures without the round trip.

### Decode the token client-side

The SPE module ships two diagnostic cmdlets that base64url-decode a JWT
locally. They do not validate signatures; they just show what the issuer
actually put in the token, which is what you need to compare against your
server config:

```powershell
$token | ConvertFrom-JwtHeader   # alg, typ, kid
$token | ConvertFrom-JwtPayload  # iss, aud, exp, scope, client_id, ...
```

Compare those values to:

- `<allowedIssuers>` and `<allowedAudiences>` in `Spe.config` /
  `Spe.OAuthBearer.config`
- `<requiredScopes>` in the matching `<oauthBearer>` block
- `<allowedAlgorithms>` (overridden values must still include the alg the
  IdP is signing with)
- The `Allowed Issuer` / `OAuth Client Ids` fields on the OAuth Client item
  in Sitecore

The vast majority of 401s come from a mismatch on this list. The token tells
you which one.

### Check startup logs for config validator warnings

`AuthProviderConfigValidator` runs once at app start and emits Warn-level
lines for misconfigurations that route deterministically but probably were
not intentional:

```
[AuthConfig] action=startupWarning detail=<warning text>
```

Three warnings can fire:

- **OAuth overlap** - two `<oauthBearer>` providers share an issuer.
- **Cross-provider issuer leak** - the same issuer is declared on both an
  OAuth Bearer provider and the SharedSecret `<allowedIssuers>` list.
- **Scopes without audiences** - an OAuth Bearer provider has
  `<requiredScopes>` but no `<allowedAudiences>`, so tokens carry scopes
  for a resource SPE never claims as its own.

Search the CM log for `[AuthConfig]` after the most recent app pool recycle.
A clean startup logs nothing here; presence of any line is the diagnosis.

### Verify the OAuth Client item state

For OAuth Bearer requests, the token can validate cryptographically but
still be rejected because the matching client item is in a bad state.
Open `/sitecore/system/Modules/PowerShell/Settings/Access/Remoting Clients`
and confirm:

- **Enabled** is checked.
- **Expires** is in the future (or empty).
- **Allowed Issuer** matches the token's `iss` exactly, including protocol
  and trailing-slash behavior.
- **OAuth Client Ids** contains the token's `client_id` (one entry per line
  if multiple).
- **Impersonated User** is populated and the user exists.

A change to any of these is picked up immediately via `OnItemSaved`; no
config reload required.

### Verify the impersonated user is allowed by the service

SPE has two authorization layers and they are independent. A token can pass
the authentication layer (signature, claims, client lookup all valid) and
still be refused at the service layer because the resolved Sitecore user
is not on the service's allowlist.

The **authentication layer** resolves the token to a Sitecore user via one
of three paths:

| Auth path | Where the user comes from |
| --- | --- |
| OAuth Bearer | The `Impersonated User` field on the OAuth Client item matched by `(iss, client_id)`. |
| Shared Secret Client (per-item) | The `Impersonated User` field on the Shared Secret Client item matched by `kid`. |
| Config-level shared secret | The token's `name` claim, used directly as a Sitecore username. |

The **service authorization layer** is the `<authorization>` block under
`<powershell><services><serviceName>` in `Spe.config`. The resolved user
must satisfy at least one rule in that block, either:

- listed directly as `<add Permission="Allow" IdentityType="User" Identity="<user>" />`, or
- a member of a role listed as `<add Permission="Allow" IdentityType="Role" Identity="<role>" />`.

If the user is neither, every call returns 401 with
`action=userUnauthorized` regardless of how clean the token is.

Quick mental check: read the user off the Remoting Client item (or the
`name` claim for the config path), then read `<authorization>` for the
service you are calling, and confirm one of those rules matches the user
either directly or via role membership. The "Authenticated user is
unauthorized" section below covers the worked example and the symmetric
fixes.

## Response header: `X-SPE-AuthFailureReason`

Before digging into server logs, check the response. When bearer validation
fails, the handler sets the `X-SPE-AuthFailureReason` response header to one
of:

| Value | Meaning |
| --- | --- |
| `expired` | Token's `exp` claim is in the past (beyond clock skew). |
| `disabled` | Matched a Remoting Client item but the item is disabled. |
| `replay` | Token already used (its `jti` was seen earlier within its lifetime). Only emitted when `<jtiReplayCacheEnabled>` is on. |
| `invalid` | Signature, issuer, audience, scope, or client lookup failed. |

The header is only set when the handler has enough information to classify the
failure. For unclassified failures (unrecognised alg, no provider configured,
etc.) the header is absent and only the server log has the reason.

## Response header: `WWW-Authenticate`

Bearer-rejection 401s also carry an RFC 6750 `WWW-Authenticate` header so
standard OAuth client libraries can react automatically (refresh, prompt,
abort). Mapping from internal reason to header value:

| Internal reason | `WWW-Authenticate` value |
| --- | --- |
| `expired` | `Bearer error="invalid_token", error_description="The access token expired"` |
| `replay` | `Bearer error="invalid_token", error_description="The access token has already been used"` |
| missing scope | `Bearer error="insufficient_scope"` |
| `invalid`, `disabled`, anything else | `Bearer error="invalid_token"` |

`disabled` collapses to bare `invalid_token` deliberately so the response does
not leak that a client item exists but is currently disabled.

## Decision tree: "My request returns 401"

1. **No `Authorization` header was sent** -> server logs
   `action=authRequired`. The service requires auth and the client did not
   provide any. Confirm the client is actually attaching the bearer header.
2. **Bearer token was sent but rejected** -> server logs end with
   `action=authRejected`. Read upward in the same `rid=` to find the more
   specific `action=bearerProviderMissing`, `action=bearerAuthFailed`,
   `action=clientSignatureFailed`, `action=oauthValidationFailed`, etc.
3. **Authenticated but service refused** -> server logs
   `action=userUnauthorized`. Auth succeeded, but the resolved user is not in
   the service's role list under `<powershell><services>` in `Spe.config`.
   This is a permissions problem, not an auth problem.
4. **Client item matched but had no impersonated user** -> server logs
   `action=clientDenied reason=noImpersonateUser`. The OAuth Client (or
   Shared Secret Client) item resolved correctly but its Impersonated User
   field is blank. Populate it on the item; no config reload required.
5. **Client item matched but had no policy assigned** -> server logs
   `action=clientDenied reason=noPolicyAssigned`. Set the item's Remoting
   Policy field to a valid policy under
   `/sitecore/system/Modules/PowerShell/Settings/Access/Policies`.
6. **Session-ownership rejection (raw HTTP callers)** -> server logs
   `action=sessionOwnershipMismatch`, response is 403 with
   `X-SPE-Restriction: session-not-owned`. Two callers with different
   identities tried to use the same `sessionId`. Generate a fresh
   `sessionId` per call when `persistentSession=false`, or do not share
   `sessionId` values across credentials when `persistentSession=true`.
7. **Script blocked by policy** -> server logs
   `action=scriptRejectedByPolicy`, response is 403 with
   `X-SPE-Restriction: policy-blocked`. See "Policy denial: 403" below.
8. **Too many requests** -> server logs `action=throttled`, response is 429
   (not 401). See "Throttling: 429" below.

## Audit action reference

Audit lines show the shape of the failure. Debug lines (see below) show the
reason. Lookup your audit action in the left column, then look for the
associated debug actions that carry the detail.

| Audit action | What it means | Likely cause or next step |
| --- | --- | --- |
| `requestReceived` | Handler accepted the request for processing. | Informational only. |
| `authRequired` | No credentials and service requires them. | Attach a bearer token, Basic auth, or query-string credentials. |
| `bearerAuthFailed authKind=none` | Bearer header present, but no provider matched the token's `alg`. | Inspect debug lines `bearerProviderMissing` or `unrecognizedAlg`. |
| `bearerAuthFailed authKind=sharedSecretClient` | Shared Secret Client (per-item kid lookup) rejected the token. | Inspect `clientNotFound`, `clientSignatureFailed`. Confirm the client item exists and is enabled. |
| `bearerAuthFailed authKind=configSecret` | Config-level shared-secret validation failed. | Token is not signed with the `<sharedSecret>` in `Spe.config`. |
| `bearerAuthFailed authKind=oauthClient` | OAuth provider accepted alg but rejected claims or client. | Inspect `oauthValidationFailed`, `clientNotFound`. |
| `bearerAuthError` | Exception thrown during validation. | See the adjacent `error=` field and any stack trace at `ERROR` level. |
| `clientDenied reason=noImpersonateUser` | Matched client item has no Impersonated User set. | Populate the Impersonated User field on the client item. |
| `throttled` | Request exceeded the client's rate limit. | Respect `Retry-After`; adjust the policy if limits are wrong. |
| `throttleBypassed` | Client's policy is configured to bypass the limit. | Informational, audited so bypasses are visible. |
| `authRejected` | Terminal 401. Always preceded by a more specific audit or debug line. | Ignore this line in isolation. Look upward by `rid=`. |
| `userUnauthorized` | Authenticated user lacks permission for the service. | Add the user's role to `<powershell><services><serviceName><authorization>` in `Spe.config`. |
| `coldStart` | The Remoting Client registry is still warming. Response is 503 with `Retry-After: 2`. | Retry. Persistent cold starts indicate registry load failure; check the CM event log. |

## Debug reason codes

These lines only appear when the `PowerShellLog` level is `DEBUG`. They sit
between `requestReceived` and the final `bearerAuthFailed` in the same
`rid=`.

| Debug action | Meaning |
| --- | --- |
| `requestUrl` | Full request URL (sanitised of secrets). Use this to confirm the right endpoint and query-string parameters are being hit. |
| `unrecognizedAlg` | JWT header has no `alg`, or an alg the handler doesn't dispatch (e.g. `none`). |
| `bearerProviderMissing alg=X` | Token's `alg` is recognised, but no provider is configured that accepts it. See "OAuth provider isn't loading" below. |
| `clientNotFound` | Bearer token's key id (`kid`) or `(issuer, client_id)` pair did not match any Remoting Client item. |
| `clientSignatureFailed` | Token matched a client item but the HMAC signature did not verify against the item's shared secret. |
| `oauthValidationFailed` | OAuth provider threw a `SecurityException` during validation. Cause varies: bad signature, wrong issuer, wrong audience, expired, missing required scope, JWKS unreachable. Look for the specific reason in the preceding exception log. |
| `configSecretFailed` | Config-level shared-secret validation threw. |
| `bearerAuthSuccess` | Auth succeeded. Shown here because it also only emits at DEBUG, not AUDIT, in the default config. |

## "My OAuth provider isn't loading"

Symptom: debug log shows `bearerProviderMissing alg=RS256` (or other `RS*` /
`ES*` alg) even though the `<oauthBearer>` element is visible in your config.

Check these in order:

1. **The config that registers the provider type is enabled.**
   `App_Config/Include/Spe/Spe.OAuthBearer.config.example` must be copied
   to `Spe.OAuthBearer.config` (drop the `.example` suffix). This file
   supplies the `type=` attribute on
   the `<oauthBearer>` element. Patch files that only set child fields (such
   as `z.Spe.Development.OAuth.config`) assume the parent already exists with
   a `type`; they do not register the provider on their own.

2. **The merged element has a `type=` attribute.** Browse
   `/sitecore/admin/showconfig.aspx` and search for
   `authenticationProviders`. The merged `<oauthBearer>` element must look
   like:
   ```xml
   <oauthBearer type="Spe.Core.Settings.Authorization.OAuthBearerTokenAuthenticationProvider, Spe">
     ...
   </oauthBearer>
   ```
   If `type=` is missing, Sitecore's `Factory.CreateObject` returns `null`
   and the provider is silently dropped from
   `ServiceAuthenticationManager.AuthenticationProviders`.

3. **The token's alg is in `<allowedAlgorithms>`.** If you've overridden the
   default list, make sure it still includes the alg the IdP signs with
   (commonly `RS256`).

4. **The app pool was recycled after the rename.** Provider discovery runs in
   the static constructor of `ServiceAuthenticationManager`; config changes
   do not take effect until the AppDomain reloads.

## Opt-in hardening flags

Four flags in `Spe.OAuthBearer.config` are off by default. If a previously
working token starts being rejected after an operator turned one on, the
debug log identifies which:

| Flag | Reason in debug log when this is the cause |
| --- | --- |
| `<jtiReplayCacheEnabled>` | `reason=tokenReplay jti=<value>`, or `reason=missingJti` if the token has no `jti` claim |
| `<requireAccessTokenType>` | `reason=accessTokenTypeRequired type=<value>` (Auth0, Entra, IdentityServer4 stamp `typ=JWT` and will fail this check) |
| `<requireAzpWhenMultiAudience>` | `reason=azpMismatch azp=<value> clientId=<value>` (only triggers when the token's `aud` claim has more than one value) |
| `<jwksAllowLoopbackHttp>` | `[JWKS] action=jwksUriRejected reason=schemeNotAllowed` (this flag must be `true` AND the JwksUri host must be a literal loopback address) |

Flip the flag back to `false` and the rejection should stop. If you need the
flag on, the issuing IdP must emit the corresponding claim. See the inline
notes in `Spe.OAuthBearer.config.example` for the per-IdP compatibility
matrix.

## JWKS over HTTPS in the local Docker dev stack

The dev stack (`docker-compose.yml` + `docker-compose.override.yml`) reaches
the Sitecore Identity Server container as `id` on the internal Docker
network. Because the JWKS resolver requires HTTPS for non-loopback hosts,
`http://id/...` is rejected with
`[JWKS] action=jwksUriRejected reason=schemeNotAllowed`. `Uri.IsLoopback`
only returns true for `localhost` / `127.0.0.1` / `[::1]`, so
`<jwksAllowLoopbackHttp>` does not help with the `id` hostname.

The fix is to point `<jwksUri>` at the Traefik-fronted hostname
(`speid.dev.local` by default, set by `${ID_HOST}` in `.env`):

```xml
<jwksUri>https://speid.dev.local/.well-known/openid-configuration/jwks</jwksUri>
```

For that to work from inside the CM container you need both DNS and trust:

1. **Network alias on Traefik** so the Docker DNS for `speid.dev.local`
   resolves to the Traefik container. In `docker-compose.yml`:
   ```yaml
   traefik:
     networks:
       default:
         aliases:
           - ${ID_HOST}
           - ${CM_HOST}
   ```
2. **Dev CA imported into LocalMachine\Root** of the CM container so the
   Traefik-served leaf cert validates. Mount `./docker/traefik/certs` into
   the CM service in `docker-compose.override.yml` and add an idempotent
   import in `docker/tools/entrypoints/iis/Development.ps1`:
   ```powershell
   $devCert = "C:\certs\devcert.cer"
   if (Test-Path $devCert) {
       $thumb = (Get-PfxCertificate -FilePath $devCert).Thumbprint
       if (-not (Test-Path "Cert:\LocalMachine\Root\$thumb")) {
           Import-Certificate -FilePath $devCert -CertStoreLocation Cert:\LocalMachine\Root | Out-Null
       }
   }
   ```

Verify from inside the CM before touching `<jwksUri>`:

```powershell
docker compose exec cm powershell -c "iwr https://speid.dev.local/.well-known/openid-configuration/jwks -UseBasicParsing | Select StatusCode"
```

A `200` confirms both DNS and trust are good.

## "Authenticated user is unauthorized"

Symptom: `action=userUnauthorized user=<user>`. Auth succeeded (signature,
claims, lookup all passed), but the resolved user is not authorised for the
target service. The fix is a mismatch between the **authentication-layer
user** and the **service-layer authorization** described in "Verify the
impersonated user is allowed by the service" above.

This is almost always one of:

- The user resolved by the auth path (OAuth Client item's Impersonated User,
  Shared Secret Client item's Impersonated User, or the token's `name`
  claim on the config-level path) is not on the service's `<authorization>`
  list and is not in any of the roles that list allows.
- The bearer path resolved to `extranet\Anonymous` because the token did not
  carry an impersonation identity and no default was configured.
- The service's authorization list in `Spe.config` has been patched to
  exclude the user's role.

### Worked example: the default-config first-time gotcha

The shipped `<remoting>` service authorization is just one role:

```xml
<remoting enabled="false" requireSecureConnection="false">
  <authorization>
    <add Permission="Allow" IdentityType="Role" Identity="sitecore\PowerShell Extensions Remoting" />
  </authorization>
</remoting>
```

If you create an OAuth Client item with `Impersonated User = sitecore\foo`,
every call returns `userUnauthorized` until `sitecore\foo` is in that role
or listed directly. Three symmetric fixes, in order of preference:

1. **Add the user to the role.** Make `sitecore\foo` a member of
   `sitecore\PowerShell Extensions Remoting` in Sitecore's User Manager.
   This is the most repeatable fix and survives re-deploys.
2. **Use the shipped service user.** SPE installs
   `sitecore\PowerShellExtensionsAPI` as a member of the role; it ships
   disabled. Enable it (or clone it under your own name) and set that as
   the Impersonated User on the Remoting Client item.
3. **Add the user directly to `<authorization>`** via a patch file (do not
   edit the shipped `Spe.config`):
   ```xml
   <add Permission="Allow" IdentityType="User" Identity="sitecore\foo" />
   ```

### Pick a service user, not `sitecore\admin`

Two things to keep in mind when choosing the Impersonated User:

- **Remoting calls execute with whatever access that user has in
  Sitecore.** Item read/write, role memberships, admin status, security
  domain access, all of it. Whatever the user can do in Content Editor or
  via `Sitecore.Context.User` from code, every script call from this
  client can do too.
- **Do not impersonate `sitecore\admin`.** It is the obvious choice and
  the wrong one. Any token compromise gives full administrative access
  to Sitecore. Audit logs become useless because every call looks like
  the same superuser. Future tightening of the policy or service
  authorization stops being meaningful.

Recommended pattern: a dedicated service user per workload, named for the
workload (e.g. `sitecore\spe-ci-deploy`, `sitecore\spe-content-import`),
made a member of `sitecore\PowerShell Extensions Remoting` (or a
purpose-built role), and granted only the item permissions the workload
actually needs. The shipped `sitecore\PowerShellExtensionsAPI` is fine as
a starting point for low-risk read-only workloads.

## Policy denial: 403

Authentication succeeded but the request was refused before the script ran.
The handler tags every 403 with an `X-SPE-Restriction` response header so
you can classify the failure without parsing the body.

| `X-SPE-Restriction` | Cause | Audit action |
| --- | --- | --- |
| `policy-blocked` | Script contains a cmdlet not on the policy's allowlist. The blocked cmdlet name is on the response in `X-SPE-BlockedCommand`, the policy name in `X-SPE-Policy`. | `scriptRejectedByPolicy` |
| `session-not-owned` | A second identity tried to attach to a `sessionId` the first caller already claimed. | `sessionOwnershipMismatch` |

For `policy-blocked`, fix by editing the matching policy at
`/sitecore/system/Modules/PowerShell/Settings/Access/Policies` to allow
the cmdlet, or by switching the Remoting Client item to a less restrictive
policy (see `remoting-policy-setup.md`). For `session-not-owned`, see the
decision tree above.

A separate, related case is `action=userUnauthorized` (covered under
"Authenticated user is unauthorized" below): the impersonated user exists
but is not in the role list the service requires. That path is gated by
`<powershell><services>` in `Spe.config`, not by the Remoting Policy.

## Script failure: 424

Auth succeeded, the script ran, but it threw. The response body still
parses; SPE captures the error as part of the standard envelope:

- With `outputFormat=json` (default for raw HTTP callers): the body is
  `{ "output": [...], "errors": [...] }`. Add `&errorFormat=structured` to
  receive each error as an object with `message`, `errorCategory`,
  `categoryReason`, `fullyQualifiedErrorId`, `exceptionType`, and
  `scriptStackTrace` instead of the bare string form. See
  `remoting-raw-http.md` for the full envelope reference.
- With `outputFormat=clixml` (the SPE module path): error records are
  appended to the CliXml document; the module surfaces them as
  PowerShell error objects.

A 424 is a script-level outcome, not an SPE bug. Read the error message in
the response body before opening a server log.

## Throttling: 429

The Remoting Client item exceeded its rate limit. The response carries:

| Header | Meaning |
| --- | --- |
| `X-RateLimit-Limit` | Configured limit on the matched policy. |
| `X-RateLimit-Remaining` | Remaining calls in the current window. |
| `X-RateLimit-Reset` | Unix-seconds when the window resets. |
| `Retry-After` | Seconds to wait before the next retry. |

Audit log: `action=throttled clientId=<id>`. To verify a specific client is
configured to bypass throttling (operational scenarios, never production
defaults), look for `action=throttleBypassed` on the same `rid`.

If throttling is firing unexpectedly, raise the policy's limit or assign a
less restrictive policy to the client item. Do not disable throttling
globally.

## Cold start: 503

Symptom: `action=coldStart`, response is 503 with `Retry-After: 2`.

The Remoting Client registry is still warming. The handler returns 503
rather than authenticate against an incomplete client list. A retry after
the suggested delay almost always succeeds.

If 503s persist past the first few seconds of an app pool recycle, the
registry failed to load. Check the CM event log for the underlying error
(usually a Sitecore database connection problem or a malformed Remoting
Client item).

## CORS preflight failures

If a browser-based caller fails on the `OPTIONS` preflight before any
script POST is made, the issue is CORS, not authentication. See
[`cors.md`](cors.md) for which origins are allowed and how the response
headers are computed.

## Correlating a request across services

Every log line for a single request carries the same `rid=`. To see the full
picture for one request:

- Filter your log viewer or shell pipeline on `rid=<value>`.
- The audit lines give you the shape. The debug lines in between give you the
  reason. Error lines for the same `rid` (if any) carry stack traces.

## Enabling debug logs

`PowerShellLog` is a dedicated log4net logger. Raise it to `DEBUG` in
`App_Config/Include/Sitecore.Logging.config` (or via a patch include) by
setting the level of the `Spe` logger or the `PowerShell` logger (whichever
your build uses) to `DEBUG`. Drop back to `INFO` once the investigation is
done; the debug volume on a busy CM is high.
