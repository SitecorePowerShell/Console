# Routes one stream record from the long-poll wait endpoint to whichever -On*
# callback the caller supplied. No-op when the matching callback is $null.
function Send-WaitStreamRecord {
    param(
        $Record,
        [scriptblock]$OnVerbose,
        [scriptblock]$OnInformation,
        [scriptblock]$OnProgress,
        [scriptblock]$OnWarning,
        [scriptblock]$OnError
    )
    switch ([string]$Record.stream) {
        'verbose'     { if ($OnVerbose)     { & $OnVerbose     ([PSCustomObject]@{ Stream = 'verbose';     Sequence = $Record.sequence; TimeUtc = $Record.timeUtc; Message = $Record.message }) } }
        'information' { if ($OnInformation) { & $OnInformation ([PSCustomObject]@{ Stream = 'information'; Sequence = $Record.sequence; TimeUtc = $Record.timeUtc; Message = $Record.message }) } }
        'warning'     { if ($OnWarning)     { & $OnWarning     ([PSCustomObject]@{ Stream = 'warning';     Sequence = $Record.sequence; TimeUtc = $Record.timeUtc; Message = $Record.message }) } }
        'progress'    { if ($OnProgress)    { & $OnProgress    ([PSCustomObject]@{
                            Stream            = 'progress'
                            Sequence          = $Record.sequence
                            TimeUtc           = $Record.timeUtc
                            Activity          = $Record.activity
                            StatusDescription = $Record.statusDescription
                            PercentComplete   = $Record.percentComplete
                            CurrentOperation  = $Record.currentOperation
                            SecondsRemaining  = $Record.secondsRemaining
                            ParentActivityId  = $Record.parentActivityId
                            RecordType        = $Record.recordType
                        }) } }
        'error'       { if ($OnError)       { & $OnError       ([PSCustomObject]@{
                            Stream                = 'error'
                            Sequence              = $Record.sequence
                            TimeUtc               = $Record.timeUtc
                            Message               = $Record.message
                            FullyQualifiedErrorId = $Record.fullyQualifiedErrorId
                            CategoryInfo          = $Record.categoryInfo
                            PositionMessage       = $Record.positionMessage
                            ScriptStackTrace      = $Record.scriptStackTrace
                        }) } }
    }
}

# Runs one long-poll iteration against /-/script/wait/. Returns a result object
# the caller dispatches on:
#   Action='Fallback' -> server lacks the wait endpoint, switch to legacy.
#   Action='Stop'     -> unrecoverable, exit the loop.
#   Action='Retry'    -> transport blip, sleep already done, loop again.
#   Action='Result'   -> server replied; check IsDone for completion.
# Cursor and LastDropped are echoed back (possibly updated) so the caller can
# advance its stream-buffer state without [ref] params.
function Invoke-WaitLongPollIteration {
    param(
        [pscustomobject]$Session,
        [string]$Id,
        [int]$MaxRetries,
        [int]$WaitTimeoutSeconds,
        [int]$Delay,
        [bool]$WantsStreams,
        [string]$Cursor,
        [long]$LastDropped,
        [scriptblock]$OnVerbose,
        [scriptblock]$OnInformation,
        [scriptblock]$OnProgress,
        [scriptblock]$OnWarning,
        [scriptblock]$OnError
    )

    $waitArgs = @{
        Session        = $Session
        JobId          = $Id
        JobType        = 'scriptsession'
        TimeoutSeconds = $WaitTimeoutSeconds
        MaxRetries     = $MaxRetries
    }
    if ($WantsStreams -and $Cursor) { $waitArgs['Cursor'] = $Cursor }
    elseif ($WantsStreams)          { $waitArgs['Cursor'] = '' }
    $wait = Invoke-RemoteWait @waitArgs

    if ($wait.NotSupported) {
        Write-Verbose "Server does not support /-/script/wait/ for job $($Id). Falling back to legacy polling."
        return [PSCustomObject]@{ Action = 'Fallback'; Cursor = $Cursor; LastDropped = $LastDropped }
    }
    if ($wait.Status -like 'HttpError_*') {
        Write-Warning "Stopped polling job $($Id). Long-poll returned $($wait.Status)."
        return [PSCustomObject]@{ Action = 'Stop'; Cursor = $Cursor; LastDropped = $LastDropped }
    }
    if ($wait.Status -eq 'TransportError') {
        Start-Sleep -Seconds $Delay
        return [PSCustomObject]@{ Action = 'Retry'; Cursor = $Cursor; LastDropped = $LastDropped }
    }

    Write-Verbose "Polling job $($Id). Status : $($wait.Status). Elapsed : $($wait.ElapsedSeconds)s."

    $newCursor = $Cursor
    $newDropped = $LastDropped
    if ($WantsStreams) {
        if ($wait.Cursor) { $newCursor = $wait.Cursor }
        if ($wait.DroppedCount -gt $LastDropped) {
            $delta     = $wait.DroppedCount - $LastDropped
            # droppedRate / droppedSize are running totals from the server. Older
            # servers omit them and we fall back to the unsplit message.
            if ($wait.DroppedRate -gt 0 -or $wait.DroppedSize -gt 0) {
                Write-Warning "Stream buffer for job $($Id) dropped $delta record(s) ($($wait.DroppedRate) over rate cap, $($wait.DroppedSize) over size cap)."
            } else {
                Write-Warning "Stream buffer for job $($Id) dropped $delta record(s) due to rate or size cap."
            }
            $newDropped = $wait.DroppedCount
        }
        foreach ($record in $wait.Streams) {
            Send-WaitStreamRecord -Record $record `
                -OnVerbose $OnVerbose -OnInformation $OnInformation `
                -OnProgress $OnProgress -OnWarning $OnWarning -OnError $OnError
        }
    }

    return [PSCustomObject]@{
        Action      = 'Result'
        IsDone      = [bool]$wait.IsDone
        Cursor      = $newCursor
        LastDropped = $newDropped
    }
}

