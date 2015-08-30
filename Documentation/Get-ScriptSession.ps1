<#
    .SYNOPSIS
        Returns the list of PowerShell Extensions script sessions running in the system.

    .DESCRIPTION
        The Get-ScriptSession command returns the list of PowerShell Extensions script sessions running in the system.
        To find all script sessions, running in the system type "Get-ScriptSession" without parameters.

    .PARAMETER Id
       Gets the script session with the specified IDs.
       The ID is a string that uniquely identifies the script session within the server. You can type one or more IDs (separated by commas). To find the ID of a script session, type "Get-ScriptSession" without parameters.

    .PARAMETER Current
        Returns current script session if the session is run in a background job.

    .PARAMETER SessionType
	Type of the script session to be retrieved.
        The SessionType is a string that identifies where the session has been launched. You can type one or more session types (separated by commas) and use wildcards to filter. To find currently running types of a script session, type "Get-ScriptSession" without parameters.

    .PARAMETER State
	Type of the script session to be retrieved.
        The parameter limits script sessions to be returned to only those in a specific state, the values should be "Busy" or "Available".  To find states of currently running script sessions, type "Get-ScriptSession" without parameters.

    .INPUTS
        None

    .OUTPUTS
        Cognifide.PowerShell.PowerShellIntegrations.Host.ScriptSession        

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Receive-ScriptSession

    .LINK
        Remove-ScriptSession

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
        PS master:\>Get-ScriptSession
         
        Type         Key                                                                              Location                                 Auto Disposed
        ----         ---                                                                              --------                                 -------------
        Console      $scriptSession$|zwlyrcmmzwisv22djsv0ej2a|8d5c3e63-3fed-0532-e7c5-761760567b83                                             False
        Context      $scriptSession$|zwlyrcmmzwisv22djsv0ej2a|renderingCopySession                    master:\content\Home                     False
        Context      $scriptSession$|zwlyrcmmzwisv22djsv0ej2a|packageBuilder                          master:\content\Home                     False

    .EXAMPLE
        PS master:\>Get-ScriptSession -Current
         
        Type         Key                                                                              Location                                 Auto Disposed
        ----         ---                                                                              --------                                 -------------
        Console      $scriptSession$|zwlyrcmmzwisv22djsv0ej2a|8d5c3e63-3fed-0532-e7c5-761760567b83                                             False
#>
