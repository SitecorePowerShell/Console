# Remoting Tests - Security Enforcement (Issue #1419)
# Tests that REQUIRE the security test config (maxTokenLifetimeSeconds).
# These are run AFTER tests/configs/test/ configs are deployed and the app has restarted.
# Run via: .\Run-RemotingTests.ps1 (automatically deployed and run in the enforced phase)
# Requires: z.SPE.Security.Tests.config deployed, SPE Remoting enabled, shared secret configured
#
# Note: CLM and command restriction enforcement moved to item-based remoting policies
# (tested in Remoting.RemotingPolicies.Tests.ps1). This file now tests only auth-level
# config settings that remain in XML.

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

function Send-CustomJwtRequest {
    param([string]$Token, [string]$Script = '"lifetime-test"')

    $httpClient.DefaultRequestHeaders.Authorization = `
        New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $Token)

    $sid = [guid]::NewGuid().ToString()
    $url = "${serviceUrl}?sessionId=$sid&rawOutput=false&outputFormat=clixml&persistentSession=false"
    $content = New-Object System.Net.Http.StringContent($Script, [System.Text.Encoding]::UTF8, "text/plain")
    return $httpClient.PostAsync($url, $content).Result
}

# ============================================================================
#  Test Group 1: Max Token Lifetime Enforcement (maxTokenLifetimeSeconds=300)
# ============================================================================
Write-Host "`n  [Test Group 1: Max Token Lifetime Enforcement]" -ForegroundColor White

# 1a. Token with short lifetime (30s, default) and iat - should pass
$token = New-Jwt -Algorithm HS256 -Issuer "SPE Remoting" -Audience $protocolHost `
    -Name "sitecore\admin" -SecretKey $sharedSecret -ValidForSeconds 30 -IncludeIssuedAt
$response = Send-CustomJwtRequest -Token $token
Assert-Equal ([int]$response.StatusCode) 200 "Token with 30s lifetime accepted (under 300s max)"

# 1b. Token with lifetime at the limit (300s) and iat - should pass
$token = New-Jwt -Algorithm HS256 -Issuer "SPE Remoting" -Audience $protocolHost `
    -Name "sitecore\admin" -SecretKey $sharedSecret -ValidForSeconds 300 -IncludeIssuedAt
$response = Send-CustomJwtRequest -Token $token
Assert-Equal ([int]$response.StatusCode) 200 "Token with 300s lifetime accepted (at max limit)"

# 1c. Token with excessive lifetime (3600s) and iat - should be rejected
$token = New-Jwt -Algorithm HS256 -Issuer "SPE Remoting" -Audience $protocolHost `
    -Name "sitecore\admin" -SecretKey $sharedSecret -ValidForSeconds 3600 -IncludeIssuedAt
$response = Send-CustomJwtRequest -Token $token
Assert-Equal ([int]$response.StatusCode) 401 "Token with 3600s lifetime rejected (exceeds 300s max)"

# 1d. Token with excessive lifetime but NO iat - should pass (iat required for enforcement)
$token = New-Jwt -Algorithm HS256 -Issuer "SPE Remoting" -Audience $protocolHost `
    -Name "sitecore\admin" -SecretKey $sharedSecret -ValidForSeconds 3600
$response = Send-CustomJwtRequest -Token $token
Assert-Equal ([int]$response.StatusCode) 200 "Token with 3600s lifetime but no iat accepted (lifetime check skipped)"

# Cleanup
$httpClient.Dispose()
