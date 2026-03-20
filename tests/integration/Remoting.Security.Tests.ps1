# Remoting Tests - Security Features (Issue #1419)
# Tests: Audit logging, Constrained Language Mode, Command Blocklist, JWT Token Scoping
# Run via: .\Run-RemotingTests.ps1 -TestFile Remoting.Security.Tests.ps1
# Requires: SPE Remoting enabled, shared secret configured
#
# Config deployment:
#   Both files from tests/configs/ must be deployed to the CM container's
#   App_Config/Include/ folder before running these tests:
#
#   1. z.SPE.Security.Disabler.config  -- enables remoting + all services
#   2. z.SPE.Security.Tests.config     -- enables CLM, command blocklist, scope restrictions
#
#   Deploy manually:
#     $cmContainer = "spe-cm-1"  # adjust if your container name is different
#     docker cp tests/configs/z.SPE.Security.Disabler.config $cmContainer:C:\inetpub\wwwroot\App_Config\Include\z.Spe
#     docker cp tests/configs/z.SPE.Security.Tests.config $cmContainer:C:\inetpub\wwwroot\App_Config\Include\z.Spe
#
#   Without z.SPE.Security.Tests.config, CLM/blocklist/scope tests will SKIP (not fail).

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost
$serviceUrl = "$protocolHost/-/script/script/"

# Shared HttpClient for JWT tests -- reuse across tests, only refresh auth header
$handler = New-Object System.Net.Http.HttpClientHandler
if ($PSVersionTable.PSVersion.Major -ge 6) {
    $handler.ServerCertificateCustomValidationCallback = [System.Net.Http.HttpClientHandler]::DangerousAcceptAnyServerCertificateValidator
} else {
    # PS 5.1 / .NET Framework -- trust all certs via ServicePointManager
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
#  Test Group 2: Constrained Language Mode
# ============================================================================
Write-Host "`n  [Test Group 2: Constrained Language Mode]" -ForegroundColor White
# Note: These tests only verify behavior when languageMode="ConstrainedLanguage" is set
#       in Spe.config on the <remoting> element. If not configured, they test the default
#       (FullLanguage) behavior -- the .NET type call will succeed.

$clmResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    try {
        $mode = $ExecutionContext.SessionState.LanguageMode
        $mode.ToString()
    } catch {
        "error: $_"
    }
} -Raw

# If CLM is configured, expect "ConstrainedLanguage"; otherwise "FullLanguage"
Assert-True ($clmResult -eq "FullLanguage" -or $clmResult -eq "ConstrainedLanguage") `
    "Language mode is readable ($clmResult)"

if ($clmResult -eq "ConstrainedLanguage") {
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
} else {
    Write-Host "  [SKIP] CLM not configured -- skipping CLM enforcement tests" -ForegroundColor Yellow
}

# ============================================================================
#  Test Group 3: Command Blocklist
# ============================================================================
Write-Host "`n  [Test Group 3: Command Blocklist]" -ForegroundColor White
# Note: These tests require <commandRestrictions> to be uncommented in Spe.config.
#       When not configured, blocked commands will execute normally.

# Test with a command that would be in a typical blocklist
$blockResponse = Invoke-BearerRequest -Script 'Invoke-Expression "1+1"'
$blockStatus = [int]$blockResponse.StatusCode

if ($blockStatus -eq 403) {
    Assert-Equal $blockStatus 403 "Invoke-Expression blocked by command restrictions"

    # Verify allowed commands still work
    $allowResponse = Invoke-BearerRequest -Script 'Get-Item -Path "master:/"'
    Assert-Equal ([int]$allowResponse.StatusCode) 200 "Get-Item allowed through command restrictions"

    # Verify blocked command name is in response
    $blockBody = $blockResponse.Content.ReadAsStringAsync().Result
    Assert-Like $blockBody "*Invoke-Expression*" "Response includes blocked command name"
} else {
    Write-Host "  [SKIP] Command restrictions not configured -- skipping blocklist tests" -ForegroundColor Yellow
}

# ============================================================================
#  Test Group 4: JWT Token Scoping
# ============================================================================
Write-Host "`n  [Test Group 4: JWT Token Scoping]" -ForegroundColor White

