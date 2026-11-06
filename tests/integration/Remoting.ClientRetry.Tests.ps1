# Remoting Tests - Client-Side Retry Behavior
# Tests that Invoke-RemoteScript retries on 429/503 when -MaxRetries is set.
# Shared Secret Clients created by Remoting.ClientRetry.Setup.ps1 before these tests run.
# Run via: .\Run-RemotingTests.ps1 (automatically run in the client-retry phase)

# ============================================================================
#  Test Group 1: Gap 1 - 429 auto-retry with -MaxRetries
# ============================================================================
Write-Host "`n  [Test Group 1: 429 Auto-Retry]" -ForegroundColor White

$retryAccessKeyId = "spe_test_client_retry_01"
$retrySecret      = "Test-ClientRetry-Secret-K3y!-LongEnough-For-Validation"

# Prime: consume the single-request budget so the next call is over-limit.
$primeSession = New-ScriptSession -SharedSecret $retrySecret -AccessKeyId $retryAccessKeyId `
    -ConnectionUri $protocolHost
$primeResult = Invoke-RemoteScript -Session $primeSession -ScriptBlock { "PRIMED" } -Raw -ErrorAction SilentlyContinue
Assert-Equal $primeResult "PRIMED" "First call consumes the rate-limit budget"
Stop-ScriptSession -Session $primeSession -ErrorAction SilentlyContinue

# Baseline: without -MaxRetries, second call fails (current behavior preserved)
$baselineSession = New-ScriptSession -SharedSecret $retrySecret -AccessKeyId $retryAccessKeyId `
    -ConnectionUri $protocolHost
$baselineErrors = @()
$baselineResult = Invoke-RemoteScript -Session $baselineSession -ScriptBlock { "SHOULD_NOT_REACH" } -Raw -ErrorAction SilentlyContinue -ErrorVariable baselineErrors
Assert-True ($baselineErrors.Count -gt 0 -or -not $baselineResult) "Second call without -MaxRetries fails with rate-limit error"
$baselineMsg = ($baselineErrors | ForEach-Object { $_.ToString() }) -join " | "
Assert-Like $baselineMsg "*Rate limit exceeded*" "Baseline error stream mentions rate-limit"
Stop-ScriptSession -Session $baselineSession -ErrorAction SilentlyContinue

# Retry: with -MaxRetries=1, call waits for Retry-After then succeeds on retry
$retrySession = New-ScriptSession -SharedSecret $retrySecret -AccessKeyId $retryAccessKeyId `
    -ConnectionUri $protocolHost
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$retryErrors = @()
$retryResult = Invoke-RemoteScript -Session $retrySession -ScriptBlock { "RETRIED_OK" } -MaxRetries 1 -Raw -ErrorAction SilentlyContinue -ErrorVariable retryErrors
$sw.Stop()
Stop-ScriptSession -Session $retrySession -ErrorAction SilentlyContinue

Assert-Equal $retryResult "RETRIED_OK" "Second call with -MaxRetries=1 succeeds after Retry-After"
Assert-True ($sw.Elapsed.TotalSeconds -ge 1.0) "Retry waited at least 1 second (ThrottleWindow=3s, Retry-After was honored)"
Assert-True ($sw.Elapsed.TotalSeconds -lt 15.0) "Retry completed within a reasonable ceiling (< 15s total)"

# ============================================================================
#  Test Group 2: Gap 1 - -MaxRetries=0 preserves current behavior (default off)
# ============================================================================
Write-Host "`n  [Test Group 2: -MaxRetries default = 0]" -ForegroundColor White

# Wait for the window to reset before this test.
Write-Host "    Waiting 4s for throttle window to reset..." -ForegroundColor Gray
Start-Sleep -Seconds 4

$defaultSession = New-ScriptSession -SharedSecret $retrySecret -AccessKeyId $retryAccessKeyId `
    -ConnectionUri $protocolHost
$primed = Invoke-RemoteScript -Session $defaultSession -ScriptBlock { "PRIMED2" } -Raw -ErrorAction SilentlyContinue
Assert-Equal $primed "PRIMED2" "Second window: first call consumes the budget"

$defaultErrors = @()
$defaultResult = Invoke-RemoteScript -Session $defaultSession -ScriptBlock { "NOPE" } -Raw -ErrorAction SilentlyContinue -ErrorVariable defaultErrors
Assert-True ($defaultErrors.Count -gt 0) "Without -MaxRetries (default 0), the call fails instead of retrying"
Stop-ScriptSession -Session $defaultSession -ErrorAction SilentlyContinue

# ============================================================================
#  Test Group 3: Gap 4 - Rate-limit headers surfaced on success via Write-Verbose
# ============================================================================
Write-Host "`n  [Test Group 3: Rate-Limit Verbose on Success]" -ForegroundColor White

