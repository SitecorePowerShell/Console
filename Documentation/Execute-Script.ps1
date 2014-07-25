<#
    .SYNOPSIS
        Executes a script from Sitecore PowerShell Extensions Script Library.

    .DESCRIPTION
        Executes a script from Sitecore PowerShell Extensions Script Library.


    .PARAMETER Item
        The script item to be executed.

    .PARAMETER Path
        Path to the script item to be executed.
        Path can be absolute or Relavie to Script library root.	e.g. the following two commands are equivalent:
        
        PS master:\> Execute-Script 'master:\system\Modules\PowerShell\Script Library\Examples\Script Testing\Long Running Script with Progress Demo'
        PS master:\> Execute-Script 'Examples\Script Testing\Long Running Script with Progress Demo'
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        System.Object

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Execute-Script 'Examples\Script Testing\Long Running Script with Progress Demo'
#>
