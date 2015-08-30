<#
    .SYNOPSIS
        Stops executing script session.

    .DESCRIPTION
        Aborts the pipeline of a session that is executing. This will stop the session immediately in its next PowerShell command.
        Caution! If your script is running a long operation in the .net code rather than in PowerShell - the session will abort after the code has finished and the control was returned to the script.

    .PARAMETER Id
       Stops the script session with the specified IDs.
       The ID is a string that uniquely identifies the script session within the server. You can type one or more IDs (separated by commas). To find the ID of a script session, type "Get-ScriptSession" without parameters.

    .PARAMETER Session
        Specifies the script session to be stopped. Enter a variable that contains the script session or a command that gets the script session. You can also pipe a script session object to Receive-ScriptSession.
        
    .INPUTS
        System.String or Cognifide.PowerShell.Core.Host.ScriptSession
    
    .OUTPUTS
        Cognifide.PowerShell.Core.Host.ScriptSession

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Get-ScriptSession

    .LINK
        Receive-ScriptSession

    .LINK
        Remove-ScriptSession

    .LINK
        Start-ScriptSession

    .LINK
        Wait-ScriptSession

    .LINK
        http://blog.najmanowicz.com/2014/10/26/sitecore-powershell-extensions-persistent-sessions/

    .LINK
        https://git.io/spe

    .EXAMPLE
        The following stops the script session with the specified Id.

        PS master:\> Stop-ScriptSession -Id "My Background Script Session"
#>
