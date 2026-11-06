# Remoting Tests - JWT Claims Validation (Issue #1420)
# Tests iat (issued-at), nbf (not-before), and token lifetime validation.
# Run via: .\Run-RemotingTests.ps1 -TestFile Remoting.JwtClaims.Tests.ps1
# Requires: SPE Remoting enabled, shared secret configured

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost
$serviceUrl = "$protocolHost/-/script/script/"

# Shared HttpClient for JWT tests
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

function Send-JwtRequest {
    param([string]$Token, [string]$Script = '"jwt-claims-test"')

    $httpClient.DefaultRequestHeaders.Authorization = `
        New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $Token)

    $sid = [guid]::NewGuid().ToString()
    $url = "${serviceUrl}?sessionId=$sid&rawOutput=false&outputFormat=clixml&persistentSession=false"
    $content = New-Object System.Net.Http.StringContent($Script, [System.Text.Encoding]::UTF8, "text/plain")
    return $httpClient.PostAsync($url, $content).Result
}

# ============================================================================
#  Test Group 1: Backward Compatibility - Tokens Without iat/nbf
# ============================================================================
Write-Host "`n  [Test Group 1: Backward Compatibility - No iat/nbf]" -ForegroundColor White

# 1a. Token without iat/nbf still works (third-party / pre-9.0 client compat)
$token = New-Jwt -Algorithm HS256 -Issuer "SPE Remoting" -Audience $protocolHost `
    -Name "sitecore\admin" -SecretKey $sharedSecret -NoIssuedAt
$response = Send-JwtRequest -Token $token
Assert-Equal ([int]$response.StatusCode) 200 "Token without iat/nbf is accepted (backward compatible)"

# ============================================================================
#  Test Group 2: Valid iat and nbf Claims
# ============================================================================
Write-Host "`n  [Test Group 2: Valid iat/nbf Claims]" -ForegroundColor White

# 2a. Token with valid iat (current time, default behaviour)
$token = New-Jwt -Algorithm HS256 -Issuer "SPE Remoting" -Audience $protocolHost `
    -Name "sitecore\admin" -SecretKey $sharedSecret
$response = Send-JwtRequest -Token $token
Assert-Equal ([int]$response.StatusCode) 200 "Token with valid iat (now) is accepted"

# 2b. Token with valid nbf (current time)
$now = [datetimeoffset]::UtcNow.ToUnixTimeSeconds()
$token = New-Jwt -Algorithm HS256 -Issuer "SPE Remoting" -Audience $protocolHost `
    -Name "sitecore\admin" -SecretKey $sharedSecret -NotBefore $now
$response = Send-JwtRequest -Token $token
Assert-Equal ([int]$response.StatusCode) 200 "Token with valid nbf (now) is accepted"

# 2c. Token with both iat and nbf set to current time
$token = New-Jwt -Algorithm HS256 -Issuer "SPE Remoting" -Audience $protocolHost `
    -Name "sitecore\admin" -SecretKey $sharedSecret -NotBefore $now
$response = Send-JwtRequest -Token $token
Assert-Equal ([int]$response.StatusCode) 200 "Token with both iat and nbf (now) is accepted"

# 2d. Token with nbf slightly in the past
$pastNbf = [datetimeoffset]::UtcNow.AddSeconds(-10).ToUnixTimeSeconds()
$token = New-Jwt -Algorithm HS256 -Issuer "SPE Remoting" -Audience $protocolHost `
    -Name "sitecore\admin" -SecretKey $sharedSecret -NotBefore $pastNbf
$response = Send-JwtRequest -Token $token
Assert-Equal ([int]$response.StatusCode) 200 "Token with nbf in the past is accepted"

# ============================================================================
#  Test Group 3: Invalid iat/nbf Claims (Should Be Rejected)
# ============================================================================
Write-Host "`n  [Test Group 3: Invalid iat/nbf Claims]" -ForegroundColor White

# 3a. Token with nbf far in the future (should be rejected)
$futureNbf = [datetimeoffset]::UtcNow.AddMinutes(10).ToUnixTimeSeconds()
$token = New-Jwt -Algorithm HS256 -Issuer "SPE Remoting" -Audience $protocolHost `
    -Name "sitecore\admin" -SecretKey $sharedSecret -NotBefore $futureNbf
$response = Send-JwtRequest -Token $token
Assert-Equal ([int]$response.StatusCode) 401 "Token with nbf 10 minutes in the future is rejected"

# 3b. Token with iat far in the future (should be rejected)
$futureIat = [datetimeoffset]::UtcNow.AddMinutes(10).ToUnixTimeSeconds()
$token = New-Jwt -Algorithm HS256 -Issuer "SPE Remoting" -Audience $protocolHost `
    -Name "sitecore\admin" -SecretKey $sharedSecret -IssuedAt $futureIat
$response = Send-JwtRequest -Token $token
Assert-Equal ([int]$response.StatusCode) 401 "Token with iat 10 minutes in the future is rejected"

# 3c. Token with nbf just outside clock skew tolerance (31+ seconds ahead)
$nearFutureNbf = [datetimeoffset]::UtcNow.AddSeconds(60).ToUnixTimeSeconds()
$token = New-Jwt -Algorithm HS256 -Issuer "SPE Remoting" -Audience $protocolHost `
    -Name "sitecore\admin" -SecretKey $sharedSecret -NotBefore $nearFutureNbf
$response = Send-JwtRequest -Token $token
Assert-Equal ([int]$response.StatusCode) 401 "Token with nbf 60s in the future is rejected (outside clock skew)"

# ============================================================================
#  Test Group 4: Clock Skew Tolerance
# ============================================================================
Write-Host "`n  [Test Group 4: Clock Skew Tolerance]" -ForegroundColor White

# 4a. Token with nbf a few seconds in the future (within 30s clock skew)
$skewNbf = [datetimeoffset]::UtcNow.AddSeconds(10).ToUnixTimeSeconds()
$token = New-Jwt -Algorithm HS256 -Issuer "SPE Remoting" -Audience $protocolHost `
    -Name "sitecore\admin" -SecretKey $sharedSecret -NotBefore $skewNbf
$response = Send-JwtRequest -Token $token
Assert-Equal ([int]$response.StatusCode) 200 "Token with nbf 10s in future is accepted (within clock skew)"

# 4b. Token with iat a few seconds in the future (within 30s clock skew)
$skewIat = [datetimeoffset]::UtcNow.AddSeconds(10).ToUnixTimeSeconds()
$token = New-Jwt -Algorithm HS256 -Issuer "SPE Remoting" -Audience $protocolHost `
    -Name "sitecore\admin" -SecretKey $sharedSecret -IssuedAt $skewIat
$response = Send-JwtRequest -Token $token
Assert-Equal ([int]$response.StatusCode) 200 "Token with iat 10s in future is accepted (within clock skew)"

# ============================================================================
#  Test Group 5: New-Jwt iat/nbf Parameters
# ============================================================================
Write-Host "`n  [Test Group 5: New-Jwt iat/nbf Parameters]" -ForegroundColor White

# 5a. New-Jwt includes iat claim by default
$token = New-Jwt -Algorithm HS256 -Issuer "SPE Remoting" -Audience $protocolHost `
    -Name "sitecore\admin" -SecretKey $sharedSecret
$payloadBase64 = $token.Split('.')[1]
switch ($payloadBase64.Length % 4) {
    2 { $payloadBase64 += "==" }
    3 { $payloadBase64 += "=" }
}
$payloadJson = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($payloadBase64.Replace('-','+').Replace('_','/')))
Assert-Like $payloadJson '*"iat":*' "New-Jwt includes iat claim by default"

# 5b. New-Jwt with -NotBefore includes nbf claim
$nbfValue = [datetimeoffset]::UtcNow.ToUnixTimeSeconds()
$token = New-Jwt -Algorithm HS256 -Issuer "SPE Remoting" -Audience $protocolHost `
    -Name "sitecore\admin" -SecretKey $sharedSecret -NotBefore $nbfValue
$payloadBase64 = $token.Split('.')[1]
switch ($payloadBase64.Length % 4) {
    2 { $payloadBase64 += "==" }
    3 { $payloadBase64 += "=" }
}
$payloadJson = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($payloadBase64.Replace('-','+').Replace('_','/')))
Assert-Like $payloadJson '*"nbf":*' "New-Jwt -NotBefore includes nbf claim in payload"

# 5c. New-Jwt with explicit -IssuedAt uses the specified value
$specificIat = 1700000000
$token = New-Jwt -Algorithm HS256 -Issuer "SPE Remoting" -Audience $protocolHost `
    -Name "sitecore\admin" -SecretKey $sharedSecret -IssuedAt $specificIat
$payloadBase64 = $token.Split('.')[1]
switch ($payloadBase64.Length % 4) {
    2 { $payloadBase64 += "==" }
    3 { $payloadBase64 += "=" }
}
$payloadJson = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($payloadBase64.Replace('-','+').Replace('_','/')))
Assert-Like $payloadJson '*"iat":1700000000*' "New-Jwt -IssuedAt uses the explicit value"

# 5d. New-Jwt with -NoIssuedAt omits iat; without -NotBefore omits nbf
$token = New-Jwt -Algorithm HS256 -Issuer "SPE Remoting" -Audience $protocolHost `
    -Name "sitecore\admin" -SecretKey $sharedSecret -NoIssuedAt
$payloadBase64 = $token.Split('.')[1]
switch ($payloadBase64.Length % 4) {
    2 { $payloadBase64 += "==" }
    3 { $payloadBase64 += "=" }
}
$payloadJson = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($payloadBase64.Replace('-','+').Replace('_','/')))
Assert-True ($payloadJson -notlike '*"iat"*') "New-Jwt -NoIssuedAt omits iat claim"
Assert-True ($payloadJson -notlike '*"nbf"*') "New-Jwt without -NotBefore omits nbf claim"

# Cleanup
$httpClient.Dispose()
Stop-ScriptSession -Session $session
