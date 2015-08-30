<#
    .SYNOPSIS
        Starts a new Script Session and executes a script provided in it.

    .DESCRIPTION
        Starts a new Script Session and executes a script provided in it. 
        The session can be a background session or if the caller session is interactive providint the -Interactive switch can open a Windowd for the new session

    .PARAMETER Id
        Id of the session to be created or retrieved. If the session with the same ID exists - it will be used, unless it's busy - in which case an error will be raised.
        If a session with the Id provided does not exist - it will be created.
        The Id is a string that uniquely identifies the script session within the server. You can type one or more IDs (separated by commas). To find the ID of a script session, type "Get-ScriptSession" without parameters.

    .PARAMETER Session
        Specifies the script session in context of which the script should be executed. Enter a variable that contains the script session or a command that gets the script session. You can also pipe a script session object to Start-ScriptSession.
        If the session is busy at the moment of the call - an error will be raised instead of running the script.

    .PARAMETER Item
        Script item containing the code to be executed.

    .PARAMETER Path
        Path to the script item containing the code to be executed.

    .PARAMETER ScriptBlock
        Script to be executed.

    .PARAMETER JobName
        Name of the Sitecore job that will run the script session. This can be used to monitor the session progress.

    .PARAMETER ArgumentList
        Hashtable with the additional parameters required by the invoked script. The parameters will be instantiated in the session as variables.

    .PARAMETER Identity
        User name including domain in context of which the script will be executed. If no domain is specified - 'sitecore' will be used as the default value. 
        If user is not specified the current user will be the impersonation context.

    .PARAMETER DisableSecurity
        Add this parameter to disable security in the Job running the script session.

    .PARAMETER AutoDispose
        Providing this parameter will cause the session to be automatically destroyed after it has executed. 
        Use this parameter if you're not in need of the results of the script execution.

    .PARAMETER Interactive
        If the new session is run from an interactive session (e.g. from desktop, menu item, console or ISE) using this parameter will cause dialog to be shown to the user to monitor the script progress.

    .PARAMETER ContextItem
        Context item for the script session. The script will start in the location of the item.
    
    .INPUTS
        Sitecore.Data.Items.Item
    
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
        Stop-ScriptSession

    .LINK
        Wait-ScriptSession

    .LINK
        http://blog.najmanowicz.com/2014/10/26/sitecore-powershell-extensions-persistent-sessions/

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        # Start the progress demo script in interactive mode (showing dialogs for each script) in 3 different ways.
        # In the first case script path is used, second case shows the script item beign retrieved and provided to the cmdlet.
        # The last case shows the script to be provided as a script block (script content)
        # Script finishes before the sessions that were launched from it end. 
        # The sessions will be disposed when user presses the "Close" button in their dialogs as the -AutoDispose parameter was provided.
        $scriptPath = "master:\system\Modules\PowerShell\Script Library\Getting Started\Script Testing\Long Running Script with Progress Demo"
        $scriptItem = Get-Item $scriptPath
        $script = [scriptblock]::Create($scriptItem.Script)
        Start-ScriptSession -Path $scriptPath -Interactive -AutoDispose
        Start-ScriptSession -Item $scriptItem -Interactive -AutoDispose
        Start-ScriptSession -ScriptBlock $script -Interactive -AutoDispose

    .EXAMPLE
        # Starts a script that changes its path to "master:\" and sleeps for 4 seconds. The session will persist in memory as no -AutoDispose parameter has been provided
	Start-ScriptSession -ScriptBlock { cd master:\; Start-Sleep -Seconds 4 } -Id "Background Task"

#>
