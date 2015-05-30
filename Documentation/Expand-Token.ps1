<#
    .SYNOPSIS
        Expands tokens in fields for items.

    .DESCRIPTION
        The Expand-Token command expands the tokens in fields for items.
        
        Some example of tokens include:
        - $name
        - $time

    .PARAMETER Item
        The item to be processed.    
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Data.Items.Item

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .LINK
        http://sitecorejunkie.com/2014/05/27/launch-powershell-scripts-in-the-item-context-menu-using-sitecore-powershell-extensions/

    .LINK
        http://sitecorejunkie.com/2014/06/02/make-bulk-item-updates-using-sitecore-powershell-extensions/

    .EXAMPLE
        PS master:\> Get-Item master:\content\home | Expand-Token
#>
