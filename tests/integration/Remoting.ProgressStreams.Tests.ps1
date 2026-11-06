# Integration tests for the wait-endpoint stream tee feature.
# Verbose/Information/Progress/Warning records emitted by an -AsJob script
# are observable via Wait-RemoteScriptSession's -On* scriptblock callbacks
# during the long-poll, not just at the end.

# ============================================================================
#  Test Group 1: Happy path - Verbose + Progress callbacks fire incrementally
# ============================================================================
Write-Host "`n  [Test Group 1: Wait-RemoteScriptSession -OnVerbose/-OnProgress]" -ForegroundColor White

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost

$jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
    1..5 | ForEach-Object {
        Write-Verbose "verbose-$_" -Verbose
        Write-Progress -Activity "Indexing" -Status "$_ of 5" -PercentComplete ($_ * 20)
        Start-Sleep -Seconds 1
    }
    Write-Information "info-final" -InformationAction Continue
    Write-Warning "warn-final"
    "DONE"
} -AsJob -Raw

Assert-NotNull $jobId "AsJob returned a job id"

$verboseRecords     = New-Object System.Collections.Generic.List[object]
$progressRecords    = New-Object System.Collections.Generic.List[object]
$informationRecords = New-Object System.Collections.Generic.List[object]
$warningRecords     = New-Object System.Collections.Generic.List[object]

$sw = [System.Diagnostics.Stopwatch]::StartNew()
$output = Wait-RemoteScriptSession -Session $session -Id $jobId -WaitTimeoutSeconds 30 `
    -OnVerbose     { param($r) $verboseRecords.Add($r) } `
    -OnProgress    { param($r) $progressRecords.Add($r) } `
    -OnInformation { param($r) $informationRecords.Add($r) } `
    -OnWarning     { param($r) $warningRecords.Add($r) }
$sw.Stop()
Stop-ScriptSession -Session $session -ErrorAction SilentlyContinue

# Output stream still drains at end via Receive-ScriptSession.
Assert-True ($output -match "DONE") "Wait returned the job's Output stream content"

# All 5 verbose records observed, in order.
Assert-Equal $verboseRecords.Count 5 "Five verbose records received"
for ($i = 0; $i -lt 5; $i++) {
    Assert-Equal ([string]$verboseRecords[$i].Message) ("verbose-" + ($i + 1)) "verbose-$($i+1) message matches"
}

# All 5 progress records observed with correct PercentComplete.
Assert-Equal $progressRecords.Count 5 "Five progress records received"
for ($i = 0; $i -lt 5; $i++) {
    Assert-Equal ([int]$progressRecords[$i].PercentComplete) (($i + 1) * 20) "progress[$i] PercentComplete matches"
    Assert-Equal ([string]$progressRecords[$i].Activity) "Indexing" "progress[$i] Activity matches"
}

Assert-Equal $informationRecords.Count 1 "One information record received"
Assert-True (([string]$informationRecords[0].Message) -match "info-final") "information record message matches"

Assert-Equal $warningRecords.Count 1 "One warning record received"
Assert-True (([string]$warningRecords[0].Message) -match "warn-final") "warning record message matches"

# ============================================================================
#  Test Group 2: Cursor tampering and cross-session use rejected (HTTP 400)
# ============================================================================
Write-Host "`n  [Test Group 2: Cursor Tampering and Cross-Session]" -ForegroundColor White

$cursorHandler = New-Object System.Net.Http.HttpClientHandler
if ($PSVersionTable.PSVersion.Major -ge 6) {
    $cursorHandler.ServerCertificateCustomValidationCallback = [System.Net.Http.HttpClientHandler]::DangerousAcceptAnyServerCertificateValidator
}
$cursorClient = New-Object System.Net.Http.HttpClient($cursorHandler)
$jwtCursor = New-Jwt -Algorithm HS256 -Issuer 'SPE Remoting' -Audience $protocolHost -Name 'sitecore\admin' -SecretKey $sharedSecret
$cursorClient.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $jwtCursor)

$session2 = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost
$jobId2 = Invoke-RemoteScript -Session $session2 -ScriptBlock {
    Write-Verbose "v" -Verbose
    Start-Sleep -Seconds 2
    "ok"
} -AsJob -Raw

# Forged cursor: shape is base64url(payload).base64url(sig) but the sig wasn't
# computed with the server's per-app-domain key. Must 400 invalid_cursor.
$forgedPayload = '{"s":"' + $jobId2 + '","o":0}'
$forgedB64     = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($forgedPayload)).TrimEnd('=').Replace('+','-').Replace('/','_')
$forgedCursor  = "$forgedB64.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"

$forgedUrl = "$protocolHost/-/script/wait/?sessionId=$($session2.SessionId)&jobId=$([uri]::EscapeDataString($jobId2))&jobType=scriptsession&timeoutSeconds=5&cursor=$([uri]::EscapeDataString($forgedCursor))"
$forgedResp = $cursorClient.GetAsync($forgedUrl).Result
Assert-Equal ([int]$forgedResp.StatusCode) 400 "Forged cursor returns HTTP 400"

