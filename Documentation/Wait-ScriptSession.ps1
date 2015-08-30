<#
    .SYNOPSIS
        Suppresses script execution command prompt until one or all of the script sessions provided are complete.

    .DESCRIPTION
        The Wait-ScriptSession cmdlet waits for script session to complete before it displays the command prompt or allows the script to continue. You can wait until any script session is complete, or until all script sessions are complete, and you can set a maximum wait time for the script session.
        When the commands in the script session are complete, Wait-ScriptSession displays the command prompt and returns a script session object so that you can pipe it to another command.
        You can use Wait-ScriptSession cmdlet to wait for script sessions, such as those that were started by using the Start-ScriptSession cmdlet.

    .PARAMETER Timeout
        The maximum time to wait for all the other running script sessions to complete.

    .PARAMETER Any
        Returns control to the script or displays the command prompt (and returns the ScriptSession object) when any script session completes. By default, Wait-ScriptSession waits until all of the specified jobs are complete before displaying the prompt.

    .PARAMETER Id
        Id(s) of the session to be stopped. 

    .PARAMETER Session
        Session(s) to be stopped.
    
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
        Stop-ScriptSession

    .LINK
        http://blog.najmanowicz.com/2014/10/26/sitecore-powershell-extensions-persistent-sessions/

    .LINK
        https://git.io/spe

    .EXAMPLE
        PS master:\> Wait-ScriptSession -Id "My Background Script Session"
#>
