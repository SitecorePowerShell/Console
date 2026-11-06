param(
    [string]$ConnectionUri = "https://spe.dev.local"
)

$ErrorActionPreference = "Stop"

. "$PSScriptRoot\..\scripts\assert-prerequisites.ps1"
Import-Module "$PSScriptRoot\..\modules\SPE\SPE.psd1" -Force

$secret = Get-EnvValue "SPE_SHARED_SECRET"
$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $secret -ConnectionUri $ConnectionUri

# Warm the instance before the real test. Right after `task deploy` the
# auth pipeline and Sitecore Jobs dispatcher both lag by a few seconds;
# without warmup the first AsJob's wait races the cold Job runner and
# the smoke fails on a perfectly healthy build.
Write-Host "Warming Sitecore connection (auth + Jobs dispatcher)..." -ForegroundColor Cyan
Wait-RemoteConnection -Session $session
Write-Host "  Connection ready." -ForegroundColor Green

# Job emits all five stream kinds plus an Output return value. The wait
# cmdlet's -On* scriptblocks fire as each record arrives over the long-poll,
# not at the end.
$jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
    1..6 | ForEach-Object {
        Write-Host "Working on $($_) at $([DateTime]::Now.ToString('HH:mm:ss'))"
        Start-Sleep -Milliseconds 500
        Write-Verbose "Step $_ at $([DateTime]::Now.ToString('HH:mm:ss'))" -Verbose
        Write-Progress -Activity "Indexing" -Status "$_ of 6" -PercentComplete ([int](($_ / 6) * 100))
        if ($_ -eq 3) {
            Write-Error -Message "demo non-terminating error at step 3" -ErrorId "Demo.Step3" -Category InvalidOperation
        }
        Start-Sleep -Seconds 1
    }
    Write-Information "all six steps complete" -InformationAction Continue
    Write-Warning "this is a demo warning"
    "Indexed 6 items"
} -AsJob

Write-Host "Job id: $jobId" -ForegroundColor Gray
Write-Host "---- BEGIN streamed progress ----" -ForegroundColor Cyan

$verboseSeen     = New-Object System.Collections.Generic.List[object]
$progressSeen    = New-Object System.Collections.Generic.List[object]
$informationSeen = New-Object System.Collections.Generic.List[object]
$warningSeen     = New-Object System.Collections.Generic.List[object]
$errorSeen       = New-Object System.Collections.Generic.List[object]

$start = [datetime]::Now
$output = Wait-RemoteScriptSession -Session $session -Id $jobId -WaitTimeoutSeconds 30 `
    -OnVerbose {
        param($r)
        $elapsed = [int]([datetime]::Now - $start).TotalSeconds
        Write-Host ("[t+{0:D2}s] [verbose] {1}" -f $elapsed, $r.Message)
        $verboseSeen.Add($r)
    } `
    -OnProgress {
        param($r)
        $elapsed = [int]([datetime]::Now - $start).TotalSeconds
        Write-Host ("[t+{0:D2}s] [progress] {1} - {2}%" -f $elapsed, $r.Activity, $r.PercentComplete)
        $progressSeen.Add($r)
    } `
    -OnInformation {
        param($r)
        $elapsed = [int]([datetime]::Now - $start).TotalSeconds
        Write-Host ("[t+{0:D2}s] [info] {1}" -f $elapsed, $r.Message) -ForegroundColor Cyan
        $informationSeen.Add($r)
    } `
    -OnWarning {
        param($r)
        $elapsed = [int]([datetime]::Now - $start).TotalSeconds
        Write-Host ("[t+{0:D2}s] [warn] {1}" -f $elapsed, $r.Message) -ForegroundColor Yellow
        $warningSeen.Add($r)
    } `
    -OnError {
        param($r)
        $elapsed = [int]([datetime]::Now - $start).TotalSeconds
        Write-Host ("[t+{0:D2}s] [err]  {1} ({2})" -f $elapsed, $r.Message, $r.FullyQualifiedErrorId) -ForegroundColor Red
        $errorSeen.Add($r)
    }

$elapsed = [int]([datetime]::Now - $start).TotalSeconds
Write-Host ("[t+{0:D2}s] [output] {1}" -f $elapsed, $output) -ForegroundColor Green
Write-Host "---- END streamed progress ----" -ForegroundColor Cyan

Stop-ScriptSession -Session $session -ErrorAction SilentlyContinue

# Fail-loud assertions so this script gives CI a real signal. Each expected
# stream and the script's Output return must show up; otherwise something
# regressed in the long-poll/streams path and we want a non-zero exit code.
$failures = New-Object System.Collections.Generic.List[string]
if ($verboseSeen.Count     -lt 6) { $failures.Add("expected >=6 verbose records, got $($verboseSeen.Count)") }
if ($progressSeen.Count    -lt 6) { $failures.Add("expected >=6 progress records, got $($progressSeen.Count)") }
if ($informationSeen.Count -lt 1) { $failures.Add("expected >=1 information record, got $($informationSeen.Count)") }
if ($warningSeen.Count     -lt 1) { $failures.Add("expected >=1 warning record, got $($warningSeen.Count)") }
if ($errorSeen.Count       -lt 1) { $failures.Add("expected >=1 error record, got $($errorSeen.Count)") }
if ([string]$output -notmatch "Indexed 6 items") { $failures.Add("Output drain missing 'Indexed 6 items'") }

if ($failures.Count -gt 0) {
    Write-Host ""
    Write-Host "SMOKE FAILED:" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host ""
Write-Host "SMOKE OK: verbose=$($verboseSeen.Count) progress=$($progressSeen.Count) info=$($informationSeen.Count) warn=$($warningSeen.Count) err=$($errorSeen.Count)" -ForegroundColor Green
exit 0
