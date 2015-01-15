<#
    .SYNOPSIS
        Executes a script from Sitecore PowerShell Extensions Script Library.
        This command used to be named Execute-Script - a matching alias added for compatibility with older scripts.

    .DESCRIPTION
        Executes a script from Sitecore PowerShell Extensions Script Library.


    .PARAMETER Item
        The script item to be executed.

    .PARAMETER Path
        Path to the script item to be executed.
        Path can be absolute or Relavie to Script library root.	e.g. the following two commands are equivalent:
        
        PS master:\> Invoke-Script 'master:\system\Modules\PowerShell\Script Library\Examples\Script Testing\Long Running Script with Progress Demo'
        PS master:\> Invoke-Script 'Examples\Script Testing\Long Running Script with Progress Demo'
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        System.Object

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .LINK
        Import-Function

    .EXAMPLE
        PS master:\> Invoke-Script 'Examples\Script Testing\Long Running Script with Progress Demo'
#>
