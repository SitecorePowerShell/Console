<#
    .SYNOPSIS
        Converts an item from a clone to a fully independent item.

    .DESCRIPTION
        Converts an item from a clone to a fully independent item.

    .PARAMETER Item
        The item to be converted.

    .PARAMETER Path
        Path to the item to be converted

    .PARAMETER Id
        Id of the item to be converted

    .PARAMETER Database
        Database containing the item to be converted
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Data.Items.Item

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        New-ItemClone

    .LINK
        Get-ItemClone

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> ConvertFrom-ItemClone -Path master:\content\home
#>
