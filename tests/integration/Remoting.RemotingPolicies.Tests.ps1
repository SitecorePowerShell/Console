# Remoting Tests - Remoting Policy Enforcement (Issue #1426)
# Tests that remoting policies (item-based) control language mode and command access.
# Policy items are created by Remoting.RemotingPolicies.Setup.ps1 before these tests run.
# API Keys bind directly to policies via the Policy Droplink field.
# Run via: .\Run-RemotingTests.ps1 (automatically run in the policy phase)
# Requires: Policy items created, SPE Remoting enabled, shared secret configured

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

$readOnlySecret = "Test-ReadOnly-Secret-K3y!-LongEnough-For-Validation"
$readOnlyKeyId = "spe_test_readonly_key_001"
$noPolicySecret = "Test-NoPolicy-Secret-K3y!-LongEnough-For-Validation"
$noPolicyKeyId = "spe_test_nopolicy_key_001"
$standardAuditSecret = "Test-StandardAudit-Secret-K3y!-LongEnough-For-Validation"
$standardAuditKeyId = "spe_test_stdaudit_key_001"
$fullAuditSecret = "Test-FullAudit-Secret-K3y!-LongEnough-For-Validation"
$fullAuditKeyId = "spe_test_fullaudit_key_001"

function Invoke-ApiKeyRequest {
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

function Invoke-LegacyRequest {
    param([string]$Script)
    $jwtParams = @{
        Algorithm = 'HS256'; Issuer = 'SPE Remoting'
        Audience  = $protocolHost; Name = 'sitecore\admin'; SecretKey = $sharedSecret
    }
    $token = New-Jwt @jwtParams

    $httpClient.DefaultRequestHeaders.Authorization = `
        New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token)

    $sid = [guid]::NewGuid().ToString()
    $url = "${serviceUrl}?sessionId=$sid&rawOutput=false&outputFormat=clixml&persistentSession=false"
    $content = New-Object System.Net.Http.StringContent($Script, [System.Text.Encoding]::UTF8, "text/plain")
    return $httpClient.PostAsync($url, $content).Result
}

# ============================================================================
#  Test Group 1: Policy Language Mode
# ============================================================================
Write-Host "`n  [Test Group 1: Policy Language Mode]" -ForegroundColor White

# The Test-ReadOnly policy enforces ConstrainedLanguage (Full Language unchecked)
$langResponse = Invoke-ApiKeyRequest -Script '$ExecutionContext.SessionState.LanguageMode.ToString()' -SecretKey $readOnlySecret -KeyId $readOnlyKeyId
$langBody = $langResponse.Content.ReadAsStringAsync().Result
Assert-Like $langBody "*ConstrainedLanguage*" "Test-ReadOnly policy enforces ConstrainedLanguage"

# ============================================================================
#  Test Group 2: Policy Command Allowlist
# ============================================================================
Write-Host "`n  [Test Group 2: Policy Command Allowlist]" -ForegroundColor White

# 2a. Write commands blocked by read-only policy
$removeResponse = Invoke-ApiKeyRequest -Script 'Remove-Item -Path "master:/content/nonexistent" -ErrorAction Stop' -SecretKey $readOnlySecret -KeyId $readOnlyKeyId
Assert-Equal ([int]$removeResponse.StatusCode) 403 "read-only policy blocks Remove-Item"

$setResponse = Invoke-ApiKeyRequest -Script 'Set-Item -Path "master:/content" -Name "test"' -SecretKey $readOnlySecret -KeyId $readOnlyKeyId
Assert-Equal ([int]$setResponse.StatusCode) 403 "read-only policy blocks Set-Item"

$newResponse = Invoke-ApiKeyRequest -Script 'New-Item -Path "master:/content" -Name "policy-test" -ItemType "Sample/Sample Item"' -SecretKey $readOnlySecret -KeyId $readOnlyKeyId
Assert-Equal ([int]$newResponse.StatusCode) 403 "read-only policy blocks New-Item"

$moveResponse = Invoke-ApiKeyRequest -Script 'Move-Item -Path "master:/content/nonexistent" -Destination "master:/content"' -SecretKey $readOnlySecret -KeyId $readOnlyKeyId
Assert-Equal ([int]$moveResponse.StatusCode) 403 "read-only policy blocks Move-Item"

# 2b. Response body includes blocked command name
$removeBody = $removeResponse.Content.ReadAsStringAsync().Result
Assert-Like $removeBody "*Remove-Item*" "Response includes blocked command name for policy violation"

# 2c. Read commands still allowed
$readResponse = Invoke-ApiKeyRequest -Script 'Get-Item -Path "master:/"' -SecretKey $readOnlySecret -KeyId $readOnlyKeyId
Assert-Equal ([int]$readResponse.StatusCode) 200 "read-only policy allows Get-Item"

$childResponse = Invoke-ApiKeyRequest -Script 'Get-ChildItem -Path "master:/"' -SecretKey $readOnlySecret -KeyId $readOnlyKeyId
Assert-Equal ([int]$childResponse.StatusCode) 200 "read-only policy allows Get-ChildItem"

# ============================================================================
#  Test Group 3: Execution Escape Prevention
# ============================================================================
Write-Host "`n  [Test Group 3: Execution Escape Prevention]" -ForegroundColor White

$iexResponse = Invoke-ApiKeyRequest -Script 'Invoke-Expression "1+1"' -SecretKey $readOnlySecret -KeyId $readOnlyKeyId
Assert-Equal ([int]$iexResponse.StatusCode) 403 "read-only policy blocks Invoke-Expression"

$icmResponse = Invoke-ApiKeyRequest -Script 'Invoke-Command -ScriptBlock { 1 }' -SecretKey $readOnlySecret -KeyId $readOnlyKeyId
Assert-Equal ([int]$icmResponse.StatusCode) 403 "read-only policy blocks Invoke-Command"

$jobResponse = Invoke-ApiKeyRequest -Script 'Start-Job -ScriptBlock { 1 }' -SecretKey $readOnlySecret -KeyId $readOnlyKeyId
Assert-Equal ([int]$jobResponse.StatusCode) 403 "read-only policy blocks Start-Job"

# ============================================================================
#  Test Group 4: Backward Compatibility (Legacy Shared Secret)
# ============================================================================
Write-Host "`n  [Test Group 4: Backward Compatibility]" -ForegroundColor White

# Legacy shared secret without API Key = no policy = unrestricted
$legacyResponse = Invoke-LegacyRequest -Script 'Remove-Item -Path "master:/content/nonexistent" -ErrorAction SilentlyContinue; "OK"'
Assert-Equal ([int]$legacyResponse.StatusCode) 200 "Legacy shared secret is unrestricted (Remove-Item allowed)"

# Pipeline cmdlets work under read-only policy
$formatResponse = Invoke-ApiKeyRequest -Script '@(1, 2, 3) | Where-Object { $_ -gt 1 } | ForEach-Object { $_ * 2 } | Measure-Object -Sum | Select-Object -ExpandProperty Sum' -SecretKey $readOnlySecret -KeyId $readOnlyKeyId
Assert-Equal ([int]$formatResponse.StatusCode) 200 "Pipeline cmdlets work under read-only policy"

# ============================================================================
#  Test Group 5: Policy Enforcement After Exception
# ============================================================================
Write-Host "`n  [Test Group 5: Policy Enforcement After Exception]" -ForegroundColor White

# Execute a script that throws under the policy
$errorResponse = Invoke-ApiKeyRequest -Script 'throw "deliberate-policy-test-error"' -SecretKey $readOnlySecret -KeyId $readOnlyKeyId
$errorStatus = [int]$errorResponse.StatusCode
Assert-True ($errorStatus -eq 424 -or $errorStatus -eq 200) "Script that throws returns status ($errorStatus)"

# Next request should still be constrained by the policy
$postErrorResponse = Invoke-ApiKeyRequest -Script 'Remove-Item -Path "master:/content/nonexistent"' -SecretKey $readOnlySecret -KeyId $readOnlyKeyId
Assert-Equal ([int]$postErrorResponse.StatusCode) 403 "Policy enforcement survives script exception"

# ============================================================================
#  Test Group 6: Dynamic Invocation Rejection
# ============================================================================
Write-Host "`n  [Test Group 6: Dynamic Invocation Rejection]" -ForegroundColor White

# 6a. Variable-based command invocation should be rejected
$dynVarResponse = Invoke-ApiKeyRequest -Script '$cmd = "Remove-Item"; & $cmd "master:/content/nonexistent"' -SecretKey $readOnlySecret -KeyId $readOnlyKeyId
Assert-Equal ([int]$dynVarResponse.StatusCode) 403 "Dynamic invocation via variable is rejected"

# 6b. String expression invocation should be rejected
$dynExprResponse = Invoke-ApiKeyRequest -Script '& ("Remove" + "-Item") "master:/content/nonexistent"' -SecretKey $readOnlySecret -KeyId $readOnlyKeyId
Assert-Equal ([int]$dynExprResponse.StatusCode) 403 "Dynamic invocation via expression is rejected"

# 6c. Static commands still work fine
$staticResponse = Invoke-ApiKeyRequest -Script 'Get-Item -Path "master:/"' -SecretKey $readOnlySecret -KeyId $readOnlyKeyId
Assert-Equal ([int]$staticResponse.StatusCode) 200 "Static command invocation still works"

# ============================================================================
#  Test Group 7: Publish-Item Allowed by Policy
# ============================================================================
Write-Host "`n  [Test Group 7: Publish-Item Allowed by Policy]" -ForegroundColor White

# Publish-Item is in the Test-ReadOnly allowlist
$publishResponse = Invoke-ApiKeyRequest -Script 'Publish-Item -Path "master:/content" -ErrorAction SilentlyContinue; "OK"' -SecretKey $readOnlySecret -KeyId $readOnlyKeyId
Assert-Equal ([int]$publishResponse.StatusCode) 200 "Publish-Item allowed by read-only policy"

# Commands not in the allowlist remain blocked
$configBlockResponse = Invoke-ApiKeyRequest -Script 'Remove-Item -Path "master:/content/nonexistent"' -SecretKey $readOnlySecret -KeyId $readOnlyKeyId
Assert-Equal ([int]$configBlockResponse.StatusCode) 403 "Remove-Item still blocked (not in allowlist)"

# ============================================================================
#  Test Group 8: API Key Without Policy Is Denied
# ============================================================================
Write-Host "`n  [Test Group 8: API Key Without Policy]" -ForegroundColor White

# API Key with no policy is denied (policy required)
$apiKeyNoPolicyResponse = Invoke-ApiKeyRequest -Script 'Get-Item -Path "master:/"' -SecretKey $noPolicySecret -KeyId $noPolicyKeyId
Assert-Equal ([int]$apiKeyNoPolicyResponse.StatusCode) 403 "API Key with no policy is denied"

# ============================================================================
#  Test Group 9: Script Approval (v2 endpoint)
#  The Approved Scripts list controls which Web API scripts can execute via v2.
#  Setup creates:
#    - ApprovedWriteScript (listed in policy's Approved Scripts)
#    - UnapprovedWriteScript (NOT listed -- denied)
# ============================================================================
Write-Host "`n  [Test Group 9: Script Approval (v2 endpoint)]" -ForegroundColor White

# restfulv2 resolves scripts registered under Web API integration points
function Invoke-ScriptItemRequest {
    param(
        [string]$ScriptName,
        [string]$SecretKey = $sharedSecret,
        [string]$KeyId
    )
    $jwtParams = @{
        Algorithm = 'HS256'; Issuer = 'SPE Remoting'
        Audience  = $protocolHost; SecretKey = $SecretKey
    }
    if ($KeyId) { $jwtParams['KeyId'] = $KeyId }
    if (-not $KeyId) { $jwtParams['Name'] = 'sitecore\admin' }
    $token = New-Jwt @jwtParams

    $httpClient.DefaultRequestHeaders.Authorization = `
        New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token)

    $url = "$protocolHost/-/script/v2/master/$ScriptName"
    return $httpClient.GetAsync($url).Result
}

# 9a. Approved script via API Key is permitted
$approvedResponse = Invoke-ScriptItemRequest -ScriptName "Test-ApprovedWriteScript" -SecretKey $readOnlySecret -KeyId $readOnlyKeyId
Assert-Equal ([int]$approvedResponse.StatusCode) 200 "Approved script executes via v2"

# 9b. Approved script respects policy language mode (ConstrainedLanguage under Test-ReadOnly)
$approvedBody = $approvedResponse.Content.ReadAsStringAsync().Result
Assert-Like $approvedBody "*APPROVED_SCRIPT_OK*" "Approved script output confirms execution"

# 9c. Unapproved script is denied via v2
$unapprovedResponse = Invoke-ScriptItemRequest -ScriptName "Test-UnapprovedWriteScript" -SecretKey $readOnlySecret -KeyId $readOnlyKeyId
Assert-Equal ([int]$unapprovedResponse.StatusCode) 403 "Unapproved script denied by policy"

# 9d. Approved script without API Key (legacy secret, no policy) runs unrestricted
$noKeyApprovedResponse = Invoke-ScriptItemRequest -ScriptName "Test-ApprovedWriteScript"
Assert-Equal ([int]$noKeyApprovedResponse.StatusCode) 200 "Approved script without API Key runs unrestricted"

# ============================================================================
#  Test Group 10: Audit Level Policies (Standard and Full)
#  Verifies that scripts execute correctly under Standard and Full audit levels.
#  Audit log output is verified visually or via log analysis, not assertions.
# ============================================================================
Write-Host "`n  [Test Group 10: Audit Level Policies]" -ForegroundColor White

# 10a. Standard audit policy allows FullLanguage execution
$standardResponse = Invoke-ApiKeyRequest -Script '"STANDARD_AUDIT_OK"' -SecretKey $standardAuditSecret -KeyId $standardAuditKeyId
Assert-Equal ([int]$standardResponse.StatusCode) 200 "Standard audit policy allows execution"

# 10b. Full audit policy allows FullLanguage execution
$fullResponse = Invoke-ApiKeyRequest -Script '"FULL_AUDIT_OK"' -SecretKey $fullAuditSecret -KeyId $fullAuditKeyId
Assert-Equal ([int]$fullResponse.StatusCode) 200 "Full audit policy allows execution"

# 10c. Full audit policy with parameters (triggers requestDetail log)
$fullParamResponse = Invoke-ApiKeyRequest -Script 'param($a) $a' -SecretKey $fullAuditSecret -KeyId $fullAuditKeyId
Assert-Equal ([int]$fullParamResponse.StatusCode) 200 "Full audit policy handles parameterized scripts"

# Cleanup
$httpClient.Dispose()
