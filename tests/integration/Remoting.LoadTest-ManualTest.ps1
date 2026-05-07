# Remoting.LoadTest-ManualTest.ps1
#
# Manual benchmark/soak script. NOT wired into Run-RemotingTests.ps1.
#
# Drives concurrent traffic against:
#   single   = POST /-/script/script/   (Invoke-RemoteScript -Raw)
#   longpoll = POST /-/script/script/?... -AsJob, then GET /-/script/wait/
#             (Wait-RemoteScriptSession with -OnInformation)
#   mixed    = alternates the two per worker iteration
#
# Outcomes the script asserts per iteration:
#   single   : response body equals 'OK'
#   longpoll : every Write-Information record reached the client AND
#              the wait cmdlet emitted no 403/session-not-owned warning
#
# Aggregates count, success/failure split, latency p50/p95/p99/max, and
# throughput per mode. Writes a CSV alongside the script for diffing runs.
#
# What this script does NOT cover (deliberate, to keep it tight):
#   - drop-counter shape on every wait response (see Remoting.WaitDropCounters.Tests)
#   - cursor-resume integrity across worker restarts
#   - audit-log content (see Remoting.AuditSanitization.Tests pattern)
# Add those if a regression points at them.
#
# Usage examples:
#   .\Remoting.LoadTest-ManualTest.ps1 -ConcurrentClients 16 -DurationSeconds 60
#   .\Remoting.LoadTest-ManualTest.ps1 -Mode longpoll -ConcurrentClients 32 -JobDurationSeconds 3
#   .\Remoting.LoadTest-ManualTest.ps1 -Mode single   -ConcurrentClients 8  -ReuseSession

