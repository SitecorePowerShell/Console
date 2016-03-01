function Wait-RemoteScriptSession {
        <#
        .SYNOPSIS
            Polls for the specified job until it has completed.

        .DESCRIPTON
            The Wait-RemoteScriptJob command waits for a ScriptSession to complete processing.
    
        .PARAMETER Job
            The ScriptSession object to poll.

        .PARAMETER Delay
            The polling interval in seconds.
    
        .EXAMPLE
            The following example remotely rebuilds link databases as a job and waits for it to complete.
            The Invoke-RemoteScript command returns a ScriptSession object.

            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
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

        [ValidateNotNull()]
        [string]$Id,

        [int]$Delay = 1
    )
    
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