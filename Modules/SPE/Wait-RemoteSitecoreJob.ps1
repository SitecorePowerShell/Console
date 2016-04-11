function Wait-RemoteSitecoreJob {
        <#
        .SYNOPSIS
            Polls for the specified job until it has completed.

        .DESCRIPTON
            The Wait-RemoteSitecoreJob command waits for a Sitecore.Jobs.Job to complete processing.
    
        .PARAMETER Job
            The Sitecore.Jobs.Job object to poll.

        .PARAMETER Delay
            The polling interval in seconds.
        
        .EXAMPLE
            The following example remotely rebuilds a search index as a job and waits for it to complete.
            The Rebuild-SearchIndex command returns a Sitecore.Jobs.Job object.
    
            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
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
        [string]$Id,

        [int]$Delay = 1
    )
    
    $doneScript = { $true }
    $finishScript = {}
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

    $keepRunning = $true
    while($keepRunning) {
        $response = Invoke-RemoteScript -Session $session -ScriptBlock $doneScript
        if($response -and $response.IsDone) {
            $keepRunning = $false
            Write-Verbose "Polling job $($response.Name). Status : $($response.Status)."
            Write-Verbose "Finished polling job $($id)."
            Invoke-RemoteScript -Session $session -ScriptBlock $finishScript
        } else {
            Write-Verbose "Polling job $($response.Name). Status : $($response.Status)."
            Start-Sleep -Seconds $Delay
        }
    }
}