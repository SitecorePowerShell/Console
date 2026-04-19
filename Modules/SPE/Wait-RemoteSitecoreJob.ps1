function Wait-RemoteSitecoreJob {
        <#
        .SYNOPSIS
            Polls for the specified job until it has completed.

        .DESCRIPTION
            The Wait-RemoteSitecoreJob command waits for a Sitecore.Jobs.Job to complete processing.

        .PARAMETER Job
            The Sitecore.Jobs.Job object to poll.

        .PARAMETER Delay
            The polling interval in seconds.

        .EXAMPLE
            The following example remotely rebuilds a search index as a job and waits for it to complete.
            The Rebuild-SearchIndex command returns a Sitecore.Jobs.Job object.

            $session = New-ScriptSession -Username admin -SharedSecret $secret -ConnectionUri https://remotesitecore
            $jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
                    Rebuild-SearchIndex -Name sitecore_master_index -AsJob |
                        ForEach-Object { $_.Handle.ToString() }
            }
            Wait-RemoteSitecoreJob -Session $session -Id $jobId -Delay 5 -Verbose
            Stop-ScriptSession -Session $session

    	.LINK
            New-ScriptSession

        .LINK
            Invoke-RemoteScript

        .LINK
            Stop-ScriptSession

        .LINK
            Wait-RemoteScriptSession

    #>

    [CmdletBinding()]
    param(
        [Parameter(ParameterSetName='Session')]
        [ValidateNotNull()]
        [pscustomobject]$Session,

        [ValidateNotNull()]
        [string[]]$Ids,

        [int]$Delay = 1,

        [Parameter()]
        [ValidateRange(0, 10)]
        [int]$MaxRetries = 2,

        [Parameter()]
        [ValidateRange(1, 60)]
        [int]$WaitTimeoutSeconds = 30
    )

    # Legacy scriptblock retained as a fallback for servers without the
    # /-/script/wait/ long-poll endpoint. Note: this scriptblock uses .NET
    # static method calls (JobManager::GetJob, Handle::Parse) which are
    # blocked in ConstrainedLanguage - fallback only works under FullLanguage
    # policies. New server + new client avoids this entirely via long-poll.
    $doneScript = {
        $remoteJob = [Sitecore.Jobs.JobManager]::GetJob([Sitecore.Handle]::Parse($using:id))
        $isDone = $remoteJob -eq $null -or $remoteJob.IsDone -or $remoteJob.Status.Failed
        $status = "No longer exists"
        $remoteJobName = $using:id
        if($remoteJob) {
            $remoteJobName = $remoteJob.Name.Replace("Index_Update_IndexName=", "")
            $state = $remoteJob.Status.State
            $processed = $remoteJob.Status.Processed
            if($remoteJob.Options -and $remoteJob.Options.CustomData -is [Sitecore.Publishing.PublishStatus]) {
                $publishStatus = $remoteJob.Options.CustomData -as [Sitecore.Publishing.PublishStatus]
                if($publishStatus.Processed -gt 0) {
                    $state = $publishStatus.State
                    $processed = $publishStatus.Processed
                }
            }
            $status = "$($state) and processed $($processed)"
        }
        [PSCustomObject]@{
            "Name" = $remoteJobName
            "IsDone" = $isDone
            "Status" = $status
        }
    }

    $useLongPoll = $true
    $completed = New-Object System.Collections.Generic.HashSet[string] ([System.StringComparer]::OrdinalIgnoreCase)

    while($completed.Count -ne $Ids.Count) {
        $pollFailed = $false
        foreach ($id in $Ids) {
            if ($completed.Contains($id)) { continue }

            if ($useLongPoll) {
                $wait = Invoke-RemoteWait -Session $session -JobId $id -JobType 'sitecore' `
                    -TimeoutSeconds $WaitTimeoutSeconds -MaxRetries $MaxRetries
                if ($wait.NotSupported) {
                    Write-Verbose "Server does not support /-/script/wait/. Falling back to legacy scriptblock polling (requires FullLanguage policy)."
                    $useLongPoll = $false
                }
                elseif ($wait.Status -like 'HttpError_*') {
                    Write-Warning "Stopped polling job $($id). Long-poll returned $($wait.Status)."
                    $pollFailed = $true
                    break
                }
                elseif ($wait.Status -eq 'TransportError') {
                    # Retry at client cadence.
                }
                else {
                    Write-Verbose "Polling job $($wait.Name). Status : $($wait.Status). Elapsed : $($wait.ElapsedSeconds)s."
                    if ($wait.IsDone) {
                        [void]$completed.Add($id)
                        Write-Verbose "Finished polling job $($id)."
                    }
                    continue
                }
            }

            # Fallback path: send the legacy scriptblock. Respects -MaxRetries.
            $response = Invoke-RemoteScript -Session $session -ScriptBlock $doneScript -MaxRetries $MaxRetries
            if($response -eq $null) {
                Write-Warning "Stopped polling job $($id). Poll call returned no result after $MaxRetries retries."
                $pollFailed = $true
                break
            }
            if($response.IsDone) {
                [void]$completed.Add($id)
                Write-Verbose "Polling job $($response.Name). Status : $($response.Status)."
                Write-Verbose "Finished polling job $($id)."
            } else {
                Write-Verbose "Polling job $($response.Name). Status : $($response.Status)."
            }
        }
        if ($pollFailed) { break }
        if ($completed.Count -ne $Ids.Count -and -not $useLongPoll) {
            # Legacy path paces itself client-side; long-poll held server-side already.
            Start-Sleep -Seconds $Delay
        }
    }
}