# Runs one iteration of the legacy fallback path (Invoke-RemoteScript loop).
# Returns Action='Stop' for unrecoverable failures, Action='Result' otherwise.
function Invoke-WaitLegacyPollIteration {
    param(
        [pscustomobject]$Session,
        [string]$Id,
        [int]$MaxRetries
    )

    $doneScript = {
        $backgroundScriptSession = Get-ScriptSession -Id $using:Id -ErrorAction SilentlyContinue
        $isDone = $null -eq $backgroundScriptSession -or $backgroundScriptSession.State -ne "Busy"
        $status = "$($backgroundScriptSession.State)"
        if([string]::IsNullOrEmpty($status)) { $status = "Unknown" }
        [PSCustomObject]@{
            "Name"   = $using:Id
            "IsDone" = $isDone
            "Status" = $status
        }
    }

    $response = Invoke-RemoteScript -Session $Session -ScriptBlock $doneScript -Verbose -MaxRetries $MaxRetries
    if ($null -eq $response) {
        Write-Warning "Stopped polling job $($Id). Poll call returned no result (rate-limited or auth failure after $MaxRetries retries)."
        return [PSCustomObject]@{ Action = 'Stop' }
    }
    if ($response -eq "login failed") {
        Write-Verbose "Stopped polling job. Login with the specified account failed."
        return [PSCustomObject]@{ Action = 'Stop' }
    }
    Write-Verbose "Polling job $($response.Name). Status : $([string]$response.Status)."
    return [PSCustomObject]@{ Action = 'Result'; IsDone = [bool]$response.IsDone }
}

