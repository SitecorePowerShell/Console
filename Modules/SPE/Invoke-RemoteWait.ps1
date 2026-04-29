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
    $clientCache = $sd.HttpClients
    $connectionUri = @($sd.ConnectionUri)[0]
    if (-not $connectionUri) {
        throw "Session has no ConnectionUri."
    }

    $client = New-SpeHttpClient -Username $sd.Username -Password $sd.Password `
        -SharedSecret $sd.SharedSecret -AccessKeyId $sd.AccessKeyId `
        -Credential $sd.Credential -UseDefaultCredentials $sd.UseDefaultCredentials `
        -Uri $connectionUri -Cache $clientCache -Algorithm $sd.Algorithm

    # apiVersion is injected by the server-side URL rewriter (derived from the
    # /-/script/wait/ path segment). Omit it here to avoid a duplicated value
    # that would turn the Params lookup into "wait,wait" and miss the route.
    $url = $connectionUri.AbsoluteUri.TrimEnd('/') +
        "/-/script/wait/?sessionId=$($sd.SessionId)&jobId=$([uri]::EscapeDataString($JobId))&jobType=$JobType&timeoutSeconds=$TimeoutSeconds"
    if ($Cursor) {
        $url += "&cursor=$([uri]::EscapeDataString($Cursor))"
    }

    $attempt = 0
    while ($true) {
        try {
            $response = $client.GetAsync($url).Result
        } catch {
            # Low-level connection failure. Surface as NotSupported=false, IsDone=false so
            # callers can retry at their own cadence without falling back to legacy polling.
            Write-Verbose "Invoke-RemoteWait: transport error: $($_.Exception.Message)"
            return [PSCustomObject]@{ IsDone = $false; Status = 'TransportError'; Name = $JobId; ElapsedSeconds = 0; NotSupported = $false }
        }

        $statusCode = [int]$response.StatusCode

        if ($statusCode -eq 404) {
            # Old server without the wait route. Signal fallback.
            return [PSCustomObject]@{ IsDone = $false; Status = 'NotSupported'; Name = $JobId; ElapsedSeconds = 0; NotSupported = $true }
        }

        if (($statusCode -eq 429 -or $statusCode -eq 503) -and $attempt -lt $MaxRetries) {
            $retryAfter = 1
            if ($response.Headers.Contains('Retry-After')) {
                $rawVal = $response.Headers.GetValues('Retry-After') | Select-Object -First 1
                $parsed = 0
                if ([int]::TryParse([string]$rawVal, [ref]$parsed) -and $parsed -ge 1) {
                    $retryAfter = [Math]::Min($parsed, 60)
                }
            }
            Write-Verbose "Invoke-RemoteWait: HTTP $statusCode, sleeping $retryAfter s before retry (attempt $($attempt + 1)/$MaxRetries)."
            Start-Sleep -Seconds $retryAfter
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

        # Streams (Verbose/Information/Progress/Warning) are present only when the
        # server supports the tee feature and recorded any new records since the
        # caller's cursor. Older servers omit these fields - returning empty array
        # and $null cursor keeps the contract uniform for downstream consumers.
        $streamRecords = @()
        if ($parsed.PSObject.Properties['streams'] -and $parsed.streams) {
            $streamRecords = @($parsed.streams)
        }
        $newCursor = $null
        if ($parsed.PSObject.Properties['cursor']) {
            $newCursor = [string]$parsed.cursor
        }
        $droppedCount = 0
        if ($parsed.PSObject.Properties['droppedCount']) {
            $droppedCount = [long]$parsed.droppedCount
        }

        return [PSCustomObject]@{
            IsDone         = [bool]$parsed.isDone
            Status         = [string]$parsed.status
            Name           = [string]$parsed.name
            ElapsedSeconds = [int]$parsed.elapsedSeconds
            NotSupported   = $false
            Streams        = $streamRecords
            Cursor         = $newCursor
            DroppedCount   = $droppedCount
        }
    }
}
