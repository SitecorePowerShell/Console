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

        # Callbacks for the four PowerShell streams emitted by the background
        # runspace. Each receives one PSCustomObject per record:
        #   Verbose / Information / Warning -> { Stream, Sequence, TimeUtc, Message }
        #   Progress                         -> { Stream, Sequence, TimeUtc, Activity,
        #                                          StatusDescription, PercentComplete,
        #                                          CurrentOperation, SecondsRemaining,
        #                                          ParentActivityId, RecordType }
        # When none of the -On* scriptblocks is supplied, the wait endpoint is
        # called without a cursor and any returned stream records are silently
        # discarded. Output stream still drains via Receive-ScriptSession at end.
        [Parameter()]
        [scriptblock]$OnVerbose,

        [Parameter()]
        [scriptblock]$OnInformation,

        [Parameter()]
        [scriptblock]$OnProgress,

        [Parameter()]
        [scriptblock]$OnWarning
    )

    $finishScript = {
        $backgroundScriptSession = Get-ScriptSession -Id $using:id -ErrorAction SilentlyContinue
        $backgroundScriptSession | Receive-ScriptSession
    }

    # Legacy fallback scriptblock used when the server doesn't support the
    # long-poll /-/script/wait/ endpoint (HTTP 404). Kept as a closure so the
    # rest of the function doesn't need to know about it.
    $doneScript = {
        $backgroundScriptSession = Get-ScriptSession -Id $using:id -ErrorAction SilentlyContinue
        $isDone = $backgroundScriptSession -eq $null -or $backgroundScriptSession.State -ne "Busy"
        $status = "$($backgroundScriptSession.State)"
        if([string]::IsNullOrEmpty($status)) { $status = "Unknown" }
        [PSCustomObject]@{
            "Name" = $using:id
            "IsDone" = $isDone
            "Status" = $status
        }
    }

    $useLongPoll = $true
    $keepRunning = $true
    $cursor = $null
    $lastDropped = 0
    $wantsStreams = $OnVerbose -or $OnInformation -or $OnProgress -or $OnWarning
    while($keepRunning) {
        $isDone = $false
        $statusText = "Unknown"

        if ($useLongPoll) {
            $waitArgs = @{
                Session        = $session
                JobId          = $id
                JobType        = 'scriptsession'
                TimeoutSeconds = $WaitTimeoutSeconds
                MaxRetries     = $MaxRetries
            }
            if ($wantsStreams -and $cursor) { $waitArgs['Cursor'] = $cursor }
            elseif ($wantsStreams)          { $waitArgs['Cursor'] = '' }
            $wait = Invoke-RemoteWait @waitArgs
            if ($wait.NotSupported) {
                Write-Verbose "Server does not support /-/script/wait/ for job $($id). Falling back to legacy polling."
                $useLongPoll = $false
                continue
            }
            if ($wait.Status -like 'HttpError_*') {
                Write-Warning "Stopped polling job $($id). Long-poll returned $($wait.Status)."
                break
            }
            if ($wait.Status -eq 'TransportError') {
                Start-Sleep -Seconds $Delay
                continue
            }
            $isDone = $wait.IsDone
            $statusText = $wait.Status
            Write-Verbose "Polling job $($id). Status : $statusText. Elapsed : $($wait.ElapsedSeconds)s."

            if ($wantsStreams) {
                if ($wait.Cursor) { $cursor = $wait.Cursor }
                if ($wait.DroppedCount -gt $lastDropped) {
                    Write-Warning "Stream buffer for job $($id) dropped $($wait.DroppedCount - $lastDropped) record(s) due to rate or size cap."
                    $lastDropped = $wait.DroppedCount
                }
                foreach ($record in $wait.Streams) {
                    switch ([string]$record.stream) {
                        'verbose'     { if ($OnVerbose)     { & $OnVerbose     ([PSCustomObject]@{ Stream = 'verbose';     Sequence = $record.sequence; TimeUtc = $record.timeUtc; Message = $record.message }) } }
                        'information' { if ($OnInformation) { & $OnInformation ([PSCustomObject]@{ Stream = 'information'; Sequence = $record.sequence; TimeUtc = $record.timeUtc; Message = $record.message }) } }
                        'warning'     { if ($OnWarning)     { & $OnWarning     ([PSCustomObject]@{ Stream = 'warning';     Sequence = $record.sequence; TimeUtc = $record.timeUtc; Message = $record.message }) } }
                        'progress'    { if ($OnProgress)    { & $OnProgress    ([PSCustomObject]@{
                                            Stream            = 'progress'
                                            Sequence          = $record.sequence
                                            TimeUtc           = $record.timeUtc
                                            Activity          = $record.activity
                                            StatusDescription = $record.statusDescription
                                            PercentComplete   = $record.percentComplete
                                            CurrentOperation  = $record.currentOperation
                                            SecondsRemaining  = $record.secondsRemaining
                                            ParentActivityId  = $record.parentActivityId
                                            RecordType        = $record.recordType
                                        }) } }
                    }
                }
            }
        }
        else {
            $response = Invoke-RemoteScript -Session $session -ScriptBlock $doneScript -Verbose -MaxRetries $MaxRetries
            if($response -eq $null) {
                Write-Warning "Stopped polling job $($id). Poll call returned no result (rate-limited or auth failure after $MaxRetries retries)."
                break
            } elseif ($response -eq "login failed") {
                Write-Verbose "Stopped polling job. Login with the specified account failed."
                break
            }
            $isDone = [bool]$response.IsDone
            $statusText = [string]$response.Status
            Write-Verbose "Polling job $($response.Name). Status : $statusText."
        }

        if ($isDone) {
            $keepRunning = $false
            Write-Verbose "Finished polling job $($id)."
            Invoke-RemoteScript -Session $session -ScriptBlock $finishScript -Raw:$Raw.IsPresent -MaxRetries $MaxRetries
        }
        elseif (-not $useLongPoll) {
            # Legacy path paces itself client-side; long-poll path already waited server-side.
            Start-Sleep -Seconds $Delay
        }
    }
}