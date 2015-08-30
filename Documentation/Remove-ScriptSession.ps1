<#
    .SYNOPSIS
        Removes a persistent Script Session from memory.

    .DESCRIPTION
        Removes a persistent Script Session from memory.

    .PARAMETER Id
        Id of the PowerShell session to be removed from memory.

    .PARAMETER Session
        Session to be removed.
    
    .INPUTS
        Cognifide.PowerShell.Core.Host.ScriptSession
	# Resident Script Session obtained through Get-ScriptSession

    .INPUTS
        System.String
	# Id of a resident Script Session

    .OUTPUTS
        

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Get-ScriptSession

    .LINK
        Receive-ScriptSession

    .LINK
        Start-ScriptSession

    .LINK
        Stop-ScriptSession

    .LINK
        Wait-ScriptSession

    .LINK
        https://git.io/spe

    .LINK
        http://blog.najmanowicz.com/2014/10/26/sitecore-powershell-extensions-persistent-sessions/

    .EXAMPLE
        The following removes the script session using the specified Id.

        PS master:\> Remove-ScriptSession -Id "Long running script"
#>