# Get a real cursor by reading from offset 0 against the running session.
$realUrl = "$protocolHost/-/script/wait/?sessionId=$($session2.SessionId)&jobId=$([uri]::EscapeDataString($jobId2))&jobType=scriptsession&timeoutSeconds=5&cursor="
$realResp = $cursorClient.GetAsync($realUrl).Result
Assert-Equal ([int]$realResp.StatusCode) 200 "Real cursor request returns HTTP 200"
$realBody = $realResp.Content.ReadAsStringAsync().Result | ConvertFrom-Json
Assert-NotNull $realBody.cursor "Server issued a cursor on cursor-less read"
$validCursorForJob2 = [string]$realBody.cursor

# Reuse that valid cursor against a DIFFERENT jobId. Cursor encodes session id;
# server must verify both signature AND session id match. Cross-session 400.
$session2b = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost
$jobId2b = Invoke-RemoteScript -Session $session2b -ScriptBlock {
    Start-Sleep -Seconds 2
    "ok2"
} -AsJob -Raw
$crossUrl = "$protocolHost/-/script/wait/?sessionId=$($session2b.SessionId)&jobId=$([uri]::EscapeDataString($jobId2b))&jobType=scriptsession&timeoutSeconds=5&cursor=$([uri]::EscapeDataString($validCursorForJob2))"
$crossResp = $cursorClient.GetAsync($crossUrl).Result
Assert-Equal ([int]$crossResp.StatusCode) 400 "Cursor for job A rejected against job B"

# Clean up.
Wait-RemoteScriptSession -Session $session2  -Id $jobId2  -WaitTimeoutSeconds 10 | Out-Null
Wait-RemoteScriptSession -Session $session2b -Id $jobId2b -WaitTimeoutSeconds 10 | Out-Null
Stop-ScriptSession -Session $session2  -ErrorAction SilentlyContinue
Stop-ScriptSession -Session $session2b -ErrorAction SilentlyContinue
$cursorClient.Dispose()

# ============================================================================
#  Test Group 4: Rate cap drops records and surfaces droppedCount to client
# ============================================================================
Write-Host "`n  [Test Group 4: Rate Cap and droppedCount]" -ForegroundColor White

$session4 = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost

# Emit 1000 verbose records as fast as possible. The ring's 100 records/sec
# token bucket (with 100-record burst) drops the rest; client sees droppedCount.
$jobId4 = Invoke-RemoteScript -Session $session4 -ScriptBlock {
    1..1000 | ForEach-Object { Write-Verbose "v-$_" -Verbose }
    "BURST_DONE"
} -AsJob -Raw

$received = New-Object System.Collections.Generic.List[object]
$gotDropWarning = $false
$output = Wait-RemoteScriptSession -Session $session4 -Id $jobId4 -WaitTimeoutSeconds 30 `
    -OnVerbose { param($r) $received.Add($r) } -WarningAction SilentlyContinue -WarningVariable wv
if ($wv) { $gotDropWarning = ($wv -join "`n") -match "dropped" }
Stop-ScriptSession -Session $session4 -ErrorAction SilentlyContinue

Assert-True ($output -match "BURST_DONE") "Burst job completed"
# The burst pushes more than the token budget allows; the client should see the
# dropped-count warning AND received fewer than 1000 records.
Assert-True ($received.Count -lt 1000) "Received fewer records than emitted (rate cap dropped some)"
Assert-True $gotDropWarning "Client surfaced a 'dropped' warning when records were rate-capped"
# Drop reason should be attributed to the rate cap specifically (not the size
# cap), since each verbose record is tiny but the burst is fast.
$gotRateCapAttribution = ($wv | ForEach-Object { [string]$_ }) -match "rate cap"
Assert-True $gotRateCapAttribution "Drop warning identifies the rate cap"

# ============================================================================
#  Test Group 5: -OnError streams non-terminating errors mid-flight
# ============================================================================
Write-Host "`n  [Test Group 5: Wait-RemoteScriptSession -OnError]" -ForegroundColor White

$session5 = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost

# Two non-terminating Write-Error calls interleaved with a verbose marker so we
# can assert the unified Sequence ordering across error and verbose streams.
$jobId5 = Invoke-RemoteScript -Session $session5 -ScriptBlock {
    Write-Verbose "before-error" -Verbose
    Write-Error -Message "first non-terminating" -ErrorId "TestErr.First" -Category ObjectNotFound
    Start-Sleep -Milliseconds 200
    Write-Error -Message "second non-terminating" -ErrorId "TestErr.Second" -Category InvalidArgument
    Write-Verbose "after-error" -Verbose
    "ERR_DONE"
} -AsJob -Raw

