# Integration tests for the long-poll wait endpoint (#1474).
# Covers:
#   - Wait-RemoteScriptSession end-to-end against -AsJob
#   - Session ownership enforcement (identity A creates, identity B rejected with 403)

$serviceUrl = "$protocolHost/-/script/script/"
$waitUrl    = "$protocolHost/-/script/wait/"

# ============================================================================
#  Test Group 1: Wait-RemoteScriptSession uses long-poll for -AsJob
# ============================================================================
Write-Host "`n  [Test Group 1: Wait-RemoteScriptSession Long-Poll]" -ForegroundColor White

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost

# Launch a short-running job.
$jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
    Start-Sleep -Seconds 2
    "JOB_OK"
} -AsJob -Raw

Assert-NotNull $jobId "AsJob returned a job id"

# Wait with verbose so we can confirm the long-poll path was used.
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$waitVerbose = @()
$waitResult = Wait-RemoteScriptSession -Session $session -Id $jobId -WaitTimeoutSeconds 30 -Verbose 4>&1 |
    Tee-Object -Variable waitCaptured | Out-Null
$sw.Stop()
Stop-ScriptSession -Session $session -ErrorAction SilentlyContinue

$verboseText = ($waitCaptured | Where-Object { $_ -is [System.Management.Automation.VerboseRecord] } |
    ForEach-Object { $_.Message }) -join " | "

# Long-poll should complete in roughly the job's own runtime (~2s) + 1 short-poll overhead.
Assert-True ($sw.Elapsed.TotalSeconds -lt 15) "Wait-RemoteScriptSession completes well under long-poll timeout"
Assert-True ($sw.Elapsed.TotalSeconds -ge 1) "Wait-RemoteScriptSession took at least the job's own duration"

# The long-poll path logs an "Elapsed : Ns" line; the legacy path doesn't.
$usedLongPoll = $verboseText -like "*Elapsed :*"
Assert-True $usedLongPoll "Long-poll endpoint was used (verbose stream contains 'Elapsed :')"

# ============================================================================
#  Test Group 2: Session ownership rejects second identity
# ============================================================================
Write-Host "`n  [Test Group 2: Session Ownership Enforcement]" -ForegroundColor White

$handler = New-Object System.Net.Http.HttpClientHandler
if ($PSVersionTable.PSVersion.Major -ge 6) {
    $handler.ServerCertificateCustomValidationCallback = [System.Net.Http.HttpClientHandler]::DangerousAcceptAnyServerCertificateValidator
}
$hc = New-Object System.Net.Http.HttpClient($handler)

$ownershipSid = [guid]::NewGuid().ToString()

# First caller: config-based shared secret + Name=admin. Claims the session.
$jwtA = New-Jwt -Algorithm HS256 -Issuer 'SPE Remoting' -Audience $protocolHost -Name 'sitecore\admin' -SecretKey $sharedSecret
$hc.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $jwtA)
$urlA = "${serviceUrl}?sessionId=$ownershipSid&rawOutput=true&outputFormat=Raw&persistentSession=true"
$contentA = New-Object System.Net.Http.StringContent('"OWNER_A"', [System.Text.Encoding]::UTF8, "text/plain")
$respA = $hc.PostAsync($urlA, $contentA).Result
Assert-Equal ([int]$respA.StatusCode) 200 "Identity A: first call claims the session"

# Same identity can still access the session.
$contentA2 = New-Object System.Net.Http.StringContent('"OWNER_A_2"', [System.Text.Encoding]::UTF8, "text/plain")
$respA2 = $hc.PostAsync($urlA, $contentA2).Result
Assert-Equal ([int]$respA2.StatusCode) 200 "Identity A: subsequent call with same identity succeeds"

# Second caller: use the Test-ReadOnly API Key (different identity). Expect 403.
$readOnlySecret = "Test-ReadOnly-Secret-K3y!-LongEnough-For-Validation"
$readOnlyKeyId  = "spe_test_readonly_key_001"
$jwtB = New-Jwt -Algorithm HS256 -Issuer 'SPE Remoting' -Audience $protocolHost -KeyId $readOnlyKeyId -SecretKey $readOnlySecret
$hc.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $jwtB)
$contentB = New-Object System.Net.Http.StringContent('Get-Item master:/', [System.Text.Encoding]::UTF8, "text/plain")
$respB = $hc.PostAsync($urlA, $contentB).Result

Assert-Equal ([int]$respB.StatusCode) 403 "Identity B cannot attach to identity A's session (403)"
$restrictionB = $null
if ($respB.Headers.Contains("X-SPE-Restriction")) {
    $restrictionB = $respB.Headers.GetValues("X-SPE-Restriction") | Select-Object -First 1
}
Assert-Equal $restrictionB "session-not-owned" "Response includes X-SPE-Restriction: session-not-owned"

$hc.Dispose()
