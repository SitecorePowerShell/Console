param(
    [string]$ConnectionUri = "https://spe.dev.local",
    [string]$TestFile
)

$ErrorActionPreference = "Stop"

# Load shared helpers (Invoke-HttpCheck, Get-EnvValue, etc.)
. "$PSScriptRoot\..\..\scripts\assert-prerequisites.ps1"

$moduleRoot = "$PSScriptRoot\..\..\modules\SPE"
Import-Module "$moduleRoot\SPE.psd1" -Force

# Load test framework (reuse from SPE/Tests/)
. "$PSScriptRoot\..\unit\TestRunner.ps1"

$global:protocolHost = $ConnectionUri
$global:sharedSecret = Get-EnvValue "SPE_SHARED_SECRET"

# Wait for Sitecore to be ready (may still be restarting after task deploy)
Write-Host "Waiting for Sitecore CM to be ready..." -ForegroundColor Cyan
$cmHost = Get-CmHost
$deadline = (Get-Date).AddSeconds(180)
while ((Get-Date) -lt $deadline) {
    $status = Invoke-HttpCheck -Uri "https://$cmHost/sitecore/login" -TimeoutMs 10000
    if ($status -and $status -ge 200 -and $status -lt 400) {
        Write-Host "  Sitecore CM is ready (HTTP $status)." -ForegroundColor Green
        break
    }
    Write-Host "  Not ready yet (HTTP $status) -- retrying in 5s..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5
}
if (-not $status -or $status -lt 200 -or $status -ge 400) {
    Write-Host "ERROR: Sitecore CM did not become ready within 180s." -ForegroundColor Red
    exit 1
}

# -- Helpers ------------------------------------------------------------------