$obsAccessKeyId = "spe_test_client_obs_01"
$obsSecret      = "Test-ClientObs-Secret-K3y!-LongEnough-For-Validation"

$obsSession = New-ScriptSession -SharedSecret $obsSecret -AccessKeyId $obsAccessKeyId `
    -ConnectionUri $protocolHost

$verboseStream = $null
$null = Invoke-RemoteScript -Session $obsSession -ScriptBlock { "OBS_OK" } -Raw -Verbose 4>&1 |
    Tee-Object -Variable capturedMessages | Out-Null
$verboseText = ($capturedMessages | ForEach-Object { $_.ToString() }) -join "`n"

Assert-Like $verboseText "*X-RateLimit-Limit*" "Verbose stream contains X-RateLimit-Limit on success"
Assert-Like $verboseText "*X-RateLimit-Remaining*" "Verbose stream contains X-RateLimit-Remaining on success"

Stop-ScriptSession -Session $obsSession -ErrorAction SilentlyContinue

# ============================================================================
#  Test Group 4: Gap 3 - 401 diagnostics (X-Spe-AuthFailureReason)
# ============================================================================
Write-Host "`n  [Test Group 4: 401 Auth Failure Reason Header]" -ForegroundColor White

# Raw HTTP: expired key returns X-Spe-AuthFailureReason=expired
$expiredSecret = "Test-Expired-Secret-K3y!-LongEnough-For-Validation"
$expiredKeyId  = "spe_test_expired_key_001"
$jwtParams = @{
    Algorithm = 'HS256'; Issuer = 'SPE Remoting'
    Audience  = $protocolHost; SecretKey = $expiredSecret; KeyId = $expiredKeyId
}
$token = New-Jwt @jwtParams

$handler = New-Object System.Net.Http.HttpClientHandler
if ($PSVersionTable.PSVersion.Major -ge 6) {
    $handler.ServerCertificateCustomValidationCallback = [System.Net.Http.HttpClientHandler]::DangerousAcceptAnyServerCertificateValidator
}
$hc = New-Object System.Net.Http.HttpClient($handler)
$hc.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token)
$sid = [guid]::NewGuid().ToString()
$url = "$protocolHost/-/script/script/?sessionId=$sid&rawOutput=false&outputFormat=clixml&persistentSession=false"
$content = New-Object System.Net.Http.StringContent('"x"', [System.Text.Encoding]::UTF8, "text/plain")
$response = $hc.PostAsync($url, $content).Result

Assert-Equal ([int]$response.StatusCode) 401 "Expired key returns 401"
$hasReasonHeader = $response.Headers.Contains("X-Spe-AuthFailureReason")
Assert-True $hasReasonHeader "Response includes X-Spe-AuthFailureReason header"
if ($hasReasonHeader) {
    $reason = $response.Headers.GetValues("X-Spe-AuthFailureReason") | Select-Object -First 1
    Assert-Equal $reason "expired" "Expired key: X-Spe-AuthFailureReason is 'expired'"
}

# Raw HTTP: disabled key returns X-Spe-AuthFailureReason=disabled
$disabledSecret = "Test-ClientDisabled-Secret-K3y!-LongEnough-For-Validation"
$disabledKeyId  = "spe_test_client_disabled_01"
$jwtParams2 = @{
    Algorithm = 'HS256'; Issuer = 'SPE Remoting'
    Audience  = $protocolHost; SecretKey = $disabledSecret; KeyId = $disabledKeyId
}
$token2 = New-Jwt @jwtParams2
$hc.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token2)
$sid2 = [guid]::NewGuid().ToString()
$url2 = "$protocolHost/-/script/script/?sessionId=$sid2&rawOutput=false&outputFormat=clixml&persistentSession=false"
$content2 = New-Object System.Net.Http.StringContent('"x"', [System.Text.Encoding]::UTF8, "text/plain")
$response2 = $hc.PostAsync($url2, $content2).Result

