# Remoting Tests - File access security (#1443)
# Verifies that GetPathFromParameters rejects:
#   - Unknown origin + absolute path (no Spe.Remoting.AllowedFileRoots set)
#   - Empty origin + absolute path
#   - Alias origin + canonicalized path that escapes the alias root
# And still allows known aliases against well-known files.
# Requires: SPE Remoting + fileDownload services enabled, shared secret configured.

Write-Host "`n  [Test Group: File access security (#1443)]" -ForegroundColor White

# Shared HttpClient with TLS-trust for the dev cert
$handler = New-Object System.Net.Http.HttpClientHandler
if ($PSVersionTable.PSVersion.Major -ge 6) {
    $handler.ServerCertificateCustomValidationCallback = [System.Net.Http.HttpClientHandler]::DangerousAcceptAnyServerCertificateValidator
} else {
    if (-not ([System.Management.Automation.PSTypeName]'TrustAllCertsPolicy').Type) {
        Add-Type @"
public class TrustAllCertsPolicy : System.Net.ICertificatePolicy {
    public bool CheckValidationResult(System.Net.ServicePoint sp,
        System.Security.Cryptography.X509Certificates.X509Certificate cert,
        System.Net.WebRequest req, int problem) { return true; }
}
"@
    }
    [System.Net.ServicePointManager]::CertificatePolicy = [TrustAllCertsPolicy]::new()
}
$httpClient = New-Object System.Net.Http.HttpClient($handler)

function New-FileBearerToken {
    $jwtParams = @{
        Algorithm = 'HS256'
        Issuer    = 'SPE Remoting'
        Audience  = $protocolHost
        Name      = 'sitecore\admin'
        SecretKey = $sharedSecret
    }
    return New-Jwt @jwtParams
}

function Invoke-FileDownload {
    param(
        [Parameter(Mandatory)] [string]$Origin,
        [Parameter()] [AllowEmptyString()] [string]$Path = ''
    )
    $token = New-FileBearerToken
    $httpClient.DefaultRequestHeaders.Authorization = `
        New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token)

    # /-/script/file/<origin>/?path=<path>
    $encoded = [System.Uri]::EscapeDataString($Path)
    $url = "$protocolHost/-/script/file/$Origin/?path=$encoded"
    return $httpClient.GetAsync($url).Result
}

# 1. Unknown origin + absolute path -- BLOCKED (allowlist empty by default)
$resp = Invoke-FileDownload -Origin 'custom' -Path 'C:\Windows\win.ini'
Assert-Equal ([int]$resp.StatusCode) 403 "Unknown origin (custom) + absolute Windows path is rejected"

# 2. Empty origin + path OUTSIDE the allowlist -- BLOCKED. The URL routes
#    /file//?path=... which collapses to origin="" on the server. We use
#    C:\Windows so the request fails the allowlist check (test config grants
#    only C:\inetpub\wwwroot).
$token = New-FileBearerToken
$httpClient.DefaultRequestHeaders.Authorization = `
    New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token)
$emptyOriginUrl = "$protocolHost/-/script/file//?path=" + [System.Uri]::EscapeDataString('C:\Windows\System32\drivers\etc\hosts')
$resp = $httpClient.GetAsync($emptyOriginUrl).Result
Assert-Equal ([int]$resp.StatusCode) 403 "Empty origin + absolute path outside allowlist is rejected"

# 3. Alias origin (data) + traversal that escapes the alias root -- BLOCKED.
#    The plain ".." string check already covers this; included as regression.
$resp = Invoke-FileDownload -Origin 'data' -Path '..\..\Windows\win.ini'
Assert-Equal ([int]$resp.StatusCode) 403 "Alias origin (data) + .. traversal is rejected"

# 4. Alias origin (data) + canonicalized escape (mixed separators that bypass plain
#    .. check but resolve outside the data root after Path.GetFullPath) -- BLOCKED.
$resp = Invoke-FileDownload -Origin 'data' -Path 'logs/../../../../Windows/win.ini'
Assert-Equal ([int]$resp.StatusCode) 403 "Alias origin (data) + canonicalized escape is rejected"

# 5. Known alias (debug) + readme.txt -- ALLOWED. Sitecore ships this file under
#    sitecore/admin/debug; it's the same fixture used by Remoting.Download.Tests.ps1.
$resp = Invoke-FileDownload -Origin 'debug' -Path 'readme.txt'
Assert-True (([int]$resp.StatusCode) -eq 200 -or ([int]$resp.StatusCode) -eq 404) "Known alias (debug) + relative path is permitted (200 or 404, never 403)"
if (([int]$resp.StatusCode) -eq 403) {
    Write-Host "         Got 403 - alias resolution regressed" -ForegroundColor Yellow
}

# 6. Known alias with no path resolves to the alias root (download will 404 since
#    a directory isn't a file, but it must not be 403'd).
$resp = Invoke-FileDownload -Origin 'data' -Path ''
Assert-NotEqual ([int]$resp.StatusCode) 403 "Known alias (data) with empty path is not rejected as forbidden"

# 7. Custom origin + absolute path UNDER an allowlisted root - allowed.
#    The deploy config (tests/configs/deploy/z.Spe.Security.Disabler.config) sets
#    Spe.Remoting.AllowedFileRoots="C:\inetpub\wwwroot". A file under that root
#    must round-trip cleanly. Probe an existing file the container always has.
$probePath = Invoke-RemoteScript -Session (New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost) -ScriptBlock {
    Get-ChildItem -Path "C:\inetpub\wwwroot\bin" -File -Filter "Spe.dll" |
        Select-Object -First 1 -ExpandProperty FullName
} -Raw
if ($probePath) {
    $resp = Invoke-FileDownload -Origin 'custom' -Path $probePath
    Assert-Equal ([int]$resp.StatusCode) 200 "Custom origin + absolute path under allowlist root is permitted"
} else {
    Skip-Test "Custom origin + absolute path under allowlist root is permitted" "no probe file found in C:\inetpub\wwwroot\bin"
}

# 8. Custom origin + absolute path OUTSIDE the allowlist root - rejected.
#    Allowlist is C:\inetpub\wwwroot only; the SAM database (and any C:\Windows
#    file) sits outside it.
$resp = Invoke-FileDownload -Origin 'custom' -Path 'C:\Windows\System32\drivers\etc\hosts'
Assert-Equal ([int]$resp.StatusCode) 403 "Custom origin + absolute path outside allowlist root is rejected"

$httpClient.Dispose()