$errorRecords   = New-Object System.Collections.Generic.List[object]
$verboseRecords5 = New-Object System.Collections.Generic.List[object]
$output5 = Wait-RemoteScriptSession -Session $session5 -Id $jobId5 -WaitTimeoutSeconds 30 `
    -OnVerbose { param($r) $verboseRecords5.Add($r) } `
    -OnError   { param($r) $errorRecords.Add($r) }
Stop-ScriptSession -Session $session5 -ErrorAction SilentlyContinue

Assert-True ($output5 -match "ERR_DONE") "Error-emitting job completed"
Assert-Equal $errorRecords.Count 2 "Two non-terminating errors received via -OnError"
Assert-True (([string]$errorRecords[0].Message) -match "first non-terminating") "First error message body matches"
Assert-True (([string]$errorRecords[0].FullyQualifiedErrorId) -match "TestErr.First") "First error FullyQualifiedErrorId carries the ErrorId"
Assert-True (([string]$errorRecords[0].CategoryInfo) -match "ObjectNotFound") "First error CategoryInfo carries the category"
Assert-True (([string]$errorRecords[1].FullyQualifiedErrorId) -match "TestErr.Second") "Second error FullyQualifiedErrorId carries the ErrorId"
# Sequence is monotonic across all streams: verbose-before < err-1 < err-2 < verbose-after.
Assert-True ($verboseRecords5.Count -ge 2) "Both verbose markers received"
Assert-True ([long]$verboseRecords5[0].Sequence -lt [long]$errorRecords[0].Sequence) "verbose-before precedes first error in unified sequence"
Assert-True ([long]$errorRecords[1].Sequence  -lt [long]$verboseRecords5[$verboseRecords5.Count-1].Sequence) "Second error precedes verbose-after in unified sequence"

# ============================================================================
#  Test Group 6: -Cursor / -OnCursor resume picks up where a prior wait left off
# ============================================================================
Write-Host "`n  [Test Group 6: Cursor Resume]" -ForegroundColor White

$session6 = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost

# Six verbose records spaced so the long-poll batches at least twice. The
# script-session record buffer is read non-destructively, so a follow-up wait
# against the same session can replay or resume from any prior cursor as long
# as the session has not been stopped.
$jobId6 = Invoke-RemoteScript -Session $session6 -ScriptBlock {
    1..6 | ForEach-Object {
        Write-Verbose "v-$_" -Verbose
        Start-Sleep -Milliseconds 250
    }
    "RESUME_DONE"
} -AsJob -Raw

$firstPass     = New-Object System.Collections.Generic.List[object]
$cursorsSeen   = New-Object System.Collections.Generic.List[string]
$output6 = Wait-RemoteScriptSession -Session $session6 -Id $jobId6 -WaitTimeoutSeconds 30 `
    -OnVerbose { param($r) $firstPass.Add($r) } `
    -OnCursor  { param($c) $cursorsSeen.Add($c) }

Assert-True ($output6 -match "RESUME_DONE") "First-pass wait completed"
Assert-Equal $firstPass.Count 6 "First pass observed all six records"
Assert-True ($cursorsSeen.Count -ge 2) "OnCursor advanced at least twice during first pass"

# Pick the earliest captured cursor and resume from it. The number of records
# already covered by that cursor is timing-dependent (one or two long-poll
# iterations may have batched records before the first OnCursor fired), so
# assert by sequence rather than by exact count.
$resumeCursor = $cursorsSeen[0]
$firstCursorAdvanceSeq = [long]$firstPass[$firstPass.Count-1].Sequence
for ($i = 0; $i -lt $firstPass.Count; $i++) {
    # The first cursor advance covers records [0..k]; record k is the last one
    # whose sequence is <= the cursor's encoded offset. We don't know k without
    # decoding the (signed, opaque) cursor, so we just bracket it: at least the
    # first record was covered.
    if ([long]$firstPass[$i].Sequence -ge 0) { $firstCursorAdvanceSeq = [long]$firstPass[$i].Sequence; break }
}

$resumed = New-Object System.Collections.Generic.List[object]
$resumedOutput = Wait-RemoteScriptSession -Session $session6 -Id $jobId6 -WaitTimeoutSeconds 10 `
    -Cursor $resumeCursor `
    -OnVerbose { param($r) $resumed.Add($r) }

# Resume must yield strictly fewer records than the full first pass and every
# record must have a sequence strictly greater than the first-pass minimum
# (i.e. we did not replay records already covered by $resumeCursor).
Assert-True ($resumed.Count -lt $firstPass.Count) "Resume returned fewer records than full first pass"
if ($resumed.Count -gt 0) {
    Assert-True ([long]$resumed[0].Sequence -gt [long]$firstPass[0].Sequence) "Resumed records start after the first-pass head"
}

Stop-ScriptSession -Session $session6 -ErrorAction SilentlyContinue
