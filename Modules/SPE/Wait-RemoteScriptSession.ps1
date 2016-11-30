function Wait-RemoteScriptSession {
        <#
        .SYNOPSIS
            Polls for the specified job until it has completed.

        .DESCRIPTON
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

        [ValidateNotNullOrEmpty()]
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
        $response = Invoke-RemoteScript -Session $session -ScriptBlock $doneScript -Verbose
        if($response -eq $null) {
            Write-Verbose "Stopped polling job $($id). No results were returned from the service."
            break
        } elseif ($response -eq "login failed") {
            Write-Verbose "Stopped polling job. Login with the specified account failed."
            break            
        }
        
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