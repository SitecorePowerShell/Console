<#
.SYNOPSIS
    Fetches an OAuth bearer token from the local IDS test client for use with
    the SPE OAuth bearer authentication provider.

.DESCRIPTION
    Posts a client_credentials grant to https://<ID_HOST>/connect/token using
    the SpeRemoting client registered via docker-compose.override.yml. Returns
    the access_token string. Reads ID_HOST and SPE_OAUTH_CLIENT_SECRET from
    the project .env file by default.

.PARAMETER ClientId
    OAuth client id. Defaults to "spe-remoting".

.PARAMETER ClientSecret
    Client secret. Defaults to SPE_OAUTH_CLIENT_SECRET from .env.

.PARAMETER Scope
    Space-delimited scopes. Defaults to "spe.remoting".

.PARAMETER Authority
    Token endpoint host. Defaults to https://<ID_HOST> from .env.

.EXAMPLE
    $token = & ./scripts/Get-SpeOAuthToken.ps1
    $session = New-ScriptSession -ConnectionUri https://spe.dev.local -AccessToken $token
    Invoke-RemoteScript -Session $session -ScriptBlock { Get-User -Current }
#>
[CmdletBinding()]
param(
    [string]$ClientId = "spe-remoting",
    [string]$ClientSecret,
    [string]$Scope = "spe.remoting",
    [string]$Authority
)

$ErrorActionPreference = "Stop"

. "$PSScriptRoot\assert-prerequisites.ps1"

if (-not $Authority) {
    $idHost = Get-EnvValue "ID_HOST"
    if (-not $idHost) {
        Write-Error "ID_HOST not found in .env and -Authority was not supplied."
        return
    }
    $Authority = "https://$idHost"
}

if (-not $ClientSecret) {
    $ClientSecret = Get-EnvValue "SPE_OAUTH_CLIENT_SECRET"
    if (-not $ClientSecret) {
        Write-Error "SPE_OAUTH_CLIENT_SECRET not found in .env. Run 'task init' to generate it, then 'docker compose up -d id' to apply."
        return
    }
}

$tokenEndpoint = "$Authority/connect/token"

# Trust the dev cert for local Sitecore IDS.
if ($PSVersionTable.PSVersion.Major -lt 6) {
    if (-not ([System.Management.Automation.PSTypeName]'TrustAllCertsPolicy').Type) {
        Add-Type @"
public class TrustAllCertsPolicy : System.Net.ICertificatePolicy {
    public bool CheckValidationResult(System.Net.ServicePoint sp, System.Security.Cryptography.X509Certificates.X509Certificate cert, System.Net.WebRequest req, int problem) { return true; }
}
"@
    }
    [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
}

$body = @{
    grant_type    = "client_credentials"
    client_id     = $ClientId
    client_secret = $ClientSecret
    scope         = $Scope
}

$invokeParams = @{
    Uri         = $tokenEndpoint
    Method      = "POST"
    Body        = $body
    ContentType = "application/x-www-form-urlencoded"
    ErrorAction = "Stop"
}
if ($PSVersionTable.PSVersion.Major -ge 6) {
    $invokeParams['SkipCertificateCheck'] = $true
}

try {
    $response = Invoke-RestMethod @invokeParams
} catch {
    Write-Error "Token request to $tokenEndpoint failed: $($_.Exception.Message)"
    return
}

if (-not $response.access_token) {
    Write-Error "Token endpoint returned no access_token. Response: $($response | ConvertTo-Json -Depth 3)"
    return
}

return $response.access_token
