# Remoting Tests - Shared Secret Client Expiration Enforcement
# Tests that expired Shared Secret Clients are rejected and valid/no-expiry clients work.
# Items created by Remoting.Expiration.Setup.ps1 before these tests run.
# Run via: .\Run-RemotingTests.ps1 (automatically run in the expiration phase)

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

$expiredSecret = "Test-Expired-Secret-K3y!-LongEnough-For-Validation"
$expiredKeyId = "spe_test_expired_key_001"
$validSecret = "Test-ExpirationValid-Secret-K3y!-LongEnough-Valid"
$validKeyId = "spe_test_expvalid_key_001"
$noExpirySecret = "Test-NoExpiry-Secret-K3y!-LongEnough-For-Validation"
$noExpiryKeyId = "spe_test_noexpiry_key_001"

function Invoke-ExpirationRequest {
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

# =============================================================================
# Test Group 1: Expired Key Rejected
# =============================================================================
Write-Host "`n  [Test Group 1: Expired Key Rejected]" -ForegroundColor White

$expiredResponse = Invoke-ExpirationRequest -Script '"should not execute"' -SecretKey $expiredSecret -KeyId $expiredKeyId
Assert-Equal ([int]$expiredResponse.StatusCode) 401 "Expired key is rejected (401)"

# =============================================================================
# Test Group 2: Valid Key (future expiration) Accepted
# =============================================================================
Write-Host "`n  [Test Group 2: Valid Key (future expiration) Accepted]" -ForegroundColor White

$validResponse = Invoke-ExpirationRequest -Script '"VALID_EXPIRY_OK"' -SecretKey $validSecret -KeyId $validKeyId
Assert-Equal ([int]$validResponse.StatusCode) 200 "Key with future expiration succeeds (200)"

$validBody = $validResponse.Content.ReadAsStringAsync().Result
Assert-Like $validBody "*VALID_EXPIRY_OK*" "Key with future expiration returns script output"

# =============================================================================
# Test Group 3: No Expiration Key Accepted
# =============================================================================
Write-Host "`n  [Test Group 3: No Expiration Key Accepted]" -ForegroundColor White

$noExpiryResponse = Invoke-ExpirationRequest -Script '"NO_EXPIRY_OK"' -SecretKey $noExpirySecret -KeyId $noExpiryKeyId
Assert-Equal ([int]$noExpiryResponse.StatusCode) 200 "Key with no expiration succeeds (200)"

$noExpiryBody = $noExpiryResponse.Content.ReadAsStringAsync().Result
Assert-Like $noExpiryBody "*NO_EXPIRY_OK*" "Key with no expiration returns script output"
