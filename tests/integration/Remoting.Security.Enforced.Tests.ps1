# Remoting Tests - Security Enforcement (Issue #1419)
# Tests that REQUIRE the security test config (CLM, command blocklist, scope restrictions).
# These are run AFTER tests/configs/test/ configs are deployed and the app has restarted.
# Run via: .\Run-RemotingTests.ps1 (automatically deployed and run in the enforced phase)
# Requires: z.SPE.Security.Tests.config deployed, SPE Remoting enabled, shared secret configured

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
        [string]$Scope,
        [string]$ClientSessionId
    )
    $jwtParams = @{
        Algorithm = 'HS256'; Issuer = 'SPE Remoting'
        Audience  = $protocolHost; Name = 'sitecore\admin'; SecretKey = $sharedSecret
    }
    if ($Scope)      { $jwtParams['Scope']      = $Scope }
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
#  Test Group 1: Constrained Language Mode Enforcement
# ============================================================================
Write-Host "`n  [Test Group 1: CLM Enforcement]" -ForegroundColor White

$clmResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    try {
        $mode = $ExecutionContext.SessionState.LanguageMode
        $mode.ToString()
    } catch {
        "error: $_"
    }
} -Raw

Assert-Equal $clmResult "ConstrainedLanguage" "Language mode is ConstrainedLanguage"

# Verify .NET type access is blocked in CLM
$typeResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    try {
        [System.IO.File]::Exists("C:\web.config")
        "type-access-allowed"
    } catch {
        "type-access-blocked"
    }
} -Raw
Assert-Equal $typeResult "type-access-blocked" "CLM blocks direct .NET type access"

# Verify basic cmdlets still work in CLM
$cmdletResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    (Get-Date).Year.ToString()
} -Raw
Assert-True ([int]$cmdletResult -ge 2025) "CLM allows basic cmdlets (Get-Date)"

# ============================================================================
#  Test Group 2: Command Blocklist
# ============================================================================
Write-Host "`n  [Test Group 2: Command Blocklist]" -ForegroundColor White

# Invoke-Expression should be blocked by commandRestrictions
$blockResponse = Invoke-BearerRequest -Script 'Invoke-Expression "1+1"'
Assert-Equal ([int]$blockResponse.StatusCode) 403 "Invoke-Expression blocked by command restrictions"

# Verify allowed commands still work
$allowResponse = Invoke-BearerRequest -Script 'Get-Item -Path "master:/"'
Assert-Equal ([int]$allowResponse.StatusCode) 200 "Get-Item allowed through command restrictions"

# Verify blocked command name is in response
$blockBody = $blockResponse.Content.ReadAsStringAsync().Result
Assert-Like $blockBody "*Invoke-Expression*" "Response includes blocked command name"

# ============================================================================
#  Test Group 3: Scope Restriction Enforcement
# ============================================================================
Write-Host "`n  [Test Group 3: Scope Restriction Enforcement]" -ForegroundColor White

# 3a. read-only scope blocks Remove-Item
$readOnlyResponse = Invoke-BearerRequest -Script 'Remove-Item -Path "master:/content/nonexistent" -ErrorAction Stop' `
    -Scope "read-only" -ClientSessionId "test-sess-002"
Assert-Equal ([int]$readOnlyResponse.StatusCode) 403 "read-only scope blocks Remove-Item"

# 3b. Read operations still allowed under read-only scope
$readResponse = Invoke-BearerRequest -Script 'Get-Item -Path "master:/"' -Scope "read-only"
Assert-Equal ([int]$readResponse.StatusCode) 200 "read-only scope allows Get-Item"

# ============================================================================
#  Test Group 4: CLM Restoration on Exception
# ============================================================================
Write-Host "`n  [Test Group 4: CLM Restoration on Exception]" -ForegroundColor White

# Execute a script that throws, then verify CLM is restored for the next call
$errorResponse = Invoke-BearerRequest -Script 'throw "deliberate-test-error"'
$errorStatus = [int]$errorResponse.StatusCode
Assert-True ($errorStatus -eq 424 -or $errorStatus -eq 200) "Script that throws returns error status ($errorStatus)"

# Verify the next script still runs under CLM (mode was restored, not stuck)
$postErrorResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $ExecutionContext.SessionState.LanguageMode.ToString()
} -Raw
Assert-Equal $postErrorResult "ConstrainedLanguage" "CLM restored after script exception"

# ============================================================================
#  Test Group 5: Unknown Scope Names
# ============================================================================
Write-Host "`n  [Test Group 5: Unknown Scope Names]" -ForegroundColor White

# Send a JWT with a scope that doesn't match any configured scopeRestriction.
# Expected: request succeeds (permissive-by-default) but a warning is logged server-side.
$unknownScopeResponse = Invoke-BearerRequest -Script '"unknown-scope-test"' -Scope "nonexistent-scope-xyz"
Assert-Equal ([int]$unknownScopeResponse.StatusCode) 200 "Unknown scope passes validation (permissive-by-default)"

# ============================================================================
#  Test Group 6: Scope + Service Restriction Interaction
# ============================================================================
Write-Host "`n  [Test Group 6: Scope + Service Restriction Interaction]" -ForegroundColor White

# A command blocked by the service restriction is still blocked even with a scope that doesn't restrict it
$composedResponse = Invoke-BearerRequest -Script 'Invoke-Expression "1+1"' -Scope "read-only"
Assert-Equal ([int]$composedResponse.StatusCode) 403 "Service blocklist still applies when scope restrictions also active"

# A command blocked by scope is still blocked even if the service allows it
$scopeBlockResponse = Invoke-BearerRequest -Script 'Remove-Item -Path "master:/content/nonexistent" -ErrorAction Stop' -Scope "read-only"
Assert-Equal ([int]$scopeBlockResponse.StatusCode) 403 "Scope blocklist applies on top of service restrictions"

# Cleanup
$httpClient.Dispose()
Stop-ScriptSession -Session $session
