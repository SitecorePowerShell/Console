# OAuth Bearer Remoting with Auth0

Auth0-specific walkthrough for wiring SPE remoting to an Auth0 tenant as the
identity provider. This complements the generic
[OAuth bearer quickstart](remoting-oauth-quickstart.md) - read that first for
the CM-side config and OAuth Client item concepts; this doc only covers the
Auth0 differences.

## 1. On Auth0 (dashboard.auth0.com)

1. **Create an API** (Applications > APIs > Create API)
   - **Name**: `SPE Remoting` (anything)
   - **Identifier (Audience)**: `https://spe-remoting` - pick a stable
     URN-style value; this becomes the `aud` claim. Copy it.
   - **Signing Algorithm**: `RS256` (required - SPE validates RS256 via JWKS)
   - After creating, open the **Permissions** tab and add a scope, e.g.
     `spe.remoting` (name it anything, just match what you will request).
   - **Settings** tab: set **Token Expiration** and **Allow Offline Access**
     as needed. Leave **RBAC** off unless you want Auth0 to gate scopes per
     client.

2. **Create a Machine-to-Machine application** (Applications > Applications >
   Create Application)
   - Type: **Machine to Machine Applications**.
   - Authorize it against the API you just created, and grant the
     `spe.remoting` scope.
   - From the app's **Settings** tab, copy:
     - **Domain** (e.g. `your-tenant.us.auth0.com`) - the issuer host
     - **Client ID**
     - **Client Secret**

3. **Note the issuer format**. Auth0 stamps `iss` as
   `https://<tenant>.<region>.auth0.com/` with a **trailing slash**. That
   exact string must go into the OAuth Client item's `Allowed Issuer`. When
   in doubt, decode the token (step 3 below) and paste what Auth0 actually
   sends.

## 2. On the Sitecore CM

Rename `App_Config/Include/Spe/Spe.OAuthBearer.config.disabled` to
`Spe.OAuthBearer.config` and set:

```xml
<oauthBearer>
  <jwksUri>https://your-tenant.us.auth0.com/.well-known/jwks.json</jwksUri>
  <allowedIssuers>
    <issuer>https://your-tenant.us.auth0.com/</issuer>
  </allowedIssuers>
  <allowedAudiences>
    <audience>https://spe-remoting</audience>
  </allowedAudiences>
  <requiredScopes>
    <scope>spe.remoting</scope>
  </requiredScopes>
</oauthBearer>
```

Keep the trailing slash on `issuer`. `jwksUri` for Auth0 is always
`https://<tenant>/.well-known/jwks.json` - not the OIDC discovery path that
Sitecore Identity Server uses.

## 3. Request a token from Auth0

Use the `client_credentials` grant. Auth0's token endpoint is `/oauth/token`
(not `/connect/token`) and it expects `audience`, not scope-only:

```powershell
$IdentityHost = "https://your-tenant.us.auth0.com"
$ClientId     = "<Auth0 client id>"
$ClientSecret = "<Auth0 client secret>"
$Audience     = "https://spe-remoting"
$Scope        = "spe.remoting"
$CmUri        = "https://your-cm-host"

$response = Invoke-RestMethod -Uri "$IdentityHost/oauth/token" -Method POST `
    -ContentType "application/json" `
    -Body (@{
        grant_type    = "client_credentials"
        client_id     = $ClientId
        client_secret = $ClientSecret
        audience      = $Audience
        scope         = $Scope
    } | ConvertTo-Json)

$AccessToken = $response.access_token
```

Decode it and copy the `iss` and the client id. Auth0 M2M tokens stamp the
client id into **`azp`** (and `sub` as `{clientId}@clients`). They do not
include a `client_id` claim by default. SPE resolves the client id by
checking `client_id`, then falling back to `azp`, `appid`, and `cid` in that
order, so `azp` is what you want to read from an Auth0 token:

```powershell
$payloadPart = $AccessToken.Split('.')[1]
switch ($payloadPart.Length % 4) { 2 { $payloadPart += "==" } 3 { $payloadPart += "=" } }
$claims = [Text.Encoding]::UTF8.GetString(
    [Convert]::FromBase64String($payloadPart.Replace('-','+').Replace('_','/'))) |
    ConvertFrom-Json

"iss  : $($claims.iss)"
"aud  : $($claims.aud -join ', ')"
"azp  : $($claims.azp)"
"scope: $($claims.scope)"
```

## 4. Create the OAuth Client item in Sitecore

Content Editor >
`/sitecore/system/Modules/PowerShell/Settings/Access/Remoting Clients` >
Insert > **OAuth Client**.

- **Allowed Issuer**: paste `$claims.iss` verbatim (Auth0's trailing slash
  included).
- **OAuth Client Ids**: paste `$claims.azp` (the Auth0 M2M app's client id).
  SPE will match this against the token's `azp` claim via its
  `client_id -> azp -> appid -> cid` fallback chain. No Auth0 Action needed
  to add a `client_id` claim.
- **Impersonated User**: e.g. `sitecore\admin`.
- **Remoting Policy**: pick one under `Access/Policies`.
- **Enabled**: check after the first successful test.

## 5. Round trip

```powershell
Import-Module SPE
$session = New-ScriptSession -ConnectionUri $CmUri -AccessToken $AccessToken
Invoke-RemoteScript -Session $session -ScriptBlock { (Get-User -Current).Name } -Raw
Stop-ScriptSession -Session $session
```

`(Get-User -Current).Name` should echo the **Impersonated User** on the OAuth
Client item, not the Auth0 M2M application name. That is how you know
signature verification, claim validation, and client matching all passed.

## Auth0 gotchas

- **`aud` is the API Identifier**, not the Auth0 application. If you request
  a token without `audience`, Auth0 returns an **opaque** token (not a JWT)
  and SPE's JWKS validation will fail with `reason=signature`. Always pass
  `audience`.
- **Trailing slash on `iss`**: Auth0 includes it; Sitecore Identity Server
  does not. A mismatch produces `reason=iss` in the CM log.
- **Client id claim is `azp`** on Auth0 tokens. SPE's client id resolver
  tries `client_id`, then `azp`, then `appid` (Azure v1), then `cid` (Okta) -
  so for Auth0 you paste `azp` into `OAuthClientIds`. No Auth0 Action to
  synthesize `client_id` is needed.
- **Region in the domain**: `us`, `eu`, `au` tenants all have the region in
  the hostname. `jwksUri` and `issuer` must both include it.
- **Key rotation**: Auth0 rotates JWKS keys occasionally. SPE caches JWKS -
  if tokens start failing with `reason=signature` after a rotation, a config
  reload or app pool recycle refetches.
- **Rate limits**: free-tier Auth0 caps M2M tokens per month. Cache the
  access token in your client and only re-request on expiry.
- **Token lifetime cap**: Auth0's default M2M token lifetime is **86400s
  (24h)**. If the CM has `<maxTokenLifetimeSeconds>` set lower (e.g. 3600),
  requests fail with `[JWT] action=validationFailed reason=lifetimeExceeded
  lifetime=86400s maximum=3600s`. Fix on the Auth0 side (preferred): in
  **Applications > APIs > (your API) > Settings**, set **Token Expiration
  (Seconds)** to match or fall below the CM cap (e.g. `3600`), then cache
  and refresh on the client. Or raise `<maxTokenLifetimeSeconds>` on the CM
  (set to `0` to disable the check - not recommended, short-lived tokens
  limit blast radius if a token leaks).
