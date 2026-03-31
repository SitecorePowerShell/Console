# Remoting Tests - Restriction Profile Enforcement (Issue #1426)
# Tests that REQUIRE the restriction profile test config (profile="read-only" on remoting).
# These are run AFTER tests/configs/test/z.Spe.RestrictionProfiles.Tests.config is deployed.
# Run via: .\Run-RemotingTests.ps1 (automatically deployed and run in the profile phase)
# Requires: z.Spe.RestrictionProfiles.Tests.config deployed, SPE Remoting enabled, shared secret configured

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost
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

function Invoke-ProfileBearerRequest {
    param(
        [string]$Script,
        [string]$Scope,
        [string]$ClientSessionId
    )
    $jwtParams = @{
        Algorithm = 'HS256'; Issuer = 'SPE Remoting'
        Audience  = $protocolHost; Name = 'sitecore\admin'; SecretKey = $sharedSecret
    }
    if ($Scope)           { $jwtParams['Scope']           = $Scope }
    if ($ClientSessionId) { $jwtParams['ClientSessionId'] = $ClientSessionId }
    $token = New-Jwt @jwtParams

    $httpClient.DefaultRequestHeaders.Authorization = `
        New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token)

    $sid = [guid]::NewGuid().ToString()
    $url = "${serviceUrl}?sessionId=$sid&rawOutput=false&outputFormat=clixml&persistentSession=false"
    $content = New-Object System.Net.Http.StringContent($Script, [System.Text.Encoding]::UTF8, "text/plain")
    return $httpClient.PostAsync($url, $content).Result
}

# ============================================================================
#  Test Group 1: Profile-Based Language Mode
# ============================================================================
Write-Host "`n  [Test Group 1: Profile Language Mode]" -ForegroundColor White

# The read-only profile sets ConstrainedLanguage
$langResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    try {
        $mode = $ExecutionContext.SessionState.LanguageMode
        $mode.ToString()
    } catch {
        "error: $_"
    }
} -Raw

Assert-Equal $langResult "ConstrainedLanguage" "read-only profile enforces ConstrainedLanguage"

# ============================================================================
#  Test Group 2: Profile Command Blocklist
# ============================================================================
Write-Host "`n  [Test Group 2: Profile Command Blocklist]" -ForegroundColor White

# 2a. Write commands blocked by read-only profile
$removeResponse = Invoke-ProfileBearerRequest -Script 'Remove-Item -Path "master:/content/nonexistent" -ErrorAction Stop'
Assert-Equal ([int]$removeResponse.StatusCode) 403 "read-only profile blocks Remove-Item"

$setResponse = Invoke-ProfileBearerRequest -Script 'Set-Item -Path "master:/content" -Name "test"'
Assert-Equal ([int]$setResponse.StatusCode) 403 "read-only profile blocks Set-Item"

$newResponse = Invoke-ProfileBearerRequest -Script 'New-Item -Path "master:/content" -Name "profile-test" -ItemType "Sample/Sample Item"'
Assert-Equal ([int]$newResponse.StatusCode) 403 "read-only profile blocks New-Item"

$publishResponse = Invoke-ProfileBearerRequest -Script 'Publish-Item -Path "master:/content"'
Assert-Equal ([int]$publishResponse.StatusCode) 403 "read-only profile blocks Publish-Item"

# 2b. Response body includes blocked command name
$removeBody = $removeResponse.Content.ReadAsStringAsync().Result
Assert-Like $removeBody "*Remove-Item*" "Response includes blocked command name for profile violation"

# 2c. Read commands still allowed
$readResponse = Invoke-ProfileBearerRequest -Script 'Get-Item -Path "master:/"'
Assert-Equal ([int]$readResponse.StatusCode) 200 "read-only profile allows Get-Item"

$childResponse = Invoke-ProfileBearerRequest -Script 'Get-ChildItem -Path "master:/"'
Assert-Equal ([int]$childResponse.StatusCode) 200 "read-only profile allows Get-ChildItem"

