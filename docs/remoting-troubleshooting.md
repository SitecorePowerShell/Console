# Remoting Authentication Troubleshooting

> **Doc status**: draft living in the `Spe` repo. Needs to be migrated into the
> public book (https://doc.sitecorepowershell.com/, source at
> `SitecorePowerShell/Book`) once it's been reviewed. Keep this file in sync
> until the book page exists; delete it afterwards.

This page explains how to diagnose 401 responses from the SPE remoting service.
It is ordered by what helps most: start at the top and stop reading when your
symptom matches.

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

## Response header: `X-SPE-AuthFailureReason`

Before digging into server logs, check the response. When bearer validation
fails, the handler sets the `X-SPE-AuthFailureReason` response header to one
of:

| Value | Meaning |
| --- | --- |
| `expired` | Token's `exp` claim is in the past (beyond clock skew). |
| `disabled` | Matched a Remoting Client item but the item is disabled. |
| `invalid` | Signature, issuer, audience, scope, or client lookup failed. |

The header is only set when the handler has enough information to classify the
failure. For unclassified failures (unrecognised alg, no provider configured,
etc.) the header is absent and only the server log has the reason.

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
4. **Too many requests** -> server logs `action=throttled`, response is 429
   (not 401). Inspect the `X-RateLimit-*` response headers.

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
   `App_Config/Include/Spe/Spe.OAuthBearer.config.disabled` must be renamed
   to `Spe.OAuthBearer.config`. This file supplies the `type=` attribute on
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

## "Authenticated user is unauthorized"

Symptom: `action=userUnauthorized user=<user>`. Auth succeeded (signature,
claims, lookup all passed), but the resolved user is not authorised for the
target service.

This is almost always one of:

- The impersonated user on a Remoting Client item is not in any of the roles
  the service allows.
- The bearer path resolved to `extranet\Anonymous` because the token did not
  carry an impersonation identity and no default was configured.
- The service's authorization list in `Spe.config` has been patched to
  exclude the user's role.

Fix by editing `<powershell><services>` in a patch file; don't edit the
shipped `Spe.config` directly.

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