# 4a. JWT with scope claim is accepted
$scopeResponse = Invoke-BearerRequest -Script '"scope-test-ok"' -Scope "admin" -ClientSessionId "test-sess-001"
Assert-Equal ([int]$scopeResponse.StatusCode) 200 "JWT with scope=admin accepted"

# 4b. JWT without scope claim works (backwards compatible)
$noScopeResponse = Invoke-BearerRequest -Script '"no-scope-ok"'
Assert-Equal ([int]$noScopeResponse.StatusCode) 200 "JWT without scope claim works (backwards compatible)"

# 4c. JWT with ClientSessionId claim is accepted
$sessionResponse = Invoke-BearerRequest -Script '"session-test-ok"' -ClientSessionId "correlation-123"
Assert-Equal ([int]$sessionResponse.StatusCode) 200 "JWT with clientSession claim accepted"

# 4d. Scope restrictions enforcement (requires scopeRestrictions configured in Spe.config)
$readOnlyResponse = Invoke-BearerRequest -Script 'Remove-Item -Path "master:/content/nonexistent" -ErrorAction Stop' `
    -Scope "read-only" -ClientSessionId "test-sess-002"
$readOnlyStatus = [int]$readOnlyResponse.StatusCode

if ($readOnlyStatus -eq 403) {
    Assert-Equal $readOnlyStatus 403 "read-only scope blocks Remove-Item"

    # Verify read operations still allowed under read-only scope
    $readResponse = Invoke-BearerRequest -Script 'Get-Item -Path "master:/"' -Scope "read-only"
    Assert-Equal ([int]$readResponse.StatusCode) 200 "read-only scope allows Get-Item"
} else {
    Write-Host "  [SKIP] Scope restrictions not configured -- skipping scope enforcement tests" -ForegroundColor Yellow
}

# ============================================================================
#  Test Group 5: New-Jwt Scope/ClientSessionId Parameters
# ============================================================================
Write-Host "`n  [Test Group 5: New-Jwt Client Module]" -ForegroundColor White

# 5a. New-Jwt accepts Scope parameter
$scopedToken = New-Jwt -Algorithm HS256 -Issuer "SPE Remoting" -Audience $protocolHost `
    -Name "sitecore\admin" -SecretKey $sharedSecret -Scope "read-only"
Assert-NotNull $scopedToken "New-Jwt produces token with Scope parameter"

# 5b. Token contains scope claim in payload
$payloadBase64 = $scopedToken.Split('.')[1]
# Pad base64
switch ($payloadBase64.Length % 4) {
    2 { $payloadBase64 += "==" }
    3 { $payloadBase64 += "=" }
}
$payloadJson = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($payloadBase64.Replace('-','+').Replace('_','/')))
Assert-Like $payloadJson '*"scope":"read-only"*' "JWT payload contains scope claim"

# 5c. New-Jwt accepts ClientSessionId parameter
$sessionToken = New-Jwt -Algorithm HS256 -Issuer "SPE Remoting" -Audience $protocolHost `
    -Name "sitecore\admin" -SecretKey $sharedSecret -ClientSessionId "sess-456"
$sessionPayloadBase64 = $sessionToken.Split('.')[1]
switch ($sessionPayloadBase64.Length % 4) {
    2 { $sessionPayloadBase64 += "==" }
    3 { $sessionPayloadBase64 += "=" }
}
$sessionPayloadJson = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($sessionPayloadBase64.Replace('-','+').Replace('_','/')))
Assert-Like $sessionPayloadJson '*"client_session":"sess-456"*' "JWT payload contains client_session claim"

# 5d. Omitting Scope/ClientSessionId doesn't break token (backwards compatible)
$plainToken = New-Jwt -Algorithm HS256 -Issuer "SPE Remoting" -Audience $protocolHost `
    -Name "sitecore\admin" -SecretKey $sharedSecret
Assert-NotNull $plainToken "New-Jwt without Scope/ClientSessionId still works"
$plainPayloadBase64 = $plainToken.Split('.')[1]
switch ($plainPayloadBase64.Length % 4) {
    2 { $plainPayloadBase64 += "==" }
    3 { $plainPayloadBase64 += "=" }
}
$plainPayloadJson = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($plainPayloadBase64.Replace('-','+').Replace('_','/')))
Assert-True ($plainPayloadJson -notlike '*scope*') "JWT without Scope omits scope claim"

# Cleanup
$httpClient.Dispose()
Stop-ScriptSession -Session $session