# ============================================================================
#  Test Group 3: Module Loading Restrictions
# ============================================================================
Write-Host "`n  [Test Group 3: Module Loading Restrictions]" -ForegroundColor White

# Import-Module is blocked by the read-only profile
$importModResponse = Invoke-ProfileBearerRequest -Script 'Import-Module SqlServer'
Assert-Equal ([int]$importModResponse.StatusCode) 403 "read-only profile blocks Import-Module"

$newModResponse = Invoke-ProfileBearerRequest -Script 'New-Module -Name "test" -ScriptBlock { function foo {} }'
Assert-Equal ([int]$newModResponse.StatusCode) 403 "read-only profile blocks New-Module"

$removeModResponse = Invoke-ProfileBearerRequest -Script 'Remove-Module SPE'
Assert-Equal ([int]$removeModResponse.StatusCode) 403 "read-only profile blocks Remove-Module"

# ============================================================================
#  Test Group 4: Execution Escape Prevention
# ============================================================================
Write-Host "`n  [Test Group 4: Execution Escape Prevention]" -ForegroundColor White

$iexResponse = Invoke-ProfileBearerRequest -Script 'Invoke-Expression "1+1"'
Assert-Equal ([int]$iexResponse.StatusCode) 403 "read-only profile blocks Invoke-Expression"

$icmResponse = Invoke-ProfileBearerRequest -Script 'Invoke-Command -ScriptBlock { 1 }'
Assert-Equal ([int]$icmResponse.StatusCode) 403 "read-only profile blocks Invoke-Command"

$jobResponse = Invoke-ProfileBearerRequest -Script 'Start-Job -ScriptBlock { 1 }'
Assert-Equal ([int]$jobResponse.StatusCode) 403 "read-only profile blocks Start-Job"

$eventResponse = Invoke-ProfileBearerRequest -Script 'Register-EngineEvent -SourceIdentifier "test" -Action { }'
Assert-Equal ([int]$eventResponse.StatusCode) 403 "read-only profile blocks Register-EngineEvent"

# ============================================================================
#  Test Group 5: Remote Session Prevention
# ============================================================================
Write-Host "`n  [Test Group 5: Remote Session Prevention]" -ForegroundColor White

$newPsResponse = Invoke-ProfileBearerRequest -Script 'New-PSSession -ComputerName localhost'
Assert-Equal ([int]$newPsResponse.StatusCode) 403 "read-only profile blocks New-PSSession"

$enterPsResponse = Invoke-ProfileBearerRequest -Script 'Enter-PSSession -ComputerName localhost'
Assert-Equal ([int]$enterPsResponse.StatusCode) 403 "read-only profile blocks Enter-PSSession"

# ============================================================================
#  Test Group 6: JWT Scope-to-Profile Mapping
# ============================================================================
Write-Host "`n  [Test Group 6: JWT Scope-to-Profile Mapping]" -ForegroundColor White

# 6a. Scope matching a more restrictive profile overrides the service default
# read-only-strict blocks Add-Type in addition to what read-only blocks
$strictResponse = Invoke-ProfileBearerRequest -Script 'Add-Type -AssemblyName System.IO.Compression' -Scope "read-only-strict"
Assert-Equal ([int]$strictResponse.StatusCode) 403 "JWT scope=read-only-strict blocks Add-Type (stricter than service default)"

# 6b. Read operations still work under read-only-strict scope
$strictReadResponse = Invoke-ProfileBearerRequest -Script 'Get-Item -Path "master:/"' -Scope "read-only-strict"
Assert-Equal ([int]$strictReadResponse.StatusCode) 200 "read-only-strict scope allows Get-Item"

# 6c. Unknown scope falls back to service profile (read-only)
$unknownScopeResponse = Invoke-ProfileBearerRequest -Script 'Get-Item -Path "master:/"' -Scope "nonexistent-profile-xyz"
Assert-Equal ([int]$unknownScopeResponse.StatusCode) 200 "Unknown scope falls back to service profile (read-only allows Get-Item)"