function Wait-SitecoreRestart {
    param(
        [int]$MaxWaitSeconds = 120,
        [int]$PollIntervalSeconds = 5,
        [string]$ExpectedLanguageMode
    )

    Write-Host "`n  Waiting for Sitecore to restart after config deployment..." -ForegroundColor Cyan

    # Initial pause: give Sitecore's file watcher time to detect the config change
    # and begin the app domain recycle. Without this, the first probe can hit the
    # old (still-running) app domain and falsely report "ready."
    Start-Sleep -Seconds 10

    $deadline = (Get-Date).AddSeconds($MaxWaitSeconds)
    while ((Get-Date) -lt $deadline) {
        # Probe SPE remoting directly -- not /sitecore/login, which responds before
        # SPE module pipelines are fully initialized.
        try {
            $probeSession = New-ScriptSession -Username "sitecore\admin" `
                -SharedSecret $global:sharedSecret -ConnectionUri $global:protocolHost
            $probeResult = Invoke-RemoteScript -Session $probeSession -ScriptBlock {
                $ExecutionContext.SessionState.LanguageMode.ToString()
            } -Raw 2>$null
            Stop-ScriptSession -Session $probeSession -ErrorAction SilentlyContinue

            if ($probeResult -and ($probeResult -eq "FullLanguage" -or $probeResult -eq "ConstrainedLanguage")) {
                # If caller expects a specific language mode, keep waiting until it matches.
                # This confirms the new config has actually taken effect.
                if ($ExpectedLanguageMode -and $probeResult -ne $ExpectedLanguageMode) {
                    Write-Host "  SPE responding but language mode is '$probeResult', waiting for '$ExpectedLanguageMode'..." -ForegroundColor Yellow
                    Start-Sleep -Seconds $PollIntervalSeconds
                    continue
                }

                Write-Host "  SPE Remoting is ready (language mode: $probeResult)." -ForegroundColor Green
                return
            }
        } catch {
            # Connection failed or SPE not ready yet -- expected during restart
        }

        Write-Host "  Not ready yet -- retrying in ${PollIntervalSeconds}s..." -ForegroundColor Yellow
        Start-Sleep -Seconds $PollIntervalSeconds
    }

    Write-Host "ERROR: SPE Remoting did not become ready within ${MaxWaitSeconds}s." -ForegroundColor Red
    exit 1
}

function Deploy-TestConfigs {
    param([string]$ConfigDir)

    $cmContainer = "spe-cm-1"
    $targetPath = "C:/inetpub/wwwroot/App_Config/Include/z.Spe"

    $configs = Get-ChildItem -Path $ConfigDir -Filter "*.config" -ErrorAction SilentlyContinue
    if (-not $configs) {
        Write-Host "  No configs found in $ConfigDir" -ForegroundColor Yellow
        return $false
    }

    Write-Host "`n  Deploying configs from $ConfigDir..." -ForegroundColor Cyan
    foreach ($f in $configs) {
        Write-Host "    -> $($f.Name)" -ForegroundColor Gray
        docker cp $f.FullName "${cmContainer}:${targetPath}/$($f.Name)"
        if ($LASTEXITCODE -ne 0) {
            Write-Host "ERROR: Failed to deploy $($f.Name) to container." -ForegroundColor Red
            exit 1
        }
    }
    return $true
}

function Remove-TestConfigs {
    param([string]$ConfigDir)

    $cmContainer = "spe-cm-1"
    $targetPath = "C:/inetpub/wwwroot/App_Config/Include/z.Spe"

    $configs = Get-ChildItem -Path $ConfigDir -Filter "*.config" -ErrorAction SilentlyContinue
    if (-not $configs) { return }

    Write-Host "`n  Removing test configs..." -ForegroundColor Cyan
    foreach ($f in $configs) {
        Write-Host "    x  $($f.Name)" -ForegroundColor Gray
        docker exec $cmContainer powershell -Command "Remove-Item -Path '${targetPath}/$($f.Name)' -Force -ErrorAction SilentlyContinue" 2>$null
    }
}

# -- Config paths -------------------------------------------------------------

$configRoot = "$PSScriptRoot\..\configs"
$testConfigDir = Join-Path $configRoot "test"

# -- Single-file mode ---------------------------------------------------------

if ($TestFile) {
    $path = Resolve-Path -Path (Join-Path $PSScriptRoot $TestFile) -ErrorAction SilentlyContinue
    if (-not $path) { $path = Resolve-Path -Path $TestFile }
    Invoke-TestFile $path
    Show-TestSummary
    return
}

# -- Full test run: two phases ------------------------------------------------

# Phase 1: Baseline tests (no security config)
# Deploy configs (tests/configs/deploy/) are handled by the Taskfile (task deploy:configs).
# Here we only ensure leftover security test configs from a previous run are removed.
Write-Host "`n=== Phase 1: Baseline Tests (no security enforcement config) ===" -ForegroundColor Magenta
Remove-TestConfigs -ConfigDir $testConfigDir
Remove-TestConfigs -ConfigDir (Join-Path $configRoot "profiles")

# Wait for SPE to be ready with FullLanguage (no security enforcement config present).
# Covers both fresh starts and removal of leftover security configs from previous runs.
Wait-SitecoreRestart -ExpectedLanguageMode "FullLanguage"

# Detect language mode for skip guards in non-security test files
$probeSession = New-ScriptSession -Username "sitecore\admin" -SharedSecret $global:sharedSecret -ConnectionUri $ConnectionUri
$global:serverLanguageMode = Invoke-RemoteScript -Session $probeSession -ScriptBlock {
    $ExecutionContext.SessionState.LanguageMode.ToString()
} -Raw 2>$null
$global:isConstrainedLanguage = $global:serverLanguageMode -eq "ConstrainedLanguage"
Stop-ScriptSession -Session $probeSession

if ($global:isConstrainedLanguage) {
    Write-Host "`n  Server is in ConstrainedLanguage mode -- tests requiring FullLanguage will be skipped" -ForegroundColor Yellow
}

# Run all test files EXCEPT enforced/profiles/DA (those need setup/teardown) and maintenance
Get-ChildItem "$PSScriptRoot\*.Tests.ps1" -Exclude "Remoting.Maintenance.Tests.ps1","Remoting.Security.Enforced.Tests.ps1","Remoting.RestrictionProfiles.Tests.ps1","Remoting.SoapProfiles.Tests.ps1","Remoting.DelegatedAccess.Tests.ps1" |
    ForEach-Object { Invoke-TestFile $_.FullName }

# Phase 1b: Delegated access tests (setup -> test -> teardown, no config deploy needed)
Write-Host "`n=== Phase 1b: Delegated Access Tests ===" -ForegroundColor Magenta
. "$PSScriptRoot\Remoting.DelegatedAccess.Setup.ps1"
Invoke-TestFile "$PSScriptRoot\Remoting.DelegatedAccess.Tests.ps1"
. "$PSScriptRoot\Remoting.DelegatedAccess.Teardown.ps1"

# Phase 2: Security enforcement tests (requires security config)
Write-Host "`n=== Phase 2: Security Enforcement Tests (deploying security config) ===" -ForegroundColor Magenta
$deployedSecurity = Deploy-TestConfigs -ConfigDir $testConfigDir
if ($deployedSecurity) {
    Wait-SitecoreRestart -ExpectedLanguageMode "ConstrainedLanguage"
}

Invoke-TestFile "$PSScriptRoot\Remoting.Security.Enforced.Tests.ps1"

# Cleanup Phase 2: remove security test configs before Phase 3
Remove-TestConfigs -ConfigDir $testConfigDir

# Phase 3: Restriction profile tests (requires profile config)
Write-Host "`n=== Phase 3: Restriction Profile Tests (deploying profile config) ===" -ForegroundColor Magenta

# Wait for Phase 2 cleanup to take effect (FullLanguage restored)
Wait-SitecoreRestart -ExpectedLanguageMode "FullLanguage"

# Setup: create override test items while remoting is still unrestricted
. "$PSScriptRoot\Remoting.RestrictionProfiles.Setup.ps1"

# Deploy profile config and wait for restart
$profileConfigDir = Join-Path $configRoot "profiles"
$deployedProfiles = Deploy-TestConfigs -ConfigDir $profileConfigDir
if ($deployedProfiles) {
    Wait-SitecoreRestart -ExpectedLanguageMode "ConstrainedLanguage"
}

Invoke-TestFile "$PSScriptRoot\Remoting.RestrictionProfiles.Tests.ps1"
Invoke-TestFile "$PSScriptRoot\Remoting.SoapProfiles.Tests.ps1"

# Cleanup: remove profile test configs first
Remove-TestConfigs -ConfigDir $profileConfigDir

# Wait for unrestricted mode to restore before teardown
Wait-SitecoreRestart -ExpectedLanguageMode "FullLanguage"

# Teardown: remove override test items (requires unrestricted remoting)
. "$PSScriptRoot\Remoting.RestrictionProfiles.Teardown.ps1"

Write-Host "`n  Profile test cleanup complete." -ForegroundColor Cyan

Show-TestSummary
