# Remoting Tests - HMAC Algorithm Validation
# Tests that the server rejects unsupported algorithms and accepts valid ones.
# Run via: .\Run-RemotingTests.ps1 -TestFile Remoting.Algorithm.Tests.ps1
# Requires: SPE Remoting enabled, shared secret configured

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost
$serviceUrl = "$protocolHost/-/script/script/"

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

function Send-RawJwtRequest {
    param([string]$Token, [string]$Script = '"algorithm-test"')

    $httpClient.DefaultRequestHeaders.Authorization = `
        New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $Token)

    $sid = [guid]::NewGuid().ToString()
    $url = "${serviceUrl}?sessionId=$sid&rawOutput=false&outputFormat=clixml&persistentSession=false"
    $content = New-Object System.Net.Http.StringContent($Script, [System.Text.Encoding]::UTF8, "text/plain")
    return $httpClient.PostAsync($url, $content).Result
}

function New-RawJwt {
    param(
        [string]$Algorithm,
        [string]$Secret,
        [string]$Username = "sitecore\admin",
        [string]$HmacVariant = $null
    )

    $exp = [datetimeoffset]::UtcNow.AddSeconds(30).ToUnixTimeSeconds()
    $header = [ordered]@{ alg = $Algorithm; typ = "JWT" }
    $payload = [ordered]@{ iss = "SPE Remoting"; exp = $exp; aud = $protocolHost; name = $Username }

    $headerJson = $header | ConvertTo-Json -Compress
    $payloadJson = $payload | ConvertTo-Json -Compress

    $headerB64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($headerJson)).Split('=')[0].Replace('+', '-').Replace('/', '_')
    $payloadB64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($payloadJson)).Split('=')[0].Replace('+', '-').Replace('/', '_')

    $toBeSigned = "$headerB64.$payloadB64"

    # Use a real HMAC to sign, but the header claims a different algorithm
    $hmacAlg = if ($HmacVariant) { $HmacVariant } else { $Algorithm }
    $signingAlgorithm = switch ($hmacAlg) {
        "HS256" { New-Object System.Security.Cryptography.HMACSHA256 }
        "HS384" { New-Object System.Security.Cryptography.HMACSHA384 }
        "HS512" { New-Object System.Security.Cryptography.HMACSHA512 }
        default { New-Object System.Security.Cryptography.HMACSHA256 }
    }
    $signingAlgorithm.Key = [System.Text.Encoding]::UTF8.GetBytes($Secret)
    $sig = [Convert]::ToBase64String($signingAlgorithm.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($toBeSigned))).Split('=')[0].Replace('+', '-').Replace('/', '_')

    return "$headerB64.$payloadB64.$sig"
}

# ============================================================================
#  Test Group 1: Unsupported Algorithm
# ============================================================================
Write-Host "`n  [Test Group 1: Unsupported Algorithm]" -ForegroundColor White

# 1a. Token with alg=none is rejected
$token = New-RawJwt -Algorithm "none" -Secret $sharedSecret
$response = Send-RawJwtRequest -Token $token
Assert-Equal ([int]$response.StatusCode) 401 "Token with alg=none is rejected"

# 1b. Token with alg=RS256 is rejected (asymmetric algorithm not supported)
$token = New-RawJwt -Algorithm "RS256" -Secret $sharedSecret
$response = Send-RawJwtRequest -Token $token
Assert-Equal ([int]$response.StatusCode) 401 "Token with alg=RS256 is rejected"

# 1c. Token with a fabricated algorithm name is rejected
$token = New-RawJwt -Algorithm "FAKE512" -Secret $sharedSecret
$response = Send-RawJwtRequest -Token $token
Assert-Equal ([int]$response.StatusCode) 401 "Token with alg=FAKE512 is rejected"

# ============================================================================
#  Test Group 2: Valid Algorithms via Session Object
# ============================================================================
Write-Host "`n  [Test Group 2: Valid Algorithms via Session Object]" -ForegroundColor White

# 2a. HS256 (default) works
$hs256Session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost
$result = Invoke-RemoteScript -Session $hs256Session -ScriptBlock { "hs256-ok" } -Raw
Assert-Equal $result "hs256-ok" "HS256 session succeeds"
Stop-ScriptSession -Session $hs256Session

# 2b. HS256 explicit works
$hs256Session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost -Algorithm HS256
$result = Invoke-RemoteScript -Session $hs256Session -ScriptBlock { "hs256-explicit-ok" } -Raw
Assert-Equal $result "hs256-explicit-ok" "HS256 explicit session succeeds"
Stop-ScriptSession -Session $hs256Session

# 2c. HS384 works (secret must be >= 48 chars)
if ($sharedSecret.Length -ge 48) {
    $hs384Session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost -Algorithm HS384
    $result = Invoke-RemoteScript -Session $hs384Session -ScriptBlock { "hs384-ok" } -Raw
    Assert-Equal $result "hs384-ok" "HS384 session succeeds"
    Stop-ScriptSession -Session $hs384Session
} else {
    Write-Host "    SKIP: HS384 test requires secret >= 48 chars (current: $($sharedSecret.Length))" -ForegroundColor Yellow
}

# 2d. HS512 works (secret must be >= 64 chars)
if ($sharedSecret.Length -ge 64) {
    $hs512Session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost -Algorithm HS512
    $result = Invoke-RemoteScript -Session $hs512Session -ScriptBlock { "hs512-ok" } -Raw
    Assert-Equal $result "hs512-ok" "HS512 session succeeds"
    Stop-ScriptSession -Session $hs512Session
} else {
    Write-Host "    SKIP: HS512 test requires secret >= 64 chars (current: $($sharedSecret.Length))" -ForegroundColor Yellow
}

# ============================================================================
#  Test Group 3: Algorithm Mismatch
# ============================================================================
Write-Host "`n  [Test Group 3: Algorithm Mismatch]" -ForegroundColor White

# 3a. Token header says HS384 but signed with HS256 - signature mismatch
$token = New-RawJwt -Algorithm "HS384" -Secret $sharedSecret -HmacVariant "HS256"
$response = Send-RawJwtRequest -Token $token
Assert-Equal ([int]$response.StatusCode) 401 "Token claiming HS384 but signed with HS256 is rejected"

# Cleanup
$httpClient.Dispose()
Stop-ScriptSession -Session $session