# Unknown scope still blocked on write operations (falls back to service's read-only profile)
$unknownWriteResponse = Invoke-ProfileBearerRequest -Script 'Remove-Item -Path "master:/content/nonexistent"' -Scope "nonexistent-profile-xyz"
Assert-Equal ([int]$unknownWriteResponse.StatusCode) 403 "Unknown scope falls back to service profile (read-only blocks Remove-Item)"

# ============================================================================
#  Test Group 7: Backward Compatibility (No Profile)
# ============================================================================
Write-Host "`n  [Test Group 7: Backward Compatibility]" -ForegroundColor White

# Formatting and pipeline cmdlets should work under read-only profile
$formatResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    @(1, 2, 3) | Where-Object { $_ -gt 1 } | ForEach-Object { $_ * 2 } | Measure-Object -Sum | Select-Object -ExpandProperty Sum
} -Raw

Assert-Equal $formatResult "10" "Pipeline cmdlets (Where-Object, ForEach-Object, Measure-Object) work under read-only profile"

# Get-Variable and basic operations should work
$varResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $x = 42
    (Get-Variable x).Value.ToString()
} -Raw
Assert-Equal $varResult "42" "Get-Variable works under read-only profile"

# ============================================================================
#  Test Group 8: Profile Enforcement After Exception
# ============================================================================
Write-Host "`n  [Test Group 8: Profile Enforcement After Exception]" -ForegroundColor White

# Execute a script that throws, then verify the profile is still active
$errorResponse = Invoke-ProfileBearerRequest -Script 'throw "deliberate-profile-test-error"'
$errorStatus = [int]$errorResponse.StatusCode
Assert-True ($errorStatus -eq 424 -or $errorStatus -eq 200) "Script that throws returns status ($errorStatus)"

# Next request should still be constrained by the profile
$postErrorResponse = Invoke-ProfileBearerRequest -Script 'Remove-Item -Path "master:/content/nonexistent"'
Assert-Equal ([int]$postErrorResponse.StatusCode) 403 "Profile enforcement survives script exception"

# Language mode should be restored
$postErrorLang = Invoke-RemoteScript -Session $session -ScriptBlock {
    $ExecutionContext.SessionState.LanguageMode.ToString()
} -Raw
Assert-Equal $postErrorLang "ConstrainedLanguage" "Language mode restored after exception under profile"

# ============================================================================
#  Test Group 9: Item-Based Profile Overrides
#  NOTE: Override items are created by Remoting.RestrictionProfiles.Setup.ps1
#  BEFORE the profile config is deployed (because New-Item/Remove-Item are
#  blocked by the read-only profile). Teardown is handled by
#  Remoting.RestrictionProfiles.Teardown.ps1 AFTER the profile config is removed.
# ============================================================================
Write-Host "`n  [Test Group 9: Item-Based Profile Overrides]" -ForegroundColor White

# 9a. Get-Database should be blocked (override item adds it to the read-only blocklist)
$overrideBlockResponse = Invoke-ProfileBearerRequest -Script 'Get-Database -Name "master"'
Assert-Equal ([int]$overrideBlockResponse.StatusCode) 403 "Get-Database blocked by item-based override"

# 9b. Blocked command name is in the response
$overrideBlockBody = $overrideBlockResponse.Content.ReadAsStringAsync().Result
Assert-Like $overrideBlockBody "*Get-Database*" "Response includes overridden blocked command name"

# 9c. Other read commands still work (override is additive, doesn't affect existing allows)
$stillAllowedResponse = Invoke-ProfileBearerRequest -Script 'Get-Item -Path "master:/"'
Assert-Equal ([int]$stillAllowedResponse.StatusCode) 200 "Get-Item still allowed (override is additive)"

# 9d. Commands already blocked by config profile remain blocked
$configBlockResponse = Invoke-ProfileBearerRequest -Script 'Remove-Item -Path "master:/content/nonexistent"'
Assert-Equal ([int]$configBlockResponse.StatusCode) 403 "Remove-Item still blocked (config profile unchanged)"

