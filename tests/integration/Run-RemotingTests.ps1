param(
    [string]$ConnectionUri = "https://spe.dev.local",
    [string]$TestFile,
    # Skips Phase 2 (token-lifetime enforcement). That phase deploys a
    # config file, waits for the app-domain recycle, runs
    # Remoting.Security.Enforced.Tests.ps1, removes the config, and
    # waits for another recycle. Combined ~60-90s. Use this flag on
    # iterations that don't touch auth/token-lifetime code.
    [switch]$SkipSecurityEnforcement
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
Get-ChildItem "$PSScriptRoot\*.Tests.ps1" -Exclude "Remoting.Maintenance.Tests.ps1","Remoting.Security.Enforced.Tests.ps1","Remoting.RemotingPolicies.Tests.ps1","Remoting.DelegatedAccess.Tests.ps1","Remoting.Throttle.Tests.ps1","Remoting.Expiration.Tests.ps1","Remoting.Expiration.DuplicateKeyId.Tests.ps1","Remoting.ClientRetry.Tests.ps1","Remoting.LongPollWait.Tests.ps1" |
    ForEach-Object { Invoke-TestFile $_.FullName }

# Phase 1b: Delegated access tests (setup -> test -> teardown, no config deploy needed)
Write-Host "`n=== Phase 1b: Delegated Access Tests ===" -ForegroundColor Magenta
. "$PSScriptRoot\Remoting.DelegatedAccess.Setup.ps1"
Invoke-TestFile "$PSScriptRoot\Remoting.DelegatedAccess.Tests.ps1"
. "$PSScriptRoot\Remoting.DelegatedAccess.Teardown.ps1"

# Phase 2: Token lifetime enforcement tests (requires maxTokenLifetimeSeconds config)
# Skipped under -SkipSecurityEnforcement to avoid the deploy + remove recycles
# (~60-90s combined). That flag is meant for iterations that don't touch auth
# or token-lifetime code.
if ($SkipSecurityEnforcement) {
    Write-Host "`n=== Phase 2: SKIPPED (-SkipSecurityEnforcement) ===" -ForegroundColor Yellow
    Write-Host "  Remoting.Security.Enforced.Tests.ps1 will not run. Use 'task test' for full coverage." -ForegroundColor Yellow
} else {
    Write-Host "`n=== Phase 2: Token Lifetime Tests (deploying auth config) ===" -ForegroundColor Magenta
    $deployedSecurity = Deploy-TestConfigs -ConfigDir $testConfigDir
    if ($deployedSecurity) {
        # Config only sets maxTokenLifetimeSeconds (auth provider level, no language mode change)
        Wait-SitecoreRestart
    }

    Invoke-TestFile "$PSScriptRoot\Remoting.Security.Enforced.Tests.ps1"

    # Cleanup Phase 2: remove test configs before Phase 3
    Remove-TestConfigs -ConfigDir $testConfigDir
}

# Phase 3: Remoting policy tests (item-based, no config deploy needed)
Write-Host "`n=== Phase 3: Remoting Policy Tests ===" -ForegroundColor Magenta

# Wait for config removal to take effect (only meaningful if Phase 2 deployed a
# config that needs to go away).
if (-not $SkipSecurityEnforcement) {
    Wait-SitecoreRestart
}

# Setup: create policy test items
. "$PSScriptRoot\Remoting.RemotingPolicies.Setup.ps1"

# When -SkipSecurityEnforcement suppressed the Phase 2/3 app-domain recycles,
# the ApiScripts cache (30-min sliding TTL, never invalidated by item events)
# still holds its pre-setup snapshot and the new Web API scripts created by
# RemotingPolicies.Setup won't resolve via /-/script/v2/. Force the cache to
# flush so Test Group 9 sees the new items.
if ($SkipSecurityEnforcement) {
    Write-Host "  Flushing ApiScripts cache (no app-domain recycle this run)..." -ForegroundColor Gray
    $flushSession = New-ScriptSession -Username "sitecore\admin" -SharedSecret $global:sharedSecret -ConnectionUri $ConnectionUri
    Invoke-RemoteScript -Session $flushSession -ScriptBlock {
        # Legacy shared key + per-db keys (Remove is a no-op on missing keys)
        [System.Web.HttpRuntime]::Cache.Remove("Spe.ApiScriptsKey") | Out-Null
        [System.Web.HttpRuntime]::Cache.Remove("Spe.ApiScriptsKey:master") | Out-Null
        [System.Web.HttpRuntime]::Cache.Remove("Spe.ApiScriptsKey:core") | Out-Null
        [System.Web.HttpRuntime]::Cache.Remove("Spe.ApiScriptsKey:web") | Out-Null
    } | Out-Null
    Stop-ScriptSession -Session $flushSession -ErrorAction SilentlyContinue
}

# Wait for API Key and policy caches to expire (TTL = AuthorizationCacheExpirationSecs, default 10s)
Write-Host "  Waiting for authorization cache to expire..." -ForegroundColor Gray
Start-Sleep -Seconds 12

# Run policy tests (policies are resolved from items, no restart needed)
Invoke-TestFile "$PSScriptRoot\Remoting.RemotingPolicies.Tests.ps1"

# Long-poll wait + session ownership tests (share Test-ReadOnly API Key from policy setup).
Invoke-TestFile "$PSScriptRoot\Remoting.LongPollWait.Tests.ps1"

# Teardown: remove test policy items
. "$PSScriptRoot\Remoting.RemotingPolicies.Teardown.ps1"

Write-Host "`n  Policy test cleanup complete." -ForegroundColor Cyan

# Phase 4: Throttle action tests (item-based, no config deploy needed)
Write-Host "`n=== Phase 4: Throttle Action Tests ===" -ForegroundColor Magenta

# Setup: create throttle test API Keys
. "$PSScriptRoot\Remoting.Throttle.Setup.ps1"

# Wait for API Key cache to expire (TTL = AuthorizationCacheExpirationSecs, default 10s)
Write-Host "  Waiting for authorization cache to expire..." -ForegroundColor Gray
Start-Sleep -Seconds 12

# Run throttle tests
Invoke-TestFile "$PSScriptRoot\Remoting.Throttle.Tests.ps1"

# Teardown: remove throttle test items
. "$PSScriptRoot\Remoting.Throttle.Teardown.ps1"

Write-Host "`n  Throttle test cleanup complete." -ForegroundColor Cyan

# Phase 5: API Key expiration and duplicate key tests (item-based, no config deploy needed)
Write-Host "`n=== Phase 5: API Key Expiration and Validation Tests ===" -ForegroundColor Magenta

# Setup: create expiration test API Keys
. "$PSScriptRoot\Remoting.Expiration.Setup.ps1"

# Wait for API Key cache to expire (TTL = AuthorizationCacheExpirationSecs, default 10s)
Write-Host "  Waiting for authorization cache to expire..." -ForegroundColor Gray
Start-Sleep -Seconds 12

# Run expiration tests
Invoke-TestFile "$PSScriptRoot\Remoting.Expiration.Tests.ps1"

# Run duplicate key ID tests (creates and cleans up its own items)
Invoke-TestFile "$PSScriptRoot\Remoting.Expiration.DuplicateKeyId.Tests.ps1"

# Teardown: remove expiration test items
. "$PSScriptRoot\Remoting.Expiration.Teardown.ps1"

Write-Host "`n  Expiration test cleanup complete." -ForegroundColor Cyan

# Phase 6: Client-side retry tests (item-based, no config deploy needed)
# Runs AFTER Phase 5 so expired-key item from Expiration.Setup is still available for
# Gap 3's X-SPE-AuthFailureReason=expired check. The Expiration teardown above removes
# the expired key, so re-create it here as part of ClientRetry.Setup.
Write-Host "`n=== Phase 6: Client-Side Retry Tests ===" -ForegroundColor Magenta

# Re-create the expired key for Gap 3 test (Expiration.Teardown removed it above)
. "$PSScriptRoot\Remoting.Expiration.Setup.ps1"

# Setup: create client-retry test API Keys (tight window, disabled key)
. "$PSScriptRoot\Remoting.ClientRetry.Setup.ps1"

Write-Host "  Waiting for authorization cache to expire..." -ForegroundColor Gray
Start-Sleep -Seconds 12

Invoke-TestFile "$PSScriptRoot\Remoting.ClientRetry.Tests.ps1"

# Teardown: remove client-retry test items and the re-created expired key
. "$PSScriptRoot\Remoting.ClientRetry.Teardown.ps1"
. "$PSScriptRoot\Remoting.Expiration.Teardown.ps1"

Write-Host "`n  Client-retry test cleanup complete." -ForegroundColor Cyan

Show-TestSummary
