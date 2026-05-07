function Invoke-RemoteWait {
    <#
        .SYNOPSIS
            Long-polls the SPE remoting wait endpoint for job completion.

        .DESCRIPTION
            Invoke-RemoteWait issues GET /-/script/wait/ against the session's
            remoting endpoint and holds the request for up to -TimeoutSeconds
            seconds while the server polls job state internally. Returns a
            PSCustomObject: @{ IsDone, Status, Name, ElapsedSeconds, NotSupported }.
            If the server returns 404, NotSupported = $true so the caller can
            fall back to the legacy per-poll pattern.

        .PARAMETER Session
            The ScriptSession returned by New-ScriptSession.

        .PARAMETER JobId
            For -JobType scriptsession: the session id returned by Invoke-RemoteScript -AsJob.
            For -JobType sitecore: the Sitecore.Handle string.

        .PARAMETER JobType
            "scriptsession" (default) or "sitecore".

        .PARAMETER TimeoutSeconds
            Server-side hold time. Clamped server-side to 1..60. Default 30.

        .PARAMETER MaxRetries
            Number of retries on 429/503 before giving up. Default 2.

        .PARAMETER Cursor
            Opaque server-issued cursor that tells the wait endpoint where to
            resume reading the script session's stream-record buffer. Pass the
            cursor returned by the previous Invoke-RemoteWait call. Omit on the
            first call to read from the start of the buffer.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Session,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$JobId,

        [Parameter()]
        [ValidateSet('scriptsession', 'sitecore')]
        [string]$JobType = 'scriptsession',

        [Parameter()]
        [ValidateRange(1, 60)]
        [int]$TimeoutSeconds = 30,

        [Parameter()]
        [ValidateRange(0, 10)]
        [int]$MaxRetries = 2,

        [Parameter()]
        [string]$Cursor
    )

    $sd = Expand-ScriptSession -Session $Session
    $connectionUri = @($sd.ConnectionUri)[0]
    if (-not $connectionUri) {
        throw "Session has no ConnectionUri."
    }

    $clientInfo = New-SpeHttpClient -Username $sd.Username -Password $sd.Password `
        -SharedSecret $sd.SharedSecret -AccessKeyId $sd.AccessKeyId -AccessToken $sd.AccessToken `
        -Credential $sd.Credential -UseDefaultCredentials $sd.UseDefaultCredentials `
        -Uri $connectionUri -Algorithm $sd.Algorithm

    # apiVersion is injected by the server-side URL rewriter (derived from the
    # /-/script/wait/ path segment). Omit it here to avoid a duplicated value
    # that would turn the Params lookup into "wait,wait" and miss the route.
    $url = $connectionUri.AbsoluteUri.TrimEnd('/') +
        "/-/script/wait/?sessionId=$($sd.SessionId)&jobId=$([uri]::EscapeDataString($JobId))&jobType=$JobType&timeoutSeconds=$TimeoutSeconds"
    if ($Cursor) {
        $url += "&cursor=$([uri]::EscapeDataString($Cursor))"
    }

    $authDesc = Get-AuthDescription -Username $sd.Username -Password $sd.Password -SharedSecret $sd.SharedSecret `
        -AccessKeyId $sd.AccessKeyId -AccessToken $sd.AccessToken -Credential $sd.Credential -UseDefaultCredentials $sd.UseDefaultCredentials
    Write-Verbose -Message "GET $url auth=$authDesc"

    $attempt = 0
    while ($true) {
        # Per-call cancellation: the cached HttpClient is shared with other cmdlets,
        # so we cannot mutate Client.Timeout safely. A CancellationTokenSource bound
        # to the server-side hold + a small slack guarantees a frozen connection or
        # black-holed proxy never blocks the cmdlet thread past TimeoutSeconds + 5.
        $cts = New-Object System.Threading.CancellationTokenSource
        $cts.CancelAfter([TimeSpan]::FromSeconds($TimeoutSeconds + 5))
        $wallSw = $null
        try {
            $wallSw = [System.Diagnostics.Stopwatch]::StartNew()
            # GetAwaiter().GetResult() unwraps the AggregateException that .Result
            # would wrap around the real transport exception, so $_.Exception.Message
            # below names the actual cause (DNS, TLS, cancellation) rather than
            # "One or more errors occurred".
            $response = (Invoke-SpeHttpRequest -ClientInfo $clientInfo `
                -Method ([System.Net.Http.HttpMethod]::Get) -Url $url `
                -CancellationToken $cts.Token).GetAwaiter().GetResult()
            $wallSw.Stop()
        } catch [System.OperationCanceledException] {
            if ($wallSw -and $wallSw.IsRunning) { $wallSw.Stop() }
            Write-Verbose "Invoke-RemoteWait: cancelled after $($TimeoutSeconds + 5)s (server-side hold + slack exceeded)."
            return [PSCustomObject]@{ IsDone = $false; Status = 'TransportError'; Name = $JobId; ElapsedSeconds = 0; NotSupported = $false }
        } catch {
            if ($wallSw -and $wallSw.IsRunning) { $wallSw.Stop() }
            # Low-level connection failure. Surface as NotSupported=false, IsDone=false so
            # callers can retry at their own cadence without falling back to legacy polling.
            Write-Verbose "Invoke-RemoteWait: transport error: $($_.Exception.Message)"
            return [PSCustomObject]@{ IsDone = $false; Status = 'TransportError'; Name = $JobId; ElapsedSeconds = 0; NotSupported = $false }
        } finally {
            $cts.Dispose()
        }

        Write-Verbose -Message (Format-ResponseLine -Response $response -WallMs $wallSw.Elapsed.TotalMilliseconds)
        $statusCode = [int]$response.StatusCode

        if ($statusCode -eq 404) {
            # Old server without the wait route. Signal fallback.
            return [PSCustomObject]@{ IsDone = $false; Status = 'NotSupported'; Name = $JobId; ElapsedSeconds = 0; NotSupported = $true }
        }

        if (($statusCode -eq 429 -or $statusCode -eq 502 -or $statusCode -eq 503) -and $attempt -lt $MaxRetries) {
            $hint = 0
            if ($response.Headers.Contains('Retry-After')) {
                $rawVal = $response.Headers.GetValues('Retry-After') | Select-Object -First 1
                $parsed = 0
                if ([int]::TryParse([string]$rawVal, [ref]$parsed) -and $parsed -ge 1) {
                    $hint = [Math]::Min($parsed, 60)
                }
            }
            $delay = Get-SpeBackoffDelay -Attempt ($attempt + 1) -BaseSeconds 1.0 -CapSeconds 10.0 -ServerHintSeconds $hint
            Write-Verbose ("Invoke-RemoteWait: HTTP $statusCode, sleeping {0:N2}s before retry (attempt {1}/{2})." -f $delay, ($attempt + 1), $MaxRetries)
            Start-Sleep -Seconds $delay
            $attempt++
            continue
        }

        if ($statusCode -ne 200) {
            # Auth/policy/other failure. Not a retryable transport issue.
            Write-Verbose "Invoke-RemoteWait: HTTP $statusCode - not retrying."
            return [PSCustomObject]@{ IsDone = $true; Status = "HttpError_$statusCode"; Name = $JobId; ElapsedSeconds = 0; NotSupported = $false }
        }

        $body = $response.Content.ReadAsStringAsync().Result
        try {
            $parsed = $body | ConvertFrom-Json
        } catch {
            Write-Verbose "Invoke-RemoteWait: malformed JSON response: $body"
            return [PSCustomObject]@{ IsDone = $true; Status = 'MalformedResponse'; Name = $JobId; ElapsedSeconds = 0; NotSupported = $false }
        }

        # Streams (Verbose/Information/Progress/Warning/Error) are present only
        # when the server supports the tee feature and recorded any new records
        # since the caller's cursor. Older servers omit these fields - returning
        # empty array and $null cursor keeps the contract uniform.
        $streamRecords = @()
        if ($parsed.PSObject.Properties['streams'] -and $parsed.streams) {
            $streamRecords = @($parsed.streams)
        }
        $newCursor = $null
        if ($parsed.PSObject.Properties['cursor']) {
            $newCursor = [string]$parsed.cursor
        }
        # droppedCount is the sum across rate + size caps for back-compat with
        # pre-split servers. droppedRate / droppedSize / truncatedCount are
        # populated when the server splits them; otherwise default to 0 and the
        # client still surfaces the aggregate via droppedCount.
        $droppedCount   = 0
        $droppedRate    = 0
        $droppedSize    = 0
        $truncatedCount = 0
        if ($parsed.PSObject.Properties['droppedCount'])   { $droppedCount   = [long]$parsed.droppedCount }
        if ($parsed.PSObject.Properties['droppedRate'])    { $droppedRate    = [long]$parsed.droppedRate }
        if ($parsed.PSObject.Properties['droppedSize'])    { $droppedSize    = [long]$parsed.droppedSize }
        if ($parsed.PSObject.Properties['truncatedCount']) { $truncatedCount = [long]$parsed.truncatedCount }

        $waitSummary = "Wait: isDone=$([bool]$parsed.isDone) status=$([string]$parsed.status) records=$($streamRecords.Count)"
        if ($droppedCount -gt 0 -or $truncatedCount -gt 0) {
            $waitSummary += " (drops total=$droppedCount"
            if ($droppedRate -gt 0)    { $waitSummary += " rate=$droppedRate" }
            if ($droppedSize -gt 0)    { $waitSummary += " size=$droppedSize" }
            if ($truncatedCount -gt 0) { $waitSummary += " truncated=$truncatedCount" }
            $waitSummary += ")"
        }
        Write-Verbose -Message $waitSummary

        return [PSCustomObject]@{
            IsDone         = [bool]$parsed.isDone
            Status         = [string]$parsed.status
            Name           = [string]$parsed.name
            ElapsedSeconds = [int]$parsed.elapsedSeconds
            NotSupported   = $false
            Streams        = $streamRecords
            Cursor         = $newCursor
            DroppedCount   = $droppedCount
            DroppedRate    = $droppedRate
            DroppedSize    = $droppedSize
            TruncatedCount = $truncatedCount
        }
    }
}
