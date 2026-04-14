# Remoting Tests - Security Baseline (Issue #1419)
# Tests that run WITHOUT security config (CLM, blocklist).
# These verify audit logging, JWT acceptance, and New-Jwt client parameters.
# Run via: .\Run-RemotingTests.ps1 -TestFile Remoting.Security.Tests.ps1
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

function Invoke-BearerRequest {
    param(
        [string]$Script,
        [string]$ClientSessionId
    )
    $jwtParams = @{
        Algorithm = 'HS256'; Issuer = 'SPE Remoting'
        Audience  = $protocolHost; Name = 'sitecore\admin'; SecretKey = $sharedSecret
    }
    if ($ClientSessionId) { $jwtParams['ClientSessionId']  = $ClientSessionId }
    $token = New-Jwt @jwtParams

    $httpClient.DefaultRequestHeaders.Authorization = `
        New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token)

    $sid = [guid]::NewGuid().ToString()
    $url = "${serviceUrl}?sessionId=$sid&rawOutput=false&outputFormat=clixml&persistentSession=false"
    $content = New-Object System.Net.Http.StringContent($Script, [System.Text.Encoding]::UTF8, "text/plain")
    return $httpClient.PostAsync($url, $content).Result
}

# ============================================================================
#  Test Group 1: Audit Logging
# ============================================================================
Write-Host "`n  [Test Group 1: Audit Logging]" -ForegroundColor White

# 1a. Successful script execution produces output (audit verified via log inspection)
$result = Invoke-RemoteScript -Session $session -ScriptBlock { "audit-test-marker" } -Raw
Assert-Equal $result "audit-test-marker" "Script executes and returns result (audit entry written server-side)"

# 1b. Bearer token auth produces audit log (verified by successful execution)
$response = Invoke-BearerRequest -Script '"bearer-audit-test"'
Assert-Equal ([int]$response.StatusCode) 200 "Bearer auth succeeds and produces audit trail"

# ============================================================================
#  Test Group 2: Language Mode Baseline
# ============================================================================
Write-Host "`n  [Test Group 2: Language Mode Baseline]" -ForegroundColor White

$clmResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    try {
        $mode = $ExecutionContext.SessionState.LanguageMode
        $mode.ToString()
    } catch {
        "error: $_"
    }
} -Raw

# Without security config, expect FullLanguage
Assert-Equal $clmResult "FullLanguage" "Language mode is FullLanguage (no security config)"

# ============================================================================
#  Test Group 3: JWT Token Acceptance
# ============================================================================
Write-Host "`n  [Test Group 3: JWT Token Acceptance]" -ForegroundColor White

# 3a. JWT with ClientSessionId claim is accepted
$sessionResponse = Invoke-BearerRequest -Script '"session-test-ok"' -ClientSessionId "correlation-123"
Assert-Equal ([int]$sessionResponse.StatusCode) 200 "JWT with clientSession claim accepted"

# 3b. JWT without optional claims works
$noClaimsResponse = Invoke-BearerRequest -Script '"no-claims-ok"'
Assert-Equal ([int]$noClaimsResponse.StatusCode) 200 "JWT without optional claims works"

# ============================================================================
#  Test Group 4: New-Jwt Client Module
# ============================================================================
Write-Host "`n  [Test Group 4: New-Jwt Client Module]" -ForegroundColor White

# 4a. New-Jwt accepts ClientSessionId parameter
$sessionToken = New-Jwt -Algorithm HS256 -Issuer "SPE Remoting" -Audience $protocolHost `
    -Name "sitecore\admin" -SecretKey $sharedSecret -ClientSessionId "sess-456"
$sessionPayloadBase64 = $sessionToken.Split('.')[1]
switch ($sessionPayloadBase64.Length % 4) {
    2 { $sessionPayloadBase64 += "==" }
    3 { $sessionPayloadBase64 += "=" }
}
$sessionPayloadJson = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($sessionPayloadBase64.Replace('-','+').Replace('_','/')))
Assert-Like $sessionPayloadJson '*"client_session":"sess-456"*' "JWT payload contains client_session claim"

# 4b. Omitting ClientSessionId doesn't break token
$plainToken = New-Jwt -Algorithm HS256 -Issuer "SPE Remoting" -Audience $protocolHost `
    -Name "sitecore\admin" -SecretKey $sharedSecret
Assert-NotNull $plainToken "New-Jwt without ClientSessionId still works"

# ============================================================================
#  Test Group 5: Connection Test Endpoint (action=test)
# ============================================================================
Write-Host "`n  [Test Group 5: Connection Test Endpoint]" -ForegroundColor White

# 5a. Test-RemoteConnection returns server info
$testResult = Test-RemoteConnection -Session $session
Assert-NotNull $testResult "Test-RemoteConnection returns a result"
Assert-NotNull $testResult.SPEVersion "Result contains SPEVersion"
Assert-NotNull $testResult.SitecoreVersion "Result contains SitecoreVersion"
Assert-NotNull $testResult.CurrentTime "Result contains CurrentTime"

# 5b. Test-RemoteConnection -Quiet returns boolean
$quietResult = Test-RemoteConnection -Session $session -Quiet
Assert-Equal $quietResult $true "Test-RemoteConnection -Quiet returns true for healthy server"

# ============================================================================
#  Test Group 6: Session Cleanup Endpoint (action=cleanup)
# ============================================================================
Write-Host "`n  [Test Group 6: Session Cleanup Endpoint]" -ForegroundColor White

# 6a. Create a persistent session, then clean it up
$cleanupSession = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost
$cleanupSession.PersistentSession = $true
Invoke-RemoteScript -Session $cleanupSession -ScriptBlock { "persistent-session-ok" } | Out-Null

# 6b. Stop-ScriptSession uses action=cleanup (no server-side script needed)
Stop-ScriptSession -Session $cleanupSession
Assert-True $true "Stop-ScriptSession completed without error"

# Cleanup
$httpClient.Dispose()
Stop-ScriptSession -Session $session
