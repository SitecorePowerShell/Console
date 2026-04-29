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
