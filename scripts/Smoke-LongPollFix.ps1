param(
    [string]$ConnectionUri = "https://spe.dev.local"
)

$ErrorActionPreference = "Stop"

. "$PSScriptRoot\..\scripts\assert-prerequisites.ps1"
Import-Module "$PSScriptRoot\..\modules\SPE\SPE.psd1" -Force

$secret = Get-EnvValue "SPE_SHARED_SECRET"

Write-Host "Waiting for SPE remoting to come back after deploy..." -ForegroundColor Cyan
$deadline = (Get-Date).AddSeconds(120)
$ready = $false
while ((Get-Date) -lt $deadline) {
    try {
        $probe = New-ScriptSession -Username "sitecore\admin" -SharedSecret $secret -ConnectionUri $ConnectionUri
        $mode = Invoke-RemoteScript -Session $probe -ScriptBlock {
            $ExecutionContext.SessionState.LanguageMode.ToString()
        } -Raw -ErrorAction Stop
        Stop-ScriptSession -Session $probe -ErrorAction SilentlyContinue
        if ($mode) {
            Write-Host "  SPE remoting ready (LanguageMode=$mode)." -ForegroundColor Green
            $ready = $true
            break
        }
    } catch {
        Write-Host "  not ready yet: $($_.Exception.Message)" -ForegroundColor Yellow
        Start-Sleep -Seconds 5
    }
}
if (-not $ready) { throw "SPE remoting did not come ready within 120s." }

Write-Host "`n=== Running the long-running session example ===" -ForegroundColor Magenta

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $secret -ConnectionUri $ConnectionUri

$jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
    1..6 | ForEach-Object {
        Write-Output "Step $_ at $([DateTime]::Now.ToString('HH:mm:ss'))"
        Start-Sleep -Seconds 5
    }
    "Done after 30s"
} -AsJob

Write-Host "Job id: $jobId"

$sw = [System.Diagnostics.Stopwatch]::StartNew()
$captured = $null
Wait-RemoteScriptSession -Session $session -Id $jobId -Delay 5 -Verbose 4>&1 |
    Tee-Object -Variable captured | Out-Null
$sw.Stop()

Stop-ScriptSession -Session $session -ErrorAction SilentlyContinue

$verbose = ($captured | Where-Object { $_ -is [System.Management.Automation.VerboseRecord] } | ForEach-Object { $_.Message })
$warnings = ($captured | Where-Object { $_ -is [System.Management.Automation.WarningRecord] } | ForEach-Object { $_.Message })
$output = ($captured | Where-Object {
    -not ($_ -is [System.Management.Automation.VerboseRecord]) -and
    -not ($_ -is [System.Management.Automation.WarningRecord]) -and
    -not ($_ -is [System.Management.Automation.ErrorRecord]) -and
    -not ($_ -is [System.Management.Automation.DebugRecord]) -and
    -not ($_ -is [System.Management.Automation.InformationRecord])
} | ForEach-Object { [string]$_ })

Write-Host "`n--- Wait elapsed: $([math]::Round($sw.Elapsed.TotalSeconds,1))s ---" -ForegroundColor Cyan

Write-Host "`n--- Verbose stream (last 8 lines) ---" -ForegroundColor Cyan
$verbose | Select-Object -Last 8 | ForEach-Object { Write-Host "  $_" }

if ($warnings) {
    Write-Host "`n--- Warnings ---" -ForegroundColor Yellow
    $warnings | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
} else {
    Write-Host "`n--- Warnings: none ---" -ForegroundColor Green
}

Write-Host "`n--- Job output ---" -ForegroundColor Cyan
$output | ForEach-Object { Write-Host "  $_" }

$failed = $false
if ($warnings -match "HttpError_403") { Write-Host "`nFAIL: 403 surfaced from long-poll." -ForegroundColor Red; $failed = $true }
if ($warnings -match "session-not-owned") { Write-Host "FAIL: session-not-owned restriction." -ForegroundColor Red; $failed = $true }
if (-not ($output -match "Done after 30s")) { Write-Host "FAIL: job output missing." -ForegroundColor Red; $failed = $true }

if ($failed) { exit 1 } else { Write-Host "`nPASS." -ForegroundColor Green }
