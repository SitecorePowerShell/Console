function Wait-RemoteScriptJob {
    [CmdletBinding()]
    param(
        [Parameter(ParameterSetName='Session')]
        [ValidateNotNull()]
        [pscustomobject]$Session,

        [pscustomobject]$Job,

        [int]$Delay = 1
    )

    $keepRunning = $true
    while($keepRunning) {
        $done = Invoke-RemoteScript -Session $session -ScriptBlock {
            $backgroundScriptSession = Get-ScriptSession -Id ($using:Job).Id
            $backgroundScriptSession.State -ne "Busy"
        }

        if($done) {
            $keepRunning = $false
            Write-Verbose "Waiting for job $($job.Id) : complete."
            Invoke-RemoteScript -Session $session -ScriptBlock {
                $backgroundScriptSession = Get-ScriptSession -Id ($using:Job).Id
                $backgroundScriptSession | Receive-ScriptSession
            }
        } else {
            Write-Verbose "Waiting for job $($job.Id) : still busy..."
            Start-Sleep -Seconds $Delay
        }
    }
}