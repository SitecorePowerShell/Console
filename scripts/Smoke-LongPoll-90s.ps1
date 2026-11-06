param(
    [string]$ConnectionUri = "https://spe.dev.local"
)

$ErrorActionPreference = "Stop"

. "$PSScriptRoot\..\scripts\assert-prerequisites.ps1"
Import-Module "$PSScriptRoot\..\modules\SPE\SPE.psd1" -Force

$secret = Get-EnvValue "SPE_SHARED_SECRET"

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $secret -ConnectionUri $ConnectionUri

Write-Host "Launching 90s job (no -Verbose on the wait)..." -ForegroundColor Cyan
$jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
    1..9 | ForEach-Object {
        Write-Output "Step $_ at $([DateTime]::Now.ToString('HH:mm:ss'))"
        Start-Sleep -Seconds 10
    }
    "Done after 90s"
} -AsJob

Write-Host "Job id: $jobId" -ForegroundColor Gray
Write-Host "---- BEGIN raw cmdlet output (no -Verbose) ----" -ForegroundColor Cyan
$sw = [System.Diagnostics.Stopwatch]::StartNew()
Wait-RemoteScriptSession -Session $session -Id $jobId
$sw.Stop()
Write-Host "---- END raw cmdlet output ----" -ForegroundColor Cyan
Write-Host "Wall clock: $([math]::Round($sw.Elapsed.TotalSeconds,1))s" -ForegroundColor Gray

Stop-ScriptSession -Session $session -ErrorAction SilentlyContinue
