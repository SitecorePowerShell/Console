# OAuth Bearer Remoting Quickstart

End-to-end walkthrough for authenticating SPE remoting with an OAuth bearer
token. The scripts in this document are self-contained. Copy them into any
PowerShell session on any machine that can reach your Sitecore Identity
Server and Content Management host. No repo checkout, `.env`, or module
sideloading required (you do need the SPE remoting module installed:
`Install-Module SPE`).

For an Auth0-specific setup (different token endpoint, `audience` parameter,
issuer trailing slash, `azp` client id claim), see
[OAuth Bearer Remoting with Auth0](remoting-oauth-auth0.md).

## 0. Prerequisites on the CM

One-time setup, performed by whoever administers the CM:

1. Copy `App_Config/Include/Spe/Spe.OAuthBearer.config.example` to
   `Spe.OAuthBearer.config` (drop the `.example` suffix).
2. Populate three values inside the `<oauthBearer>` element:
   - `<jwksUri>` - your IdP's JWKS endpoint, e.g.
     `https://<identity-host>/.well-known/openid-configuration/jwks`
   - `<allowedIssuers><issuer>` - the exact `iss` claim your IdP stamps, e.g.
     `https://<identity-host>`
   - `<allowedAudiences><audience>` - the `aud` value your IdP registers for
     the remoting client (e.g. `spe-remoting`). Sitecore Identity Server also
     adds `https://<identity-host>/resources` - list both if your tokens
     carry it.
3. Register an OAuth client at your IdP using the `client_credentials` grant.
   Note the `client_id` and `client_secret`.

## 1. Variables

Fill these in once. Every script below reads from this block.

```powershell
# Identity Server
$IdentityHost  = "https://your-identity-host"      # no trailing slash
$ClientId      = "your-client-id"
$ClientSecret  = "your-client-secret"
$Scope         = "spe.remoting"                    # space-delimited if multiple

# Sitecore CM (where SPE remoting runs)
$CmUri         = "https://your-cm-host"

# OAuth Client item (for the Content Editor / API creation below)
$ImpersonateAs = "sitecore\admin"
$PolicyName    = "Unrestricted"                    # must exist under Access/Policies
```

## 2. Get a token from Identity Server

Posts a `client_credentials` grant and returns the `access_token`. Works on
Windows PowerShell 5.1 and PowerShell 7+. The `TrustAllCertsPolicy` block
only matters if your IdP uses a self-signed dev cert; remove it in
production.

```powershell
# Trust self-signed dev certs (PS 5.1 only, harmless elsewhere)
if ($PSVersionTable.PSVersion.Major -lt 6) {
    if (-not ([System.Management.Automation.PSTypeName]'TrustAllCertsPolicy').Type) {
        Add-Type @"
public class TrustAllCertsPolicy : System.Net.ICertificatePolicy {
    public bool CheckValidationResult(System.Net.ServicePoint sp,
        System.Security.Cryptography.X509Certificates.X509Certificate cert,
        System.Net.WebRequest req, int problem) { return true; }
}
"@
    }
    [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
    [System.Net.ServicePointManager]::SecurityProtocol  = [System.Net.SecurityProtocolType]::Tls12
}

$tokenParams = @{
    Uri         = "$IdentityHost/connect/token"
    Method      = "POST"
    ContentType = "application/x-www-form-urlencoded"
    Body        = @{
        grant_type    = "client_credentials"
        client_id     = $ClientId
        client_secret = $ClientSecret
        scope         = $Scope
    }
    ErrorAction = "Stop"
}
if ($PSVersionTable.PSVersion.Major -ge 6) { $tokenParams["SkipCertificateCheck"] = $true }

$response = Invoke-RestMethod @tokenParams
$AccessToken = $response.access_token
if (-not $AccessToken) { throw "Token endpoint returned no access_token." }
Write-Host "Got token ($([int]$response.expires_in)s lifetime)" -ForegroundColor Green
```

### Decode the token (so the next step can match it)

The OAuth Client item you create in Sitecore must match the `iss` and
`client_id` claims exactly. Peek at them before creating the item:

```powershell
$payloadPart = $AccessToken.Split('.')[1]
switch ($payloadPart.Length % 4) { 2 { $payloadPart += "==" } 3 { $payloadPart += "=" } }
$payloadJson = [Text.Encoding]::UTF8.GetString(
    [Convert]::FromBase64String($payloadPart.Replace('-','+').Replace('_','/')))
$claims = ConvertFrom-Json $payloadJson
"`n  iss       : $($claims.iss)"
"  aud       : $($claims.aud -join ', ')"
"  client_id : $($claims.client_id)"
"  scope     : $($claims.scope)"
```

Copy the `iss` and `client_id` values. You will paste them into the OAuth
Client item next.

## 3. Create the OAuth Client item

Pick one of the two options.

### Option A: Content Editor (recommended for first-time setup)

1. Log in to Sitecore and open **Content Editor**.
2. Navigate to `/sitecore/system/Modules/PowerShell/Settings/Access/Remoting Clients`.
3. Right-click **Remoting Clients**, choose **Insert > OAuth Client**.
4. On the dialog:
   - **Name** - any descriptive label, e.g. `CI Pipeline`.
   - **Enabled** (General tab) - leave unchecked for now; you will flip it on
     after a successful dry-run test below.
   - **Allowed Issuer** (Authentication tab) - paste the `iss` from the
     decoded token (e.g. `https://your-identity-host`). It must match
     exactly, including protocol and any trailing slash behavior.
   - **OAuth Client Ids** (Authentication tab) - paste the `client_id` from
     the decoded token. One per line if you have more than one.
   - **Impersonated User** (Authorization tab) - e.g. `sitecore\admin`.
   - **Remoting Policy** (Authorization tab) - pick a policy that allows the
     cmdlets you plan to call.
