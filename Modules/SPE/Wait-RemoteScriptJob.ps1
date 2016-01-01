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
            $isDone = $remoteJob -eq $null -or $remoteJob.Status.State -ne "Running"
            $remoteJobName = $remoteJob.Name.Replace("Index_Update_IndexName=", "")
            [PSCustomObject]@{
                "Name" = $remoteJobName
                "IsDone" = $isDone
                "Status" = "$($remoteJob.Status.State) and processed $($remoteJob.Status.Processed)"
            }
        }
    }

    $keepRunning = $true
    while($keepRunning) {
        $response = Invoke-RemoteScript -Session $session -ScriptBlock $doneScript
        if($response -and $response.IsDone) {
            $keepRunning = $false
            Write-Verbose "Polling job $($response.Name). Status : $($response.Status)."
            Invoke-RemoteScript -Session $session -ScriptBlock $finishScript
        } else {
            Write-Verbose "Polling job $($response.Name). Status : $($response.Status)."
            Start-Sleep -Seconds $Delay
        }
    }
}