Assert-Equal ([int]$response2.StatusCode) 401 "Disabled key returns 401"
$hasReasonHeader2 = $response2.Headers.Contains("X-Spe-AuthFailureReason")
Assert-True $hasReasonHeader2 "Disabled-key response includes X-Spe-AuthFailureReason header"
if ($hasReasonHeader2) {
    $reason2 = $response2.Headers.GetValues("X-Spe-AuthFailureReason") | Select-Object -First 1
    Assert-Equal $reason2 "disabled" "Disabled key: X-Spe-AuthFailureReason is 'disabled'"
}

# Raw HTTP: unknown kid returns X-Spe-AuthFailureReason=invalid (not 'unknown' -- collapsed to resist enumeration)
$jwtParams3 = @{
    Algorithm = 'HS256'; Issuer = 'SPE Remoting'
    Audience  = $protocolHost; SecretKey = "Any-Secret-Long-Enough-For-Validation-123"
    KeyId = "spe_not_a_real_key_99"
}
$token3 = New-Jwt @jwtParams3
$hc.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token3)
$sid3 = [guid]::NewGuid().ToString()
$url3 = "$protocolHost/-/script/script/?sessionId=$sid3&rawOutput=false&outputFormat=clixml&persistentSession=false"
$content3 = New-Object System.Net.Http.StringContent('"x"', [System.Text.Encoding]::UTF8, "text/plain")
$response3 = $hc.PostAsync($url3, $content3).Result

Assert-Equal ([int]$response3.StatusCode) 401 "Unknown kid returns 401"
if ($response3.Headers.Contains("X-Spe-AuthFailureReason")) {
    $reason3 = $response3.Headers.GetValues("X-Spe-AuthFailureReason") | Select-Object -First 1
    Assert-Equal $reason3 "invalid" "Unknown kid: X-Spe-AuthFailureReason is 'invalid' (collapsed from unknown+signatureInvalid)"
} else {
    Assert-True $false "Unknown kid: expected X-Spe-AuthFailureReason header"
}

# Client-side: Invoke-RemoteScript surfaces the reason in the error message
$expiredSession = New-ScriptSession -SharedSecret $expiredSecret -AccessKeyId $expiredKeyId `
    -ConnectionUri $protocolHost
$expErrors = @()
$null = Invoke-RemoteScript -Session $expiredSession -ScriptBlock { "nope" } -Raw -ErrorAction SilentlyContinue -ErrorVariable expErrors
Stop-ScriptSession -Session $expiredSession -ErrorAction SilentlyContinue

Assert-True ($expErrors.Count -gt 0) "Invoke-RemoteScript with expired key emits an error"
$expMsg = ($expErrors | ForEach-Object { $_.ToString() }) -join " | "
Assert-Like $expMsg "*expired*" "Client error stream mentions 'expired'"

$hc.Dispose()

# ============================================================================
#  Test Group 5: Transport-Error Retry against a closed loopback port
#  Verifies the unambiguous-safe-transport classification end-to-end:
#  ConnectionRefused on loopback should retry under -MaxRetries and fail
#  immediately without it. Picks a fresh port via TcpListener+Stop so we
#  never collide with whatever else the host is running.
# ============================================================================
Write-Host "`n  [Test Group 5: Transport-Error Retry (closed port)]" -ForegroundColor White

$tcpProbe = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
$tcpProbe.Start()
$closedPort = ([System.Net.IPEndPoint]$tcpProbe.LocalEndpoint).Port
$tcpProbe.Stop()
$closedUri = "http://127.0.0.1:$closedPort"
Write-Host "    Selected closed loopback port: $closedPort" -ForegroundColor Gray

$transportSecret = "Transport-Error-Test-Secret-LongEnough-For-Validation-12345"

# Without -MaxRetries: connection-refused fails on the first attempt only.
# Windows TCP retransmits SYN before reporting refused, so the baseline is
# typically ~2s on loopback even with no retry layer above.
$noRetrySw = [System.Diagnostics.Stopwatch]::StartNew()
$noRetryErrors = @()
$null = Invoke-RemoteScript -ConnectionUri $closedUri -Username "admin" `
    -SharedSecret $transportSecret -ScriptBlock { "x" } -Raw `
    -ErrorAction SilentlyContinue -ErrorVariable noRetryErrors 2>$null
$noRetrySw.Stop()
Assert-True ($noRetrySw.Elapsed.TotalSeconds -lt 5.0) "Closed port without -MaxRetries: single attempt (< 5s, got $([Math]::Round($noRetrySw.Elapsed.TotalSeconds, 2))s)"

