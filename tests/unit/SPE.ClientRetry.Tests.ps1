# Unit tests for client-side retry behavior in Invoke-RemoteScript.
# Uses a mocked HttpMessageHandler (via a shimmed New-SpeHttpClient) to simulate
# server responses without needing a running Sitecore container.

Add-Type -AssemblyName System.Net.Http

if (-not ([System.Management.Automation.PSTypeName]'Spe.Tests.MockRetryHandler').Type) {
    Add-Type -ReferencedAssemblies @('System.Net.Http') -TypeDefinition @"
using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Spe.Tests {
    public class MockRetryHandler : HttpMessageHandler {
        private int _callCount = 0;
        public int CallCount { get { return _callCount; } }
        public int FailCount = 1;
        public int FailStatusCode = 503;
        public int RetryAfterSeconds = 1;
        public bool EmitRetryAfter = true;
        public string SuccessBody = "MOCK_OK";
        public int SuccessLimit = 10;
        public int SuccessRemaining = 9;
        public long SuccessResetEpoch = 0;

        // When > 0, the first FailCount calls throw a synthetic HttpRequestException
        // wrapping a SocketException with this code (10061 ConnectionRefused, etc.).
        // Used to exercise the safe-transport-error retry path. Mutually exclusive
        // with FailStatusCode (this short-circuits ahead of it).
        public int FailSocketErrorCode = 0;

        // Captured request data for inspection (last request).
        public string LastRequestUrl = null;
        public string LastRequestBody = null;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Interlocked.Increment(ref _callCount);
            LastRequestUrl = request.RequestUri != null ? request.RequestUri.ToString() : null;
            if (request.Content != null) {
                try {
                    var bytes = request.Content.ReadAsByteArrayAsync().Result;
                    using (var ms = new System.IO.MemoryStream(bytes))
                    using (var gz = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Decompress))
                    using (var reader = new System.IO.StreamReader(gz)) {
                        LastRequestBody = reader.ReadToEnd();
                    }
                } catch {
                    LastRequestBody = null;
                }
            }
            if (_callCount <= FailCount) {
                if (FailSocketErrorCode != 0) {
                    var sock = new SocketException(FailSocketErrorCode);
                    var ex = new HttpRequestException("Synthetic transport failure", sock);
                    return Task.FromException<HttpResponseMessage>(ex);
                }
                var fail = new HttpResponseMessage((HttpStatusCode)FailStatusCode) { Content = new StringContent("") };
                if (EmitRetryAfter) {
                    fail.Headers.TryAddWithoutValidation("Retry-After", RetryAfterSeconds.ToString());
                }
                return Task.FromResult(fail);
            }
            var ok = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(SuccessBody) };
            ok.Headers.TryAddWithoutValidation("X-RateLimit-Limit", SuccessLimit.ToString());
            ok.Headers.TryAddWithoutValidation("X-RateLimit-Remaining", SuccessRemaining.ToString());
            if (SuccessResetEpoch > 0) {
                ok.Headers.TryAddWithoutValidation("X-RateLimit-Reset", SuccessResetEpoch.ToString());
            }
            return Task.FromResult(ok);
        }
    }
}
"@
}

# Hold a single shared slot that the shimmed New-SpeHttpClient always reads from.
# Tests swap $global:__speMockHandler before each Invoke-RemoteScript call.
$global:__speMockHandler = $null

# Capture the ScriptBlock value (not the FunctionInfo) because FunctionInfo.ScriptBlock
# is evaluated at access time and would resolve to the override after Set-Item below.
$__originalNewSpeHttpClient = (Get-Command New-SpeHttpClient -ErrorAction SilentlyContinue).ScriptBlock

# Override New-SpeHttpClient to return @{ Client; AuthHeader } shaped like the
# real one, with the Client wired to the current mock handler. AuthHeader is
# set to a dummy bearer so Invoke-SpeHttpRequest exercises the same code path
# the real cmdlets do, but the mock handler doesn't validate it.
Set-Item "function:global:New-SpeHttpClient" -Value {
    param($Username, $Password, $SharedSecret, $AccessKeyId, $AccessToken, $Credential, $UseDefaultCredentials, $Uri, $Algorithm)
    [pscustomobject]@{
        Client     = New-Object System.Net.Http.HttpClient($global:__speMockHandler)
        AuthHeader = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-token")
    }
}