function Wait-RemoteScriptSession {
        <#
        .SYNOPSIS
            Polls for the specified job until it has completed.

        .DESCRIPTION
            The Wait-RemoteScriptSession command waits for a ScriptSession to complete processing.

        .PARAMETER Session
            The ScriptSession object to poll.

        .PARAMETER Id
            The id of the asynchronous job returned when calling Invoke-RemoteScript with the -AsJob switch.

        .PARAMETER Delay
            The polling interval in seconds.

        .EXAMPLE
            The following example remotely rebuilds link databases as a job and waits for it to complete.
            The Invoke-RemoteScript command returns a ScriptSession object.

            $session = New-ScriptSession -Username admin -SharedSecret $secret -ConnectionUri https://remotesitecore
            $jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
                    "master", "web" | Get-Database |
                        ForEach-Object {
                            [Sitecore.Globals]::LinkDatabase.Rebuild($_)
                        }
            } -AsJob
            Wait-RemoteScriptSession -Session $session -Id $jobId -Delay 5 -Verbose
            Stop-ScriptSession -Session $session

    	.LINK
            New-ScriptSession

        .LINK
            Invoke-RemoteScript

        .LINK
            Stop-ScriptSession

        .LINK
            Wait-RemoteSitecoreJob
    #>

    [CmdletBinding()]
    param(
        [Parameter(ParameterSetName='Session')]
        [ValidateNotNull()]
        [pscustomobject]$Session,

        [ValidateNotNullOrEmpty()]
        [string]$Id,

        [int]$Delay = 1,

        [Parameter()]
        [switch]$Raw,

        [Parameter()]
        [ValidateRange(0, 10)]
        [int]$MaxRetries = 2,

        [Parameter()]
        [ValidateRange(1, 60)]
        [int]$WaitTimeoutSeconds = 30,

        # Callbacks for the PowerShell streams emitted by the background
        # runspace. Each receives one PSCustomObject per record:
        #   Verbose / Information / Warning -> { Stream, Sequence, TimeUtc, Message }
        #   Progress                         -> { Stream, Sequence, TimeUtc, Activity,
        #                                          StatusDescription, PercentComplete,
        #                                          CurrentOperation, SecondsRemaining,
        #                                          ParentActivityId, RecordType }
        #   Error                            -> { Stream, Sequence, TimeUtc, Message,
        #                                          FullyQualifiedErrorId, CategoryInfo,
        #                                          PositionMessage, ScriptStackTrace }
        # OnError fires only for non-terminating errors (Write-Error, cmdlet
        # WriteError). Terminating exceptions still surface via LastErrors at
        # post-Idle drain. When none of the -On* scriptblocks is supplied, the
        # wait endpoint is called without a cursor and any returned stream
        # records are silently discarded. Output stream still drains via
        # Receive-ScriptSession at end.
        [Parameter()]
        [scriptblock]$OnVerbose,

        [Parameter()]
        [scriptblock]$OnInformation,

        [Parameter()]
        [scriptblock]$OnProgress,

        [Parameter()]
        [scriptblock]$OnWarning,

        [Parameter()]
        [scriptblock]$OnError,

        # Resume support: pass an opaque cursor previously emitted via -OnCursor
        # to start reading the per-session stream-record buffer from that
        # offset. Useful when a wait was killed (Ctrl+C, network drop, restart)
        # and you want to avoid replaying records the caller already saw.
        # Cursors are HMAC-signed with a per-app-domain key that regenerates on
        # IIS app-pool recycle - a recycled-key cursor returns HTTP 400 and the
        # cmdlet stops with a warning; re-issue without -Cursor to read from
        # the head of the (post-recycle) buffer.
        [Parameter()]
        [string]$Cursor,

        # Fires whenever the server-issued cursor advances. Pipe the latest
        # value to a file (or any persistent store) so a follow-up invocation
        # can pass it via -Cursor to resume.
        [Parameter()]
        [scriptblock]$OnCursor
    )

    $finishScript = {
        $backgroundScriptSession = Get-ScriptSession -Id $using:id -ErrorAction SilentlyContinue
        $backgroundScriptSession | Receive-ScriptSession
    }

    $useLongPoll = $true
    # Seed cursor from -Cursor (resume) if provided; otherwise start at the
    # head of the buffer. Passing -Cursor implies the caller wants streams,
    # even if they didn't supply an -On* callback for this run.
    $cursor = if ([string]::IsNullOrEmpty($Cursor)) { $null } else { $Cursor }
    $lastDropped = 0
    $wantsStreams = $OnVerbose -or $OnInformation -or $OnProgress -or $OnWarning -or $OnError -or $cursor

    while ($true) {
        if ($useLongPoll) {
            $iter = Invoke-WaitLongPollIteration -Session $session -Id $id `
                -MaxRetries $MaxRetries -WaitTimeoutSeconds $WaitTimeoutSeconds -Delay $Delay `
                -WantsStreams $wantsStreams -Cursor $cursor -LastDropped $lastDropped `
                -OnVerbose $OnVerbose -OnInformation $OnInformation `
                -OnProgress $OnProgress -OnWarning $OnWarning -OnError $OnError
            $oldCursor   = $cursor
            $cursor      = $iter.Cursor
            $lastDropped = $iter.LastDropped
            if ($OnCursor -and $cursor -and $cursor -ne $oldCursor) {
                & $OnCursor $cursor
            }

            if ($iter.Action -eq 'Fallback') { $useLongPoll = $false; continue }
            if ($iter.Action -eq 'Stop')     { break }
            if ($iter.Action -eq 'Retry')    { continue }
        } else {
            $iter = Invoke-WaitLegacyPollIteration -Session $session -Id $id -MaxRetries $MaxRetries
            if ($iter.Action -eq 'Stop') { break }
        }

        if ($iter.IsDone) {
            Write-Verbose "Finished polling job $($id)."
            Invoke-RemoteScript -Session $session -ScriptBlock $finishScript -Raw:$Raw.IsPresent -MaxRetries $MaxRetries
            break
        }

        # Long-poll already waited server-side; legacy must pace client-side.
        if (-not $useLongPoll) {
            Start-Sleep -Seconds $Delay
        }
    }
}