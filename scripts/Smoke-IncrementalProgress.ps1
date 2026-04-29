param(
    [string]$ConnectionUri = "https://spe.dev.local"
)

$ErrorActionPreference = "Stop"

. "$PSScriptRoot\..\scripts\assert-prerequisites.ps1"
Import-Module "$PSScriptRoot\..\modules\SPE\SPE.psd1" -Force

$secret = Get-EnvValue "SPE_SHARED_SECRET"
$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $secret -ConnectionUri $ConnectionUri

# 18-second job that emits Verbose + Progress every 3s, plus an Information
# and a Warning at the end. The wait cmdlet's -On* scriptblocks fire as each
# record arrives over the long-poll, not at the end.
$jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
    1..6 | ForEach-Object {
        Write-Host "Working on $($_)"
        Write-Verbose "Step $_ at $([DateTime]::Now.ToString('HH:mm:ss'))" -Verbose
        Write-Progress -Activity "Indexing" -Status "$_ of 6" -PercentComplete ([int](($_ / 6) * 100))
        Start-Sleep -Seconds 3
    }
    Write-Information "all six steps complete" -InformationAction Continue
    Write-Warning "this is a demo warning"
    "Indexed 6 items"
} -AsJob

Write-Host "Job id: $jobId" -ForegroundColor Gray
Write-Host "---- BEGIN streamed progress ----" -ForegroundColor Cyan

$start = [datetime]::Now
$output = Wait-RemoteScriptSession -Session $session -Id $jobId -WaitTimeoutSeconds 30 `
    -OnVerbose {
        param($r)
        $elapsed = [int]([datetime]::Now - $start).TotalSeconds
        Write-Host ("[t+{0:D2}s] [verbose] {1}" -f $elapsed, $r.Message)
    } `
    -OnProgress {
        param($r)
        $elapsed = [int]([datetime]::Now - $start).TotalSeconds
        Write-Host ("[t+{0:D2}s] [progress] {1} - {2}%" -f $elapsed, $r.Activity, $r.PercentComplete)
    } `
    -OnInformation {
        param($r)
        $elapsed = [int]([datetime]::Now - $start).TotalSeconds
        Write-Host ("[t+{0:D2}s] [info] {1}" -f $elapsed, $r.Message) -ForegroundColor Cyan
    } `
    -OnWarning {
        param($r)
        $elapsed = [int]([datetime]::Now - $start).TotalSeconds
        Write-Host ("[t+{0:D2}s] [warn] {1}" -f $elapsed, $r.Message) -ForegroundColor Yellow
    }

$elapsed = [int]([datetime]::Now - $start).TotalSeconds
Write-Host ("[t+{0:D2}s] [output] {1}" -f $elapsed, $output) -ForegroundColor Green
Write-Host "---- END streamed progress ----" -ForegroundColor Cyan

Stop-ScriptSession -Session $session -ErrorAction SilentlyContinue
