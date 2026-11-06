# Integration tests for the wait-endpoint drop-counter contract (#1491).
#
# droppedCount, droppedRate, droppedSize, and truncatedCount must appear on
# every wait response - NotFound, empty poll, or busy poll - so a monitoring
# client sees a continuous series instead of values dropping to undefined
# between busy ticks. Defaults are 0 when no StreamSnapshot was produced.
#
# A regression that re-gates these fields inside the `if (streams != null)`
# branch is caught here (would not be caught by Remoting.ProgressStreams or
# Remoting.WaitEventWake, both of which exercise only the streams-non-null
# path).

$waitUrl = "$protocolHost/-/script/wait/"

Write-Host "`n  [Wait Drop Counters: always-emit invariant]" -ForegroundColor White

$handler = New-Object System.Net.Http.HttpClientHandler
if ($PSVersionTable.PSVersion.Major -ge 6) {
    $handler.ServerCertificateCustomValidationCallback = [System.Net.Http.HttpClientHandler]::DangerousAcceptAnyServerCertificateValidator
}
$hc = New-Object System.Net.Http.HttpClient($handler)
$hc.Timeout = [TimeSpan]::FromSeconds(15)

function Assert-DropCountersPresent {
    param(
        [Parameter(Mandatory)] $Body,
        [Parameter(Mandatory)] [string] $Context
    )
    Assert-True ($Body.PSObject.Properties['droppedCount'])   "${Context}: response carries droppedCount"
    Assert-True ($Body.PSObject.Properties['droppedRate'])    "${Context}: response carries droppedRate"
    Assert-True ($Body.PSObject.Properties['droppedSize'])    "${Context}: response carries droppedSize"
    Assert-True ($Body.PSObject.Properties['truncatedCount']) "${Context}: response carries truncatedCount"
    Assert-Equal ([long]$Body.droppedCount)   0 "${Context}: droppedCount defaults to 0"
    Assert-Equal ([long]$Body.droppedRate)    0 "${Context}: droppedRate defaults to 0"
    Assert-Equal ([long]$Body.droppedSize)    0 "${Context}: droppedSize defaults to 0"
    Assert-Equal ([long]$Body.truncatedCount) 0 "${Context}: truncatedCount defaults to 0"
}

# ----------------------------------------------------------------------------
#  Case 1: NotFound - bogus jobId, no session bound, streams stays null.
# ----------------------------------------------------------------------------
Write-Host "`n  [Case 1: NotFound emits drop counters]" -ForegroundColor White

$callerSession = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost
$callerSessionId = $callerSession.SessionId

$bogusJobId = [guid]::NewGuid().ToString()
$jwtNotFound = New-Jwt -Algorithm HS256 -Issuer 'SPE Remoting' -Audience $protocolHost -Name 'sitecore\admin' -SecretKey $sharedSecret
$hc.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $jwtNotFound)

# timeoutSeconds=2 covers the 750ms NotFoundGraceMs retry window plus a
# margin, so the loop reliably reaches the genuine-NotFound branch.
$urlNotFound = "${waitUrl}?sessionId=$callerSessionId&jobId=$([uri]::EscapeDataString($bogusJobId))&jobType=scriptsession&timeoutSeconds=2&cursor="
$respNotFound = $hc.GetAsync($urlNotFound).Result
Assert-Equal ([int]$respNotFound.StatusCode) 200 "Case 1: NotFound wait returns 200"

$bodyNotFound = $respNotFound.Content.ReadAsStringAsync().Result | ConvertFrom-Json
Assert-Equal ([string]$bodyNotFound.status) "NotFound" "Case 1: status is NotFound"
Assert-True  ([bool]$bodyNotFound.isDone)             "Case 1: isDone is true on NotFound"
Assert-True  (-not $bodyNotFound.PSObject.Properties['streams']) "Case 1: streams is absent on NotFound"
Assert-DropCountersPresent -Body $bodyNotFound -Context "Case 1 (NotFound)"

# ----------------------------------------------------------------------------
#  Case 2: empty poll - real busy session, no records produced, deadline
#  elapses without a streams short-circuit.
# ----------------------------------------------------------------------------
Write-Host "`n  [Case 2: empty poll emits drop counters]" -ForegroundColor White

$idleJobId = Invoke-RemoteScript -Session $callerSession -ScriptBlock {
    # Pure sleep, no Write-* records, no progress updates, so StreamRing
    # produces nothing and the wait loop drains the deadline. Long enough
    # to outlast the 2s wait below.
    Start-Sleep -Seconds 6
} -AsJob -Raw

Assert-NotNull $idleJobId "Case 2: AsJob returned a job id"

# Brief beat so the AsJob's session registers in ScriptSessionManager
# before the wait fires (otherwise the registration-race grace path
# applies and we test a different branch).
Start-Sleep -Milliseconds 800

$urlEmpty = "${waitUrl}?sessionId=$callerSessionId&jobId=$([uri]::EscapeDataString($idleJobId))&jobType=scriptsession&timeoutSeconds=2&cursor="
$respEmpty = $hc.GetAsync($urlEmpty).Result
Assert-Equal ([int]$respEmpty.StatusCode) 200 "Case 2: empty-poll wait returns 200"

$bodyEmpty = $respEmpty.Content.ReadAsStringAsync().Result | ConvertFrom-Json
Assert-True (-not $bodyEmpty.PSObject.Properties['streams']) "Case 2: streams is absent when no records produced"
Assert-DropCountersPresent -Body $bodyEmpty -Context "Case 2 (empty poll)"

$hc.Dispose()
Stop-ScriptSession -Session $callerSession -ErrorAction SilentlyContinue