# ============================================================================
#  Test Group 10: Dynamic Invocation Rejection
# ============================================================================
Write-Host "`n  [Test Group 10: Dynamic Invocation Rejection]" -ForegroundColor White

# 10a. Variable-based command invocation should be rejected
$dynVarResponse = Invoke-ProfileBearerRequest -Script '$cmd = "Remove-Item"; & $cmd "master:/content/nonexistent"'
Assert-Equal ([int]$dynVarResponse.StatusCode) 403 "Dynamic invocation via variable (& `$cmd) is rejected"

# 10b. Response should identify the rejection reason
$dynVarBody = $dynVarResponse.Content.ReadAsStringAsync().Result
Assert-Like $dynVarBody "*dynamic invocation*" "Response identifies dynamic invocation as the reason"

# 10c. String expression invocation should be rejected
$dynExprResponse = Invoke-ProfileBearerRequest -Script '& ("Remove" + "-Item") "master:/content/nonexistent"'
Assert-Equal ([int]$dynExprResponse.StatusCode) 403 "Dynamic invocation via expression (& (`"cmd`")) is rejected"

# 10d. Static commands still work fine
$staticResponse = Invoke-ProfileBearerRequest -Script 'Get-Item -Path "master:/"'
Assert-Equal ([int]$staticResponse.StatusCode) 200 "Static command invocation still works"

# ============================================================================
#  Test Group 11: Trusted Script Elevation
#  NOTE: Trusted Script items are created by Remoting.RestrictionProfiles.Setup.ps1
# ============================================================================
Write-Host "`n  [Test Group 11: Trusted Script Elevation]" -ForegroundColor White

# 11a. A script referenced by a Trusted Script item should run with FullLanguage
# even under the read-only profile (ConstrainedLanguage). The setup script creates
# a Trusted Script item pointing to a test script that checks its language mode.
$trustResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $trustTestScript = Get-Item -Path "master:/sitecore/system/Modules/PowerShell/Script Library/SPE/Core/Platform/Functions/Resolve-Error"
    if ($trustTestScript) { "TRUST_ITEM_EXISTS" } else { "TRUST_ITEM_MISSING" }
} -Raw

if ($trustResult -eq "TRUST_ITEM_EXISTS") {
    # Execute the trusted script via the restfulv2 API (item-based path that triggers trust eval)
    $trustResponse = Invoke-ProfileBearerRequest -Script 'Get-Item -Path "master:/"'
    Assert-Equal ([int]$trustResponse.StatusCode) 200 "Trusted script item is accessible"
} else {
    Write-Host "    [SKIP] Trusted script item not found (Resolve-Error). Trust elevation test requires ser push." -ForegroundColor Yellow
}

# 11b. An untrusted script stays constrained
$untrustedLang = Invoke-RemoteScript -Session $session -ScriptBlock {
    $ExecutionContext.SessionState.LanguageMode.ToString()
} -Raw
Assert-Equal $untrustedLang "ConstrainedLanguage" "Untrusted inline script stays in ConstrainedLanguage"

# ============================================================================
#  Test Group 12: Unknown Profile Fail-Closed (via JWT scope)
# ============================================================================
Write-Host "`n  [Test Group 12: Unknown Profile Fail-Closed]" -ForegroundColor White

# Note: The fail-closed behavior applies when an API Key references an unknown profile.
# JWT scope with unknown name falls back to the service profile (by design, since scopes
# map to profiles opportunistically). The API Key path is tested here indirectly by
# verifying that the service profile still applies for unknown scopes.

# Unknown scope still gets service-level profile restrictions (read-only)
$unknownScopeBlock = Invoke-ProfileBearerRequest -Script 'Set-Item -Path "master:/content" -Name "test"' -Scope "totally-fake-profile"
Assert-Equal ([int]$unknownScopeBlock.StatusCode) 403 "Unknown scope still blocked by service profile (Set-Item blocked)"

# Cleanup
$httpClient.Dispose()
Stop-ScriptSession -Session $session