5. Click **Create**. Then open the item, check **Enabled**, and save.

### Option B: Via remoting (if you have an existing admin session)

Requires that you already have a working SPE remoting credential (Shared
Secret or another OAuth client). Adjust the inner script as needed.

```powershell
# Open an existing admin session by your preferred method, then:
Invoke-RemoteScript -Session $adminSession -ScriptBlock {
    $oauthTemplateId   = "{E1F946A8-86E0-4CDF-BFA7-3089E669D153}"
    $clientsFolderPath = "master:/sitecore/system/Modules/PowerShell/Settings/Access/Remoting Clients"
    $policiesPath      = "master:/sitecore/system/Modules/PowerShell/Settings/Access/Policies"

    $policy = Get-Item -Path "$policiesPath/$($using:PolicyName)"
    if (-not $policy) { throw "Policy '$($using:PolicyName)' not found." }

    $item = New-Item -Path $clientsFolderPath -Name "CiPipeline" -ItemType $oauthTemplateId
    $item.Editing.BeginEdit()
    $item["AllowedIssuer"]   = $using:claims.iss
    $item["OAuthClientIds"]  = $using:claims.client_id
    $item["ImpersonatedUser"] = $using:ImpersonateAs
    $item["Policy"]           = $policy.ID.ToString()
    $item["Enabled"]          = "1"
    $item.Editing.EndEdit() | Out-Null
    $item.ID.ToString()
}
```

The `OnItemSaved` handler invalidates the client cache immediately, so the
item is live on the very next request.

## 4. Test the round trip

Uses the token from step 2. Confirms signature verification, claim
validation, and item match all pass.

```powershell
Import-Module SPE

$session = New-ScriptSession -ConnectionUri $CmUri -AccessToken $AccessToken
try {
    $who = Invoke-RemoteScript -Session $session -ScriptBlock {
        (Get-User -Current).Name
    } -Raw
    Write-Host "Impersonated as: $who" -ForegroundColor Green

    # Anything the Remoting Policy allows is fair game here.
    Invoke-RemoteScript -Session $session -ScriptBlock {
        [PSCustomObject]@{
            Host = $env:COMPUTERNAME
            User = (Get-User -Current).Name
            Time = (Get-Date).ToString("o")
        }
    } | Format-List
}
finally {
    Stop-ScriptSession -Session $session -ErrorAction SilentlyContinue
}
```

`Impersonated as:` should echo whatever you set on the OAuth Client item's
**Impersonated User** field, not the service account you registered with the
IdP. That is how you know the whole chain worked.

## 5. Troubleshooting

When a request is rejected, the CM log writes a single diagnostic line:

```
[OAuthBearer] action=validationFailed reason=<specific-reason>
```

Common reasons and fixes:

| Reason                                       | Fix                                                              |
| -------------------------------------------- | ---------------------------------------------------------------- |
| `signatureInvalid`, `keyNotResolved`         | `<jwksUri>` is wrong, unreachable, or the IdP rotated keys.      |
| `audienceNotAllowed`, `missingAudience`      | Add the token's `aud` value to `<allowedAudiences>`.             |
| `missingScope`                               | Token is missing a scope listed in `<requiredScopes>`.           |
| `clientNotFound`                             | `(iss, client_id)` pair does not match any enabled OAuth Client. |
| `disabled` or `expired`                      | The OAuth Client item's Enabled flag is off or Expires is past.  |
| `tokenReplay`, `missingJti`                  | Token replayed within its lifetime (or IdP omits `jti`). Only when `<jtiReplayCacheEnabled>` is on. |
| `accessTokenTypeRequired`, `invalidTokenType`| Token's `typ` header is not `at+jwt`. Only fails when `<requireAccessTokenType>` is on. |
| `azpMismatch`                                | Token's `azp` claim does not match the resolved `client_id`. Only when `<requireAzpWhenMultiAudience>` is on and `aud` has more than one value. |

For a 401 response with no detailed log line, check the response headers:
the `X-SPE-AuthFailureReason` and `WWW-Authenticate` headers identify the
classified failure. See [Remoting Authentication
Troubleshooting](remoting-troubleshooting.md) for the full reference.

After editing the OAuth Client item or the config, the change is picked up
immediately (item) or on the next config reload (config).