# With -MaxRetries=2: classifier retries twice. Three connection-refused round
# trips (~2s each on Windows loopback) plus two jittered backoff sleeps
# (rand(0.1, 2) + rand(0.1, 4)). Expected ~6-12s, bounded above by ~20s.
$retrySw = [System.Diagnostics.Stopwatch]::StartNew()
$null = Invoke-RemoteScript -ConnectionUri $closedUri -Username "admin" `
    -SharedSecret $transportSecret -ScriptBlock { "x" } -Raw -MaxRetries 2 `
    -ErrorAction SilentlyContinue 2>$null
$retrySw.Stop()
$retryDelta = $retrySw.Elapsed.TotalSeconds - $noRetrySw.Elapsed.TotalSeconds
Assert-True ($retryDelta -ge 1.5) "Closed port with -MaxRetries=2: extra time vs no-retry indicates retries happened (>= 1.5s extra, got $([Math]::Round($retryDelta, 2))s)"
Assert-True ($retrySw.Elapsed.TotalSeconds -lt 20.0) "Closed port with -MaxRetries=2: bounded by cap (< 20s, got $([Math]::Round($retrySw.Elapsed.TotalSeconds, 2))s)"

# ============================================================================
#  Test Group 6: 502 Bad Gateway Retry against a stub HTTP server
#  Stands up an HttpListener on loopback that returns 502 for the first
#  request and 200 for the second. Verifies Invoke-RemoteScript retries 502
#  end-to-end (the unit test mocks the HttpMessageHandler; this exercises
#  the real HttpClient + sockets + Resolve-HttpErrorResponse path).
# ============================================================================
Write-Host "`n  [Test Group 6: 502 Bad Gateway Retry (stub HTTP server)]" -ForegroundColor White

$tcpProbe2 = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
$tcpProbe2.Start()
$stubPort = ([System.Net.IPEndPoint]$tcpProbe2.LocalEndpoint).Port
$tcpProbe2.Stop()
$stubUri = "http://127.0.0.1:$stubPort"

$stubListener = New-Object System.Net.HttpListener
$stubListener.Prefixes.Add("$stubUri/")
$stubListener.Start()

$stubState = [hashtable]::Synchronized(@{ Calls = 0; Errors = @() })

$rs = [PowerShell]::Create()
$null = $rs.AddScript({
    param($listener, $state)
    while ($listener.IsListening) {
        try {
            $context = $listener.GetContext()
        } catch {
            break
        }
        try {
            $state.Calls = $state.Calls + 1
            if ($state.Calls -le 1) {
                $context.Response.StatusCode = 502
                $context.Response.StatusDescription = "Bad Gateway"
            } else {
                $body = "STUB_502_OK"
                $bytes = [System.Text.Encoding]::UTF8.GetBytes($body)
                $context.Response.StatusCode = 200
                $context.Response.ContentType = "text/plain"
                $context.Response.ContentLength64 = $bytes.Length
                $context.Response.OutputStream.Write($bytes, 0, $bytes.Length)
            }
            $context.Response.OutputStream.Close()
        } catch {
            $state.Errors += $_.ToString()
            try { $context.Response.Abort() } catch {}
        }
    }
}).AddArgument($stubListener).AddArgument($stubState)

$rsHandle = $rs.BeginInvoke()

try {
    $sw502 = [System.Diagnostics.Stopwatch]::StartNew()
    $result502 = Invoke-RemoteScript -ConnectionUri $stubUri -Username "admin" `
        -SharedSecret "Stub-502-Test-Secret-LongEnough-For-Validation-12345" `
        -ScriptBlock { "x" } -Raw -MaxRetries 1 -ErrorAction SilentlyContinue 2>$null
    $sw502.Stop()

    Assert-Equal $stubState.Calls 2 "502 stub: server received 2 requests (initial 502 + retry)"
    Assert-Equal $result502 "STUB_502_OK" "502 retry returns the 200 body from the second request"
    Assert-True ($sw502.Elapsed.TotalSeconds -lt 15.0) "502 retry completes within ceiling (< 15s, got $([Math]::Round($sw502.Elapsed.TotalSeconds, 2))s)"
    if ($stubState.Errors.Count -gt 0) {
        Write-Host "    Stub server errors: $($stubState.Errors -join ' | ')" -ForegroundColor Yellow
    }
} finally {
    $stubListener.Stop()
    $stubListener.Close()
    # Give the runspace a moment to exit its loop after Stop() unblocks GetContext.
    if (-not $rsHandle.IsCompleted) { Start-Sleep -Milliseconds 200 }
    try { $rs.Stop() } catch {}
    $rs.Dispose()
}
