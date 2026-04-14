# AuditLevel-ManualTest.ps1
# Cycles through all 8 audit test API keys, sending both an allowed and blocked command.
# Review SPE.log after running to confirm expected audit events per level.

Import-Module SPE -Force

$baseUrl = "https://spe.dev.local"

$keys = @(
    @{ Name = "Unrestricted-None";       AccessKeyId = "spe_audit_unrestricted_none";       Secret = "AuditTest-Unrestricted-None-Secret!LongEnough";       Restricted = $false }
    @{ Name = "Restricted-None";         AccessKeyId = "spe_audit_restricted_none";         Secret = "AuditTest-Restricted-None-Secret!LongEnough";         Restricted = $true }
    @{ Name = "Unrestricted-Violations"; AccessKeyId = "spe_audit_unrestricted_violations"; Secret = "AuditTest-Unrestricted-Violations-Secret!LongEnough"; Restricted = $false }
    @{ Name = "Restricted-Violations";   AccessKeyId = "spe_audit_restricted_violations";   Secret = "AuditTest-Restricted-Violations-Secret!LongEnough";   Restricted = $true }
    @{ Name = "Unrestricted-Standard";   AccessKeyId = "spe_audit_unrestricted_standard";   Secret = "AuditTest-Unrestricted-Standard-Secret!LongEnough";   Restricted = $false }
    @{ Name = "Restricted-Standard";     AccessKeyId = "spe_audit_restricted_standard";     Secret = "AuditTest-Restricted-Standard-Secret!LongEnough";     Restricted = $true }
    @{ Name = "Unrestricted-Full";       AccessKeyId = "spe_audit_unrestricted_full";       Secret = "AuditTest-Unrestricted-Full-Secret!LongEnough";       Restricted = $false }
    @{ Name = "Restricted-Full";         AccessKeyId = "spe_audit_restricted_full";         Secret = "AuditTest-Restricted-Full-Secret!LongEnough";         Restricted = $true }
)

# ============================================================================
# Test phase: use AccessKeyId-based authentication
# ============================================================================
foreach ($key in $keys) {
    Write-Host "`n=== $($key.Name) ===" -ForegroundColor Cyan

    $session = New-ScriptSession -AccessKeyId $key.AccessKeyId -SharedSecret $key.Secret -ConnectionUri $baseUrl

    # 1. Connection test (Standard-gated)
    Write-Host "  Test-RemoteConnection: " -NoNewline
    try {
        $test = Test-RemoteConnection -Session $session
        Write-Host "OK" -ForegroundColor Green
    } catch {
        Write-Host "FAILED - $($_.Exception.Message)" -ForegroundColor Red
    }

    # 2. Allowed command - no piping, stays within allowlist
    Write-Host "  Get-Item (allowed): " -NoNewline
    try {
        $result = Invoke-RemoteScript -Session $session -ScriptBlock { (Get-Item -Path "master:/").Name } -Raw
        Write-Host "$result" -ForegroundColor Green
    } catch {
        Write-Host "FAILED - $($_.Exception.Message)" -ForegroundColor Red
    }

    # 3. Blocked command - only meaningful for restricted policies
    if ($key.Restricted) {
        Write-Host "  Add-Type (blocked): " -NoNewline
        try {
            $result = Invoke-RemoteScript -Session $session -ScriptBlock { Add-Type -AssemblyName System.IO } -Raw -ErrorAction Stop
            Write-Host "UNEXPECTED SUCCESS - $result" -ForegroundColor Yellow
        } catch {
            if ($_.Exception.Message -match "blocked|policy|403") {
                Write-Host "Blocked (expected)" -ForegroundColor Green
            } else {
                Write-Host "ERROR - $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    } else {
        Write-Host "  Add-Type (unrestricted, skip)" -ForegroundColor DarkGray
    }

    Stop-ScriptSession -Session $session
}

Write-Host "`n=== Done ===" -ForegroundColor Cyan
Write-Host "Review SPE.log for audit entries. Expected per level:"
Write-Host "  None:       Only unconditional (requestReceived, auth, apiKeyValidated)"
Write-Host "  Violations: + commandBlocked, scriptRejectedByPolicy"
Write-Host "  Standard:   + scriptStarting, scriptCompleted, connectionTest, sessionCleanup, policyAudit"
Write-Host "  Full:       + scriptDetail (length, languageMode), responseDetail, requestDetail"
