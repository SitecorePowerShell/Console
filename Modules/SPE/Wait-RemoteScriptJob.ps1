function Wait-RemoteScriptJob {
        <#
        .SYNOPSIS
            Polls for the specified job until it has completed.

        .DESCRIPTON
            The Wait-RemoteScriptJob command waits for a ScriptSession or Sitecore.Jobs.Job to complete processing.
    
        .PARAMETER Job
            The ScriptSession or Sitecore.Jobs.Job object to poll.

        .PARAMETER Delay
            The polling interval in seconds.
        
        .EXAMPLE
            The following example remotely rebuilds a search index as a job and waits for it to complete.
            The Rebuild-SearchIndex command returns a Sitecore.Jobs.Job object.
    
            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
            $job = Invoke-RemoteScript -Session $session -ScriptBlock {
                    Rebuild-SearchIndex -Name sitecore_master_index -AsJob
            }
            Wait-RemoteScriptJob -Session $session -Job $job -Delay 5 -Verbose
    
        .EXAMPLE
            The following example remotely rebuilds link databases as a job and waits for it to complete.
            The Invoke-RemoteScript command returns a ScriptSession object.

            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
            $job = Invoke-RemoteScript -Session $session -ScriptBlock {
                    "master", "web" | Get-Database | 
                        ForEach-Object { 
                            [Sitecore.Globals]::LinkDatabase.Rebuild($_)
                        }
            } -AsJob
            Wait-RemoteScriptJob -Session $session -Job $job -Delay 5 -Verbose
    #>
    
    [CmdletBinding()]
    param(
        [Parameter(ParameterSetName='Session')]
        [ValidateNotNull()]
        [pscustomobject]$Session,

        [ValidateNotNull()]
        [pscustomobject]$Job,

        [int]$Delay = 1
    )
    
    $id = -1
    $doneScript = { $true }
    $finishScript = {}
    if($job.PSObject.TypeNames -like "*ScriptSession") {
        $id = $job.Id
        $doneScript = {
            $backgroundScriptSession = Get-ScriptSession -Id $using:id
            $isDone = $backgroundScriptSession -eq $null -or $backgroundScriptSession.State -ne "Busy"
            [PSCustomObject]@{
                "Name" = $backgroundScriptSession.Id
                "IsDone" = $isDone
                "Status" = "$($backgroundScriptSession.State)"
            }
        }
        $finishScript = {
            $backgroundScriptSession = Get-ScriptSession -Id $using:id
            $backgroundScriptSession | Receive-ScriptSession
        }
    } elseif($job.PSObject.TypeNames -like "*Job") {
        $id = $job.Handle.ToString()
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
                    $state = $publishStatus.State
                    $processed = $publishStatus.Processed
                }
                $status = "$($state) and processed $($processed)"
            }
            [PSCustomObject]@{
                "Name" = $remoteJobName
                "IsDone" = $isDone
                "Status" = $status
            }
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