[CmdletBinding()]
param(
    [string]$ConnectionUri = "https://spe.dev.local",

    [ValidateRange(1, 256)]
    [int]$ConcurrentClients = 8,

    [ValidateRange(5, 7200)]
    [int]$DurationSeconds = 30,

    # Linear ramp-up so all workers don't punch the auth path on the same tick.
    [ValidateRange(0, 600)]
    [int]$RampSeconds = 5,

    [ValidateSet('single', 'longpoll', 'mixed')]
    [string]$Mode = 'mixed',

    # Per-iteration script body sleeps this long. Sets the latency floor for
    # 'single' and the streams-flush window for 'longpoll'.
    [ValidateRange(0, 30)]
    [int]$JobDurationSeconds = 1,

    # Number of Write-Information records the longpoll job emits. Each record
    # forces a stream-buffer enqueue + cursor advance on the server.
    [ValidateRange(0, 200)]
    [int]$StreamRecordsPerJob = 5,

    # Server-side hold for /-/script/wait/. Short values cycle the long-poll
    # loop more often (more pressure on the wait endpoint).
    [ValidateRange(1, 60)]
    [int]$WaitTimeoutSeconds = 10,

    # If set, each worker keeps a single session for its lifetime instead of
    # creating + tearing down per iteration. Use to amortize the JWT-mint /
    # auth-cache cost and isolate the pure script-execution path.
    [switch]$ReuseSession,

    # Error-rate threshold above which the script returns exit code 1.
    [ValidateRange(0.0, 1.0)]
    [double]$ErrorRateThreshold = 0.01,

    # Time to wait for in-flight iterations to finish after the steady-state
    # window closes. Workers can be deep inside a Wait-RemoteScriptSession at
    # deadline (server hold + 5s slack); without an upper bound the drain
    # looks like a hang. After this grace expires we forcibly stop the
    # runspace so the script can return its summary.
    [ValidateRange(5, 600)]
    [int]$DrainGraceSeconds = 30
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
. "$repoRoot\scripts\assert-prerequisites.ps1"

$modulePath = "$repoRoot\modules\SPE\SPE.psd1"
Import-Module $modulePath -Force

$sharedSecret = Get-EnvValue "SPE_SHARED_SECRET"
if ([string]::IsNullOrWhiteSpace($sharedSecret)) {
    throw "SPE_SHARED_SECRET is not set. Run 'task init' first."
}

# Pre-flight CM probe so we don't burn ramp-up against a dead container.
$health = Invoke-HttpCheck -Uri "$ConnectionUri/sitecore/login" -TimeoutMs 10000
if (-not ($health -and $health -ge 200 -and $health -lt 400)) {
    throw "CM at $ConnectionUri returned HTTP $health on /sitecore/login. Bring it up with 'task up' first."
}

Write-Host "`n=== SPE Load Test ===" -ForegroundColor Cyan
Write-Host ("  Target           : {0}" -f $ConnectionUri)
Write-Host ("  Mode             : {0}" -f $Mode)
Write-Host ("  Workers          : {0}" -f $ConcurrentClients)
Write-Host ("  Ramp / Steady    : {0}s / {1}s" -f $RampSeconds, $DurationSeconds)
Write-Host ("  Job duration     : {0}s" -f $JobDurationSeconds)
if ($Mode -ne 'single') {
    Write-Host ("  Stream records   : {0}/job" -f $StreamRecordsPerJob)
    Write-Host ("  Wait timeout     : {0}s" -f $WaitTimeoutSeconds)
}
Write-Host ("  Reuse session    : {0}" -f $ReuseSession.IsPresent)
Write-Host ""

# -----------------------------------------------------------------------------
# Worker scriptblock. Lives inside a runspace from the pool.
# -----------------------------------------------------------------------------
$workerScript = {
    param(
        [string]$ConnectionUri,
        [string]$SharedSecret,
        [string]$Mode,
        [int]$JobDurationSeconds,
        [int]$StreamRecordsPerJob,
        [int]$WaitTimeoutSeconds,
        [bool]$ReuseSession,
        [datetime]$EndUtc,
        [int]$WorkerId,
        [int]$RampSeconds,
        [int]$ConcurrentClients,
        [string]$ModulePath,
        $Samples
    )

    Import-Module $ModulePath -Force

    # All time arithmetic is in UTC. Get-Date returns local-Kind DateTime;
    # comparing it to a UTC-Kind DateTime via -lt uses Ticks regardless of
    # Kind, so on a non-UTC system the worker would see the deadline as
    # hours in the future and never exit.
    if ($RampSeconds -gt 0) {
        $offsetSec = ($WorkerId / [double]$ConcurrentClients) * $RampSeconds
        $startAt = [datetime]::UtcNow.AddSeconds($offsetSec)
        while ([datetime]::UtcNow -lt $startAt -and [datetime]::UtcNow -lt $EndUtc) {
            Start-Sleep -Milliseconds 100
        }
    }

    $persistentSession = $null
    if ($ReuseSession) {
        $persistentSession = New-ScriptSession -Username 'sitecore\admin' `
            -SharedSecret $SharedSecret -ConnectionUri $ConnectionUri
    }

    $singleScript = [scriptblock]::Create("Start-Sleep -Seconds $JobDurationSeconds; 'OK'")

    $perRecordSleepMs = if ($StreamRecordsPerJob -gt 0) {
        [int](($JobDurationSeconds * 1000) / $StreamRecordsPerJob)
    } else { 0 }
    $longpollScript = [scriptblock]::Create(@"
for (`$i = 0; `$i -lt $StreamRecordsPerJob; `$i++) {
    Write-Information "tick `$i" -InformationAction Continue
    Start-Sleep -Milliseconds $perRecordSleepMs
}
'OK'
"@)

    $iter = 0
    while ([datetime]::UtcNow -lt $EndUtc) {
        $iter++

        $resolvedMode = switch ($Mode) {
            'mixed' { if ($iter % 2 -eq 0) { 'longpoll' } else { 'single' } }
            default { $Mode }
        }

        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $status = 'unknown'
        $errMsg = $null
        $session = $persistentSession

        try {
            if (-not $session) {
                $session = New-ScriptSession -Username 'sitecore\admin' `
                    -SharedSecret $SharedSecret -ConnectionUri $ConnectionUri
            }

            switch ($resolvedMode) {
                'single' {
                    $result = Invoke-RemoteScript -Session $session -ScriptBlock $singleScript -Raw
                    if ([string]$result -eq 'OK') {
                        $status = 'success'
                    } else {
                        $status = 'badResponse'
                        $errMsg = "got=$result"
                    }
                }
                'longpoll' {
                    $jobId = Invoke-RemoteScript -Session $session -ScriptBlock $longpollScript -AsJob -Raw
                    if (-not $jobId) {
                        $status = 'noJobId'
                        break
                    }

                    # Closure over $infoCounter so the OnInformation callback
                    # mutates the property (member access goes through the
                    # setter, not script-scope variable assignment).
                    $infoCounter = [pscustomobject]@{ N = 0 }
                    $onInfo = { param($r) $infoCounter.N++ }.GetNewClosure()

                    $waitWarnings = $null
                    Wait-RemoteScriptSession -Session $session -Id $jobId `
                        -WaitTimeoutSeconds $WaitTimeoutSeconds `
                        -OnInformation $onInfo `
                        -WarningVariable +waitWarnings `
                        -ErrorAction SilentlyContinue *>&1 | Out-Null

                    $warnText = if ($waitWarnings) { ($waitWarnings | ForEach-Object { [string]$_ }) -join ' | ' } else { '' }
                    if ($warnText -match 'HttpError_403|session-not-owned') {
                        $status = 'ownership403'
                        $errMsg = $warnText
                    } elseif ($warnText -match 'HttpError_') {
                        $status = 'httpError'
                        $errMsg = $warnText
                    } elseif ($infoCounter.N -lt $StreamRecordsPerJob) {
                        $status = 'partialStreams'
                        $errMsg = "got=$($infoCounter.N) expected>=$StreamRecordsPerJob"
                    } else {
                        $status = 'success'
                    }
                }
            }
        }
        catch {
            $status = 'exception'
            $errMsg = "$($_.Exception.GetType().Name): $($_.Exception.Message)"
        }
        finally {
            $sw.Stop()
            if (-not $ReuseSession -and $session) {
                Stop-ScriptSession -Session $session -ErrorAction SilentlyContinue
            }
        }

        $null = $Samples.Add([PSCustomObject]@{
            WorkerId   = $WorkerId
            Iteration  = $iter
            Mode       = $resolvedMode
            Status     = $status
            DurationMs = [long]$sw.Elapsed.TotalMilliseconds
            FinishedAt = [datetime]::UtcNow
            Error      = $errMsg
        })
    }

    if ($persistentSession) {
        Stop-ScriptSession -Session $persistentSession -ErrorAction SilentlyContinue
    }
}

# -----------------------------------------------------------------------------
# Drive the workers via a runspace pool. ConcurrentBag is thread-safe and the
# pool's runspaces share its reference (no serialization across boundaries).
# -----------------------------------------------------------------------------
$samples = New-Object 'System.Collections.Concurrent.ConcurrentBag[psobject]'
$iss = [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault()
$pool = [runspacefactory]::CreateRunspacePool(1, $ConcurrentClients, $iss, $Host)
$pool.Open()

$startUtc = (Get-Date).ToUniversalTime()
$endUtc   = $startUtc.AddSeconds($RampSeconds + $DurationSeconds)

$handles = @()
for ($w = 0; $w -lt $ConcurrentClients; $w++) {
    $ps = [powershell]::Create()
    $ps.RunspacePool = $pool
    $null = $ps.AddScript($workerScript).
        AddParameter('ConnectionUri',        $ConnectionUri).
        AddParameter('SharedSecret',         $sharedSecret).
        AddParameter('Mode',                 $Mode).
        AddParameter('JobDurationSeconds',   $JobDurationSeconds).
        AddParameter('StreamRecordsPerJob',  $StreamRecordsPerJob).
        AddParameter('WaitTimeoutSeconds',   $WaitTimeoutSeconds).
        AddParameter('ReuseSession',         [bool]$ReuseSession.IsPresent).
        AddParameter('EndUtc',               $endUtc).
        AddParameter('WorkerId',             $w).
        AddParameter('RampSeconds',          $RampSeconds).
        AddParameter('ConcurrentClients',    $ConcurrentClients).
        AddParameter('ModulePath',           $modulePath).
        AddParameter('Samples',              $samples)

    $handles += [PSCustomObject]@{
        WorkerId    = $w
        PowerShell  = $ps
        AsyncResult = $ps.BeginInvoke()
    }
}

# Live tick: total + per-second delta until the steady-state window closes.
$lastTotal = 0
while ((Get-Date).ToUniversalTime() -lt $endUtc) {
    Start-Sleep -Seconds 1
    $now = (Get-Date).ToUniversalTime()
    $elapsed = [int]($now - $startUtc).TotalSeconds
    $total = $samples.Count
    $delta = $total - $lastTotal
    $lastTotal = $total
    Write-Host ("  t={0,5}s  total={1,7}  +{2,4}/s" -f $elapsed, $total, $delta)
}

# Drain runspaces. Workers exit on their own at $EndUtc, but a worker can
# be inside a Wait-RemoteScriptSession at that moment - those iterations
# must finish their bounded HTTP wait before the runspace becomes idle. Poll
# for completion with a grace window; any worker still running past it gets
# a forced Stop() so EndInvoke returns instead of blocking the script.
$drainDeadline = (Get-Date).AddSeconds($DrainGraceSeconds)
Write-Host ""
while ($true) {
    $pending = @($handles | Where-Object { -not $_.AsyncResult.IsCompleted })
    if ($pending.Count -eq 0) { break }
    if ((Get-Date) -ge $drainDeadline) { break }
    $remaining = [int]($drainDeadline - (Get-Date)).TotalSeconds
    Write-Host ("  draining: {0} worker(s) still running, {1}s grace remaining" -f $pending.Count, $remaining) -ForegroundColor DarkGray
    Start-Sleep -Milliseconds 500
}

foreach ($h in $handles) {
    if (-not $h.AsyncResult.IsCompleted) {
        Write-Warning ("Worker {0} did not drain within {1}s; forcing stop." -f $h.WorkerId, $DrainGraceSeconds)
        try { $h.PowerShell.Stop() } catch { }
    }
    try { $null = $h.PowerShell.EndInvoke($h.AsyncResult) }
    catch { Write-Warning "Worker $($h.WorkerId) raised: $($_.Exception.Message)" }
    $h.PowerShell.Dispose()
}
$pool.Close()
$pool.Dispose()

# -----------------------------------------------------------------------------
# Report.
# -----------------------------------------------------------------------------
function Get-Percentile {
    param([long[]]$Values, [double]$P)
    if (-not $Values -or $Values.Count -eq 0) { return 0 }
    $sorted = $Values | Sort-Object
    $rank = [Math]::Min($sorted.Count - 1, [int][Math]::Ceiling(($P / 100.0) * $sorted.Count) - 1)
    if ($rank -lt 0) { $rank = 0 }
    return $sorted[$rank]
}

$all = $samples.ToArray()
$wallSeconds = ((Get-Date).ToUniversalTime() - $startUtc).TotalSeconds

Write-Host "`n=== Results ===" -ForegroundColor Cyan
Write-Host ("  Total requests : {0}" -f $all.Count)
Write-Host ("  Wall time      : {0:N1}s" -f $wallSeconds)
Write-Host ("  Throughput     : {0:N2} req/s" -f ($all.Count / [Math]::Max($wallSeconds, 0.001)))

$exitCode = 0
foreach ($g in ($all | Group-Object Mode)) {
    $items = $g.Group
    $success = ($items | Where-Object { $_.Status -eq 'success' }).Count
    $failure = $items.Count - $success
    $durations = @($items | ForEach-Object { [long]$_.DurationMs })
    $p50 = Get-Percentile -Values $durations -P 50
    $p95 = Get-Percentile -Values $durations -P 95
    $p99 = Get-Percentile -Values $durations -P 99
    $max = ($durations | Measure-Object -Maximum).Maximum

    Write-Host ("`n  [Mode: {0}]" -f $g.Name) -ForegroundColor White
    Write-Host ("    count                 : {0}" -f $items.Count)
    Write-Host ("    success / failure     : {0} / {1}" -f $success, $failure)
    Write-Host ("    p50/p95/p99/max (ms)  : {0,6}/{1,6}/{2,6}/{3,6}" -f $p50, $p95, $p99, $max)

    if ($failure -gt 0) {
        $items | Where-Object { $_.Status -ne 'success' } |
            Group-Object Status |
            Sort-Object Count -Descending |
            ForEach-Object { Write-Host ("      {0,-18}: {1}" -f $_.Name, $_.Count) -ForegroundColor Yellow }
        $items | Where-Object { $_.Error } | Select-Object -First 3 | ForEach-Object {
            Write-Host ("      sample[{0}]: {1}" -f $_.Status, $_.Error) -ForegroundColor DarkYellow
        }
    }

    $errorRate = if ($items.Count -eq 0) { 0 } else { $failure / [double]$items.Count }
    if ($errorRate -gt $ErrorRateThreshold) {
        Write-Host ("    ERROR RATE {0:P2} EXCEEDS THRESHOLD {1:P2}" -f $errorRate, $ErrorRateThreshold) -ForegroundColor Red
        $exitCode = 1
    }
}

# Persist raw samples for post-run analysis / diffing across runs.
$outDir = Join-Path $PSScriptRoot "_loadtest-results"
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$csvPath = Join-Path $outDir ("loadtest-{0}-{1}-c{2}-{3}.csv" -f $Mode, $stamp, $ConcurrentClients, $DurationSeconds)
$all | Sort-Object FinishedAt | Export-Csv -Path $csvPath -NoTypeInformation
Write-Host ("`n  Raw samples : {0}" -f $csvPath) -ForegroundColor DarkGray

exit $exitCode