function script:New-MockHandler {
    param(
        [int]$FailCount = 1,
        [int]$FailStatusCode = 503,
        [int]$RetryAfterSeconds = 1,
        [bool]$EmitRetryAfter = $true,
        [int]$FailSocketErrorCode = 0,
        [string]$SuccessBody = "MOCK_OK",
        [int]$SuccessLimit = 10,
        [int]$SuccessRemaining = 9,
        [long]$SuccessResetEpoch = 0
    )
    $h = New-Object Spe.Tests.MockRetryHandler
    $h.FailCount = $FailCount
    $h.FailStatusCode = $FailStatusCode
    $h.RetryAfterSeconds = $RetryAfterSeconds
    $h.EmitRetryAfter = $EmitRetryAfter
    $h.FailSocketErrorCode = $FailSocketErrorCode
    $h.SuccessBody = $SuccessBody
    $h.SuccessLimit = $SuccessLimit
    $h.SuccessRemaining = $SuccessRemaining
    $h.SuccessResetEpoch = $SuccessResetEpoch
    $h
}

try {
    # ========================================================================
    #  Group: Get-SpeBackoffDelay (helper)
    # ========================================================================
    Write-Host "`n  [Group: Get-SpeBackoffDelay]" -ForegroundColor White

    # Server-hint mode: result lands in [hint*0.8, hint*1.2]. Sample 50 times so
    # a flaky pseudo-random draw cannot hide a broken bound check.
    $hint = 5
    $samplesHint = 1..50 | ForEach-Object { Get-SpeBackoffDelay -Attempt 1 -ServerHintSeconds $hint }
    $minHint = ($samplesHint | Measure-Object -Minimum).Minimum
    $maxHint = ($samplesHint | Measure-Object -Maximum).Maximum
    Assert-True ($minHint -ge ($hint * 0.8 - 0.001)) "Get-SpeBackoffDelay (hint=5): min sample >= 0.8 * hint (got $minHint)"
    Assert-True ($maxHint -le ($hint * 1.2 + 0.001)) "Get-SpeBackoffDelay (hint=5): max sample <= 1.2 * hint (got $maxHint)"
    Assert-True ($maxHint -gt $minHint) "Get-SpeBackoffDelay (hint=5): jitter actually varies across samples"

    # Hint clamped by Cap.
    $clamped = Get-SpeBackoffDelay -Attempt 1 -ServerHintSeconds 9999 -CapSeconds 5.0
    Assert-True ($clamped -le 5.0) "Get-SpeBackoffDelay: huge hint clamped to Cap"

    # Full-jitter mode (no hint): attempt=1, base=1, cap=30 -> result in [0.1, 2.0].
    $samplesFull = 1..50 | ForEach-Object { Get-SpeBackoffDelay -Attempt 1 -BaseSeconds 1.0 -CapSeconds 30.0 -ServerHintSeconds 0 }
    $minFull = ($samplesFull | Measure-Object -Minimum).Minimum
    $maxFull = ($samplesFull | Measure-Object -Maximum).Maximum
    Assert-True ($minFull -ge 0.1) "Get-SpeBackoffDelay (full jitter, attempt=1): min >= 0.1s"
    Assert-True ($maxFull -le 2.001) "Get-SpeBackoffDelay (full jitter, attempt=1): max <= base*2"
    Assert-True ($maxFull -gt $minFull) "Get-SpeBackoffDelay (full jitter): values vary across samples"

    # Full-jitter cap respected at high attempt count.
    $highAttempt = Get-SpeBackoffDelay -Attempt 10 -BaseSeconds 1.0 -CapSeconds 5.0 -ServerHintSeconds 0
    Assert-True ($highAttempt -le 5.001) "Get-SpeBackoffDelay (full jitter, attempt=10): cap honored"

    # ========================================================================
    #  Group: Test-SpeSafeTransportError (helper)
    # ========================================================================
    Write-Host "`n  [Group: Test-SpeSafeTransportError]" -ForegroundColor White

    Assert-True (Test-SpeSafeTransportError -StatusCode 502) "502 status is unambiguously safe"
    Assert-True (-not (Test-SpeSafeTransportError -StatusCode 504)) "504 status is NOT classified safe (deferred opt-in)"
    Assert-True (-not (Test-SpeSafeTransportError -StatusCode 500)) "500 status is NOT classified safe"

    $sockRefused = New-Object System.Net.Sockets.SocketException 10061
    Assert-True (Test-SpeSafeTransportError -Exception $sockRefused) "SocketException ConnectionRefused (10061) is safe"

    $sockHostNotFound = New-Object System.Net.Sockets.SocketException 11001
    Assert-True (Test-SpeSafeTransportError -Exception $sockHostNotFound) "SocketException HostNotFound (11001) is safe"

    $sockReset = New-Object System.Net.Sockets.SocketException 10054
    Assert-True (-not (Test-SpeSafeTransportError -Exception $sockReset)) "SocketException ConnectionReset (10054) is NOT safe (post-send ambiguous)"

    $wrappedRefused = New-Object System.Net.Http.HttpRequestException("wrapper", $sockRefused)
    Assert-True (Test-SpeSafeTransportError -Exception $wrappedRefused) "HttpRequestException wrapping ConnectionRefused walks InnerException chain"

    $genericEx = New-Object System.InvalidOperationException "not a transport error"
    Assert-True (-not (Test-SpeSafeTransportError -Exception $genericEx)) "Generic exception is NOT safe"

    Assert-True (-not (Test-SpeSafeTransportError -Exception $null)) "Null exception is NOT safe (and does not crash)"

    # ========================================================================
    #  Group: 502 Bad Gateway Retry (NEW)
    # ========================================================================
    Write-Host "`n  [Group: 502 Bad Gateway Retry]" -ForegroundColor White

    # Without -MaxRetries: single 502 fails, no retry.
    $global:__speMockHandler = New-MockHandler -FailStatusCode 502 -FailCount 1 -EmitRetryAfter $false -SuccessBody "GATEWAY_OK"
    $errs = @()
    $result502NoRetry = Invoke-RemoteScript -ConnectionUri "http://mock.local/" -Username "admin" `
        -SharedSecret "any-shared-secret-of-sufficient-length-1234" `
        -ScriptBlock { "x" } -Raw -ErrorAction SilentlyContinue -ErrorVariable errs
    Assert-Equal $global:__speMockHandler.CallCount 1 "502 without -MaxRetries: handler called once (no retry)"
    Assert-True ($errs.Count -gt 0 -or -not $result502NoRetry) "502 without -MaxRetries: surfaces as error"

    # With -MaxRetries=1: 502 then 200 -> success.
    $global:__speMockHandler = New-MockHandler -FailStatusCode 502 -FailCount 1 -EmitRetryAfter $false -SuccessBody "GATEWAY_OK"
    $result502 = Invoke-RemoteScript -ConnectionUri "http://mock.local/" -Username "admin" `
        -SharedSecret "any-shared-secret-of-sufficient-length-1234" `
        -ScriptBlock { "x" } -Raw -MaxRetries 1 -ErrorAction SilentlyContinue
    Assert-Equal $global:__speMockHandler.CallCount 2 "502 with -MaxRetries=1: handler called twice"
    Assert-Equal $result502 "GATEWAY_OK" "502 retry succeeds and body returned"

    # ========================================================================
    #  Group: Transport-Error Retry (NEW - safe transport classification)
    # ========================================================================
    Write-Host "`n  [Group: Transport-Error Retry]" -ForegroundColor White

    # Connection-refused (10061) + -MaxRetries=1: retries and succeeds.
    $global:__speMockHandler = New-MockHandler -FailCount 1 -FailSocketErrorCode 10061 -SuccessBody "RECONNECTED"
    $resultRefused = Invoke-RemoteScript -ConnectionUri "http://mock.local/" -Username "admin" `
        -SharedSecret "any-shared-secret-of-sufficient-length-1234" `
        -ScriptBlock { "x" } -Raw -MaxRetries 1 -ErrorAction SilentlyContinue
    Assert-Equal $global:__speMockHandler.CallCount 2 "Connection-refused + -MaxRetries=1: retried once"
    Assert-Equal $resultRefused "RECONNECTED" "Connection-refused retry succeeds"

    # Connection-reset (10054, ambiguous) + -MaxRetries=1: NOT retried.
    $global:__speMockHandler = New-MockHandler -FailCount 1 -FailSocketErrorCode 10054 -SuccessBody "WONT_REACH"
    $null = Invoke-RemoteScript -ConnectionUri "http://mock.local/" -Username "admin" `
        -SharedSecret "any-shared-secret-of-sufficient-length-1234" `
        -ScriptBlock { "x" } -Raw -MaxRetries 1 -ErrorAction SilentlyContinue 2>$null
    Assert-Equal $global:__speMockHandler.CallCount 1 "Connection-reset (ambiguous) NOT retried even with -MaxRetries=1"

    # Without -MaxRetries: connection-refused is not retried (gated on MaxRetries > 0).
    $global:__speMockHandler = New-MockHandler -FailCount 1 -FailSocketErrorCode 10061 -SuccessBody "WONT_REACH"
    $null = Invoke-RemoteScript -ConnectionUri "http://mock.local/" -Username "admin" `
        -SharedSecret "any-shared-secret-of-sufficient-length-1234" `
        -ScriptBlock { "x" } -Raw -ErrorAction SilentlyContinue 2>$null
    Assert-Equal $global:__speMockHandler.CallCount 1 "Connection-refused without -MaxRetries: not retried"

    # ========================================================================
    #  Group: 503 Cold-Start Retry
    # ========================================================================
    Write-Host "`n  [Group: 503 Cold-Start Retry]" -ForegroundColor White

    # Without -MaxRetries: single 503 fails, handler called once (no retry)
    $global:__speMockHandler = New-MockHandler -FailStatusCode 503 -FailCount 1 -SuccessBody "SERVER_READY"
    $errs = @()
    $result = Invoke-RemoteScript -ConnectionUri "http://mock.local/" -Username "admin" `
        -SharedSecret "any-shared-secret-of-sufficient-length-1234" `
        -ScriptBlock { "x" } -Raw -ErrorAction SilentlyContinue -ErrorVariable errs
    Assert-Equal $global:__speMockHandler.CallCount 1 "Without -MaxRetries: handler called exactly once (no retry on 503)"
    Assert-True ($errs.Count -gt 0 -or -not $result) "Without -MaxRetries: 503 surfaces as error"

    # With -MaxRetries=1: 503 then 200 -> success, handler called twice
    $global:__speMockHandler = New-MockHandler -FailStatusCode 503 -FailCount 1 -RetryAfterSeconds 1 -SuccessBody "SERVER_READY"
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $result = Invoke-RemoteScript -ConnectionUri "http://mock.local/" -Username "admin" `
        -SharedSecret "any-shared-secret-of-sufficient-length-1234" `
        -ScriptBlock { "x" } -Raw -MaxRetries 1 -ErrorAction SilentlyContinue
    $sw.Stop()
    Assert-Equal $global:__speMockHandler.CallCount 2 "With -MaxRetries=1: handler called twice (one retry on 503)"
    Assert-Equal $result "SERVER_READY" "With -MaxRetries=1: retry succeeds and body returned"
    # With +/-20% jitter on Retry-After=1s, the floor is 0.8s. Allow a small
    # measurement slack (timer + Start-Sleep granularity).
    Assert-True ($sw.Elapsed.TotalSeconds -ge 0.7) "With -MaxRetries=1: waited at least jitter-floor (~0.8s)"
    Assert-True ($sw.Elapsed.TotalSeconds -lt 5.0) "With -MaxRetries=1: total elapsed under 5s"

    # 503 ceiling: cap honored Retry-After even if server returns huge value (10s ceiling for 503)
    $global:__speMockHandler = New-MockHandler -FailStatusCode 503 -FailCount 1 -RetryAfterSeconds 9999 -SuccessBody "CAPPED_OK"
    $sw2 = [System.Diagnostics.Stopwatch]::StartNew()
    $result2 = Invoke-RemoteScript -ConnectionUri "http://mock.local/" -Username "admin" `
        -SharedSecret "any-shared-secret-of-sufficient-length-1234" `
        -ScriptBlock { "x" } -Raw -MaxRetries 1 -ErrorAction SilentlyContinue
    $sw2.Stop()
    Assert-Equal $result2 "CAPPED_OK" "503 retry succeeds even when server asks for huge Retry-After"
    Assert-True ($sw2.Elapsed.TotalSeconds -lt 15.0) "503 retry capped at 10s ceiling (elapsed < 15s)"

    # ========================================================================
    #  Group: 429 Rate-Limit Retry (unit-level)
    # ========================================================================
    Write-Host "`n  [Group: 429 Rate-Limit Retry Unit]" -ForegroundColor White

    $global:__speMockHandler = New-MockHandler -FailStatusCode 429 -FailCount 1 -RetryAfterSeconds 1 -SuccessBody "THROTTLE_OK"
    $result3 = Invoke-RemoteScript -ConnectionUri "http://mock.local/" -Username "admin" `
        -SharedSecret "any-shared-secret-of-sufficient-length-1234" `
        -ScriptBlock { "x" } -Raw -MaxRetries 1 -ErrorAction SilentlyContinue
    Assert-Equal $global:__speMockHandler.CallCount 2 "429 + -MaxRetries=1: handler called twice"
    Assert-Equal $result3 "THROTTLE_OK" "429 retry succeeds and body returned"

    # ========================================================================
    #  Group: Rate-Limit Verbose on Success
    # ========================================================================
    Write-Host "`n  [Group: Rate-Limit Verbose on Success]" -ForegroundColor White

    $global:__speMockHandler = New-MockHandler -FailCount 0 -SuccessBody "OBS_OK" `
        -SuccessLimit 60 -SuccessRemaining 59 -SuccessResetEpoch 1700000000

    $verboseMessages = Invoke-RemoteScript -ConnectionUri "http://mock.local/" -Username "admin" `
        -SharedSecret "any-shared-secret-of-sufficient-length-1234" `
        -ScriptBlock { "x" } -Raw -Verbose 4>&1 |
        Where-Object { $_ -is [System.Management.Automation.VerboseRecord] }

    $verboseText = ($verboseMessages | ForEach-Object { $_.Message }) -join "`n"
    Assert-Like $verboseText "*X-RateLimit-Limit*" "Verbose stream contains X-RateLimit-Limit on success"
    Assert-Like $verboseText "*60*" "Verbose stream contains the Limit value (60)"
    Assert-Like $verboseText "*Remaining*" "Verbose stream contains Remaining"

    # ========================================================================
    #  Group: Server-side stream capture contract (B)
    #  When -Verbose is set, the client must signal captureStreams=true in the
    #  URL and must NOT prepend the Write-* bootstrap to the script body.
    #  The server is expected to inject the bootstrap after the policy scan.
    # ========================================================================
    Write-Host "`n  [Group: Server-side Stream Capture Contract]" -ForegroundColor White

    # With -Verbose: URL carries captureStreams=true
    $global:__speMockHandler = New-MockHandler -FailCount 0 -SuccessBody "VERB_OK"
    $null = Invoke-RemoteScript -ConnectionUri "http://mock.local/" -Username "admin" `
        -SharedSecret "any-shared-secret-of-sufficient-length-1234" `
        -ScriptBlock { Write-Verbose "hello" } -Raw -Verbose -ErrorAction SilentlyContinue 4>$null
    Assert-Like $global:__speMockHandler.LastRequestUrl "*captureStreams=true*" "With -Verbose: URL includes captureStreams=true"

    # With -Verbose: request body does NOT contain the Write-* override bootstrap
    Assert-True ($global:__speMockHandler.LastRequestBody -notlike '*Microsoft.PowerShell.Utility\\Write-Information*') `
        "With -Verbose: request body does NOT contain the client-side Write-* bootstrap (server injects server-side)"

    # Without -Verbose / -Debug: URL does NOT include captureStreams
    $global:__speMockHandler = New-MockHandler -FailCount 0 -SuccessBody "QUIET_OK"
    $null = Invoke-RemoteScript -ConnectionUri "http://mock.local/" -Username "admin" `
        -SharedSecret "any-shared-secret-of-sufficient-length-1234" `
        -ScriptBlock { "x" } -Raw -ErrorAction SilentlyContinue
    Assert-True ($global:__speMockHandler.LastRequestUrl -notlike '*captureStreams=true*') `
        "Without -Verbose: URL omits captureStreams"

    # With -Debug (but no -Verbose): URL still carries captureStreams=true
    $global:__speMockHandler = New-MockHandler -FailCount 0 -SuccessBody "DBG_OK"
    $null = Invoke-RemoteScript -ConnectionUri "http://mock.local/" -Username "admin" `
        -SharedSecret "any-shared-secret-of-sufficient-length-1234" `
        -ScriptBlock { "x" } -Raw -Debug -ErrorAction SilentlyContinue 5>$null
    Assert-Like $global:__speMockHandler.LastRequestUrl "*captureStreams=true*" "With -Debug: URL includes captureStreams=true"

} finally {
    if ($__originalNewSpeHttpClient) {
        Set-Item "function:global:New-SpeHttpClient" $__originalNewSpeHttpClient
    }
    $global:__speMockHandler = $null
}
