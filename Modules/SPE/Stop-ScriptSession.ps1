function Stop-ScriptSession {
    <#
        .SYNOPSIS
            Stop Sitecore PowerShell Extensions script session after a script execution.
            This command should always be executed to clean up after a session created using New-ScriptSession was used to Invoke-RemoteScript. 
            If no script was executed on the server (i.e. the $session object was only used to download or upload files/items the cleanup is not necessary.
    
        .EXAMPLE
            The following example remotely executes a script in Sitecore using a reusable session and disposes of it afterwards
    
            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
            Invoke-RemoteScript -Session $session -ScriptBlock { Get-User -id admin }
            Stop-ScriptSession -Session $session
    
            Name                     Domain       IsAdministrator IsAuthenticated
            ----                     ------       --------------- ---------------
            sitecore\admin           sitecore     True            False
    
        .EXAMPLE
            The following example runs a script as a ScriptSession job on the server (using Start-ScriptSession internally).
            The arguments are passed to the server with the help of the $Using convention.
            The results are finally returned and the job is removed after which the session is closed..
            
            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
            $identity = "admin"
            $date = [datetime]::Now
            $jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
                [Sitecore.Security.Accounts.User]$user = Get-User -Identity $using:identity
                $user.Name
                $using:date
            } -AsJob
            Start-Sleep -Seconds 2

            Invoke-RemoteScript -Session $session -ScriptBlock {
                $ss = Get-ScriptSession -Id $using:JobId
                $ss | Receive-ScriptSession

                if($ss.LastErrors) {
                    $ss.LastErrors
                }
            }
            Stop-ScriptSession -Session $session
        
    	.LINK
            Wait-RemoteScriptSession

    	.LINK
            New-ScriptSession

        .LINK
            Invoke-RemoteScript
    #>
    
    [CmdletBinding()]
    param(
        
        [Parameter(ParameterSetName='Session')]
        [ValidateNotNull()]
        [pscustomobject]$Session,

        [Parameter(ParameterSetName='Uri')]
        [Uri[]]$ConnectionUri,

        [Parameter(ParameterSetName='Uri')]
        [string]$SessionId,

        [Parameter(ParameterSetName='Uri')]
        [string]$Username,

        [Parameter(ParameterSetName='Uri')]
        [string]$Password,

        [Parameter(ParameterSetName='Uri')]
        [System.Management.Automation.PSCredential]
        $Credential
    )

    $id = $Session.SessionId

    $newSession = $Session.PSObject.Copy()
    $newSession.PersistentSession = $false
    Invoke-RemoteScript -Session $newSession -ScriptBlock {
        $failureCount = 0
        while($currentSessions = (Get-ScriptSession -Id $using:id -ErrorAction 0)) {
            $shouldSleep = $false
            foreach($currentSession in $currentSessions) {
                if($currentSession.State -ne "Busy") {
                    $currentSession | Remove-ScriptSession
                } else {
                    $shouldSleep = $true
                }
            }

            if($shouldSleep) {
                Start-Sleep -Milliseconds 100
                $failureCount++
            }

            if($failureCount -ge 20) {
                Write-Warning "Unable to remove some script sessions because they were in a Busy state. Session id: $($id)"
                break
            }
        }
    }
}