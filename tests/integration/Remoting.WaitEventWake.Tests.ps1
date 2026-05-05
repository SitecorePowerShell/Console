# Integration test: event-driven wake on the long-poll wait endpoint.
#
# Today the wait endpoint uses a fixed 200ms server-side polling tick
# (RemoteScriptCall.ashx.cs:~1026). When a script emits stream records
# faster than 200ms apart, the tick batches them into one response.
# After the event-driven wake change, each emit signals the wait loop
# immediately and produces a one-record response.
#
# Test shape: emit 6 verbose records 100ms apart, then drive the wait
# endpoint directly via HttpClient (the higher-level Wait-RemoteScriptSession
# cmdlet hides per-response batch shape; Invoke-RemoteWait is loaded but
# not exported by the SPE module). Compare avg-batch-size against a
# threshold that reliably separates polling (avg ~2.0) from event-driven
# (avg ~1.0).

Write-Host "`n  [Wait Endpoint Event-Driven Wake]" -ForegroundColor White

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost

$jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
    # Initial pause: gives the wait endpoint time to register on the
    # server before the first record lands. Otherwise the burst can
    # complete before the first wait request arrives, collapsing all
    # six records into a single post-loop final read.
    Start-Sleep -Milliseconds 500
    1..6 | ForEach-Object {
        Start-Sleep -Milliseconds 100
        # Warning (not Verbose): default $VerbosePreference is
        # SilentlyContinue in the AsJob runspace, so Write-Verbose
        # records are suppressed before reaching ps.Streams.Verbose
        # and never enter StreamRing.
        Write-Warning "MARK_$_"
    }
    # Keep the job busy after the last emit so the wait loop returns
    # because of stream records, not because of isDone.
    Start-Sleep -Seconds 3
} -AsJob -Raw

Assert-NotNull $jobId "AsJob returned a job id"

# The Wait endpoint requires a sessionId query-param matching the caller's
# ScriptSession identity for ownership. New-ScriptSession buried the id in
# $session.SessionId; LongPollWait Group 2 reaches in the same way.
$callerSessionId = $session.SessionId

$handler = New-Object System.Net.Http.HttpClientHandler
if ($PSVersionTable.PSVersion.Major -ge 6) {
    $handler.ServerCertificateCustomValidationCallback = [System.Net.Http.HttpClientHandler]::DangerousAcceptAnyServerCertificateValidator
}
$hc = New-Object System.Net.Http.HttpClient($handler)
$hc.Timeout = [TimeSpan]::FromSeconds(15)

$cursor = ''
$batchSizes = @()
$sw = [System.Diagnostics.Stopwatch]::StartNew()

# Drive the wait endpoint until we have collected all 6 records or hit
# an 8s safety deadline (verbose emit window is ~600ms; 8s is generous
# even on slow CI containers).
while ((($batchSizes | Measure-Object -Sum).Sum -lt 6) -and ($sw.Elapsed.TotalSeconds -lt 8)) {
    # Mint a fresh JWT per call: tokens have a 30s lifetime and we can
    # absorb that into one bearer here, but minting per call also matches
    # how the production client behaves.
    $jwt = New-Jwt -Algorithm HS256 -Issuer 'SPE Remoting' -Audience $protocolHost -Name 'sitecore\admin' -SecretKey $sharedSecret
    $hc.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $jwt)

    $url = "$protocolHost/-/script/wait/?sessionId=$callerSessionId&jobId=$([uri]::EscapeDataString($jobId))&jobType=scriptsession&timeoutSeconds=2"
    if ($cursor) {
        $url += "&cursor=$([uri]::EscapeDataString($cursor))"
    } else {
        $url += "&cursor="
    }

    $resp = $hc.GetAsync($url).Result
    $statusCode = [int]$resp.StatusCode
    $rawBody = $resp.Content.ReadAsStringAsync().Result

    if ($statusCode -ne 200) {
        Write-Host "  Wait failed with HTTP $statusCode body=$rawBody -- aborting trial" -ForegroundColor Yellow
        break
    }

    $body = $rawBody | ConvertFrom-Json

    # PowerShell unwraps single-element arrays when they pass through a
    # PSObject property; @($body.streams).Count is the only reliable shape.
    $recordCount = 0
    if ($body.PSObject.Properties['streams']) {
        $recordCount = @($body.streams).Count
    }
    if ($recordCount -gt 0) {
        $batchSizes += $recordCount
    }
    # Always advance cursor when the server returns one, even on empty
    # responses (the cursor reflects the high-watermark of producedSoFar).
    if ($body.PSObject.Properties['cursor'] -and $body.cursor) {
        $cursor = [string]$body.cursor
    }
    # If the job is reported done and we have all records, exit early.
    if ($body.isDone -and (($batchSizes | Measure-Object -Sum).Sum -ge 6)) { break }
}
$sw.Stop()

$hc.Dispose()
Stop-ScriptSession -Session $session -ErrorAction SilentlyContinue

$totalRecords = ($batchSizes | Measure-Object -Sum).Sum
$batchCount   = $batchSizes.Count
$avgBatchSize = if ($batchCount -gt 0) { [double]$totalRecords / [double]$batchCount } else { 0 }

Write-Host ("  Batches: {0}, total records: {1}, avg batch size: {2:F2}" -f $batchCount, $totalRecords, $avgBatchSize)
Write-Host ("  Batch sizes: {0}" -f ($batchSizes -join ', '))

Assert-True ($totalRecords -ge 5) "Wait endpoint delivered all (or near all) emitted verbose records ($totalRecords/6)"

# Threshold 1.3 separates polling (observed avg ~1.5 in red-phase trials:
# pattern [1, 1, 2, 2] = 1.50) from event-driven (expected avg ~1.0
# because each emit signals the wait loop immediately). Heuristic.
# Re-tune if the 100ms emit interval or the 200ms server tick changes.
Assert-True ($avgBatchSize -lt 1.3) "Average wait-response batch size under 1.3 (event-driven wake; current 200ms polling produces ~1.5)"

# Structural complement to the avg-size threshold: 6 records emitted at 100ms
# intervals produce >= 5 distinct batches under event-driven wake (each emit
# wakes the loop on its own round-trip; one occasional merge is acceptable).
# 200ms server tick polling collapses adjacent 100ms emits into pairs and
# typically produces <= 4 batches. The two assertions are deliberately
# overlapping - if scheduler jitter pushes the avg to 1.4 on a slow CI box,
# the batch count still pins down whether the wake mechanism is firing.
Assert-True ($batchCount -ge 5) "Distinct wait-response batches >= 5 (event-driven wake; polling produces <= 4 from a 6-record / 100ms / 200ms-tick burst)"

# ============================================================================
#  Audit-shape assertions for the request/auth/script-execute trio.
#
#  Catches regressions in the audit-correlation surface that no other test
#  in the suite verifies (sessionId propagation, endpoint= field,
#  remotingClient/Kid pair, scriptStarting visibility). The wake change
#  itself doesn't depend on these, but the audit-correlation work landed on
#  the same branch and lacked direct coverage; without these assertions a
#  future refactor could silently strip a field, rename an action, or
#  re-introduce the kid empty-string bug, and every existing integration
#  test would still pass.
# ============================================================================
Write-Host "`n  [Audit Shape (correlation by sessionId)]" -ForegroundColor White

# Tail the most recent SPE.log via docker exec - matches the pattern used
# elsewhere in Run-RemotingTests.ps1 for container interaction. Read the
# whole file because line 4 onwards may be elsewhere on disk depending on
# rotation; filter by sessionId to scope to this test's calls.
$auditRaw = docker exec spe-cm-1 powershell -NoProfile -Command "Get-ChildItem C:/inetpub/wwwroot/App_Data/logs/SPE.log.*.txt | Sort-Object LastWriteTime -Descending | Select-Object -First 1 | ForEach-Object { Get-Content `$_.FullName }"
$mySessionLines = @($auditRaw | Where-Object { $_ -match "sessionId=$callerSessionId" })

Write-Host ("  Audit lines correlated by sessionId={0}: {1}" -f $callerSessionId, $mySessionLines.Count)

# AsJob POST + Stop-ScriptSession cleanup = at least 5 audit lines
# (requestReceived, authenticated, scriptStarting, scriptCompleted for the
# AsJob; requestReceived, authenticated, sessionCleanup for cleanup). Wait
# endpoint hits are silent at default Standard auditing.
Assert-True ($mySessionLines.Count -ge 5) "Audit log carries at least 5 lines correlated by sessionId (got $($mySessionLines.Count))"

$requestReceived = $mySessionLines | Where-Object { $_ -match 'action=requestReceived' } | Select-Object -First 1
Assert-NotNull $requestReceived "requestReceived audit fires for the test session"
Assert-True ($requestReceived -match 'endpoint=script') "requestReceived carries endpoint=script"

$authenticated = $mySessionLines | Where-Object { $_ -match 'action=authenticated' } | Select-Object -First 1
Assert-NotNull $authenticated "authenticated audit fires for the test session"
Assert-True ($authenticated -match 'remotingClient=') "authenticated carries remotingClient field"
Assert-True ($authenticated -match 'remotingClientKid=\S') "remotingClientKid is non-empty (regression guard for kid empty-string bug)"

$scriptStarting = $mySessionLines | Where-Object { $_ -match 'action=scriptStarting' } | Select-Object -First 1
Assert-NotNull $scriptStarting "scriptStarting audit fires for the test session (regression guard for the GetSession-before-scriptStarting cold-start gap)"

$sessionCleanup = $mySessionLines | Where-Object { $_ -match 'action=sessionCleanup' } | Select-Object -First 1
Assert-NotNull $sessionCleanup "sessionCleanup audit fires after Stop-ScriptSession (regression guard for the OAuth-bearer cleanup AccessToken propagation)"
