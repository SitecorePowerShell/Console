<#
    .SYNOPSIS
        Get-ItemTemplate.

    .DESCRIPTION
        Get-ItemTemplate.


    .PARAMETER Item
        The item to be processed.

    .PARAMETER Recurse
        Process the item and all of its children.    
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Data.Items.TemplateItem

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Get-ItemTemplate -Path master:\content\home
#>
