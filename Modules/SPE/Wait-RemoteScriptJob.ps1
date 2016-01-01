function Wait-RemoteScriptJob {
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