# Remoting Tests - Throttle Action Enforcement
# Tests that throttle actions (Block, Bypass) work correctly per Shared Secret Client.
# Shared Secret Clients are created by Remoting.Throttle.Setup.ps1 before these tests run.
# Each key has RequestLimit=3, ThrottleWindow=60s.
# Run via: .\Run-RemotingTests.ps1 (automatically run in the throttle phase)
# Requires: Throttle test Shared Secret Clients created, SPE Remoting enabled

$serviceUrl = "$protocolHost/-/script/script/"

# Shared HttpClient for JWT/bearer tests
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

function Invoke-ThrottleRequest {
    param(
        [string]$Script,
        [string]$SecretKey,
        [string]$KeyId
    )
    $jwtParams = @{
        Algorithm = 'HS256'; Issuer = 'SPE Remoting'
        Audience  = $protocolHost; SecretKey = $SecretKey
    }
    if ($KeyId) { $jwtParams['KeyId'] = $KeyId }
    $token = New-Jwt @jwtParams

    $httpClient.DefaultRequestHeaders.Authorization = `
        New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token)

    $sid = [guid]::NewGuid().ToString()
    $url = "${serviceUrl}?sessionId=$sid&rawOutput=false&outputFormat=clixml&persistentSession=false"
    $content = New-Object System.Net.Http.StringContent($Script, [System.Text.Encoding]::UTF8, "text/plain")
    return $httpClient.PostAsync($url, $content).Result
}

$blockSecret   = "Test-ThrottleBlock-Secret-K3y!-LongEnough-For-Validation"
$blockKeyId    = "spe_test_throttle_block_01"
$bypassSecret  = "Test-ThrottleBypass-Secret-K3y!-LongEnough-For-Validation"
$bypassKeyId   = "spe_test_throttle_bypass01"
$defaultSecret = "Test-ThrottleDefault-Secret-K3y!-LongEnough-For-Validation"
$defaultKeyId  = "spe_test_throttle_deflt_01"

# ============================================================================
#  Test Group 1: Block Action -- requests within limit succeed
# ============================================================================
Write-Host "`n  [Test Group 1: Block Action -- Within Limit]" -ForegroundColor White

for ($i = 1; $i -le 3; $i++) {
    $response = Invoke-ThrottleRequest -Script '"OK"' -SecretKey $blockSecret -KeyId $blockKeyId
    Assert-Equal ([int]$response.StatusCode) 200 "Block key request $i of 3 succeeds (within limit)"
}

# ============================================================================
#  Test Group 2: Block Action -- exceeding limit returns 429
# ============================================================================
Write-Host "`n  [Test Group 2: Block Action -- Exceeds Limit]" -ForegroundColor White

$blockedResponse = Invoke-ThrottleRequest -Script '"OK"' -SecretKey $blockSecret -KeyId $blockKeyId
Assert-Equal ([int]$blockedResponse.StatusCode) 429 "Block key request 4 returns 429 (rate limit exceeded)"

# Verify rate-limit headers are present
$limitHeader = $blockedResponse.Headers.GetValues("X-RateLimit-Limit") | Select-Object -First 1
Assert-Equal $limitHeader "3" "X-RateLimit-Limit header is 3"

$remainingHeader = $blockedResponse.Headers.GetValues("X-RateLimit-Remaining") | Select-Object -First 1
Assert-Equal $remainingHeader "0" "X-RateLimit-Remaining header is 0"

$retryHeader = $blockedResponse.Headers.GetValues("Retry-After") | Select-Object -First 1
Assert-True ([int]$retryHeader -gt 0) "Retry-After header is present and positive"

# ============================================================================
#  Test Group 3: Bypass Action -- requests within limit succeed
# ============================================================================
Write-Host "`n  [Test Group 3: Bypass Action -- Within Limit]" -ForegroundColor White

for ($i = 1; $i -le 3; $i++) {
    $response = Invoke-ThrottleRequest -Script '"OK"' -SecretKey $bypassSecret -KeyId $bypassKeyId
    Assert-Equal ([int]$response.StatusCode) 200 "Bypass key request $i of 3 succeeds (within limit)"
}

# ============================================================================
#  Test Group 4: Bypass Action -- exceeding limit still returns 200
# ============================================================================
Write-Host "`n  [Test Group 4: Bypass Action -- Exceeds Limit (allowed)]" -ForegroundColor White

$bypassedResponse = Invoke-ThrottleRequest -Script '"BYPASSED"' -SecretKey $bypassSecret -KeyId $bypassKeyId
Assert-Equal ([int]$bypassedResponse.StatusCode) 200 "Bypass key request 4 returns 200 (throttle bypassed)"

$bypassBody = $bypassedResponse.Content.ReadAsStringAsync().Result
Assert-Like $bypassBody "*BYPASSED*" "Bypass key response body contains script output"

# Verify rate-limit headers still present (tracking continues even in bypass)
$bypassLimitHeader = $bypassedResponse.Headers.GetValues("X-RateLimit-Limit") | Select-Object -First 1
Assert-Equal $bypassLimitHeader "3" "Bypass: X-RateLimit-Limit header is 3"

$bypassRemainingHeader = $bypassedResponse.Headers.GetValues("X-RateLimit-Remaining") | Select-Object -First 1
Assert-Equal $bypassRemainingHeader "0" "Bypass: X-RateLimit-Remaining header is 0"

# ============================================================================
#  Test Group 5: Default Action (empty field) -- behaves like Block
# ============================================================================
Write-Host "`n  [Test Group 5: Default Action -- Behaves Like Block]" -ForegroundColor White

for ($i = 1; $i -le 3; $i++) {
    $response = Invoke-ThrottleRequest -Script '"OK"' -SecretKey $defaultSecret -KeyId $defaultKeyId
    Assert-Equal ([int]$response.StatusCode) 200 "Default key request $i of 3 succeeds (within limit)"
}

$defaultBlockedResponse = Invoke-ThrottleRequest -Script '"OK"' -SecretKey $defaultSecret -KeyId $defaultKeyId
Assert-Equal ([int]$defaultBlockedResponse.StatusCode) 429 "Default key request 4 returns 429 when action is empty -- defaults to Block"

# Cleanup
$httpClient.Dispose()
