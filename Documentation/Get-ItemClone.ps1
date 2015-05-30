<#
    .SYNOPSIS
        Returns all the clones for the specified item.

    .DESCRIPTION
        The Get-ItemClone command returns all the clones for the specified item.


    .PARAMETER Item
        The item to be analysed for clones presence.

    .PARAMETER Path
        Path to the item to be analysed for clones presence.

    .PARAMETER Id
        Id of the item to be analysed for clones presence.

    .PARAMETER Database
        Database containing the item to be processed - if item is being provided through Id.

    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Data.Items.Item

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        New-ItemClone

    .LINK
        ConvertFrom-ItemClone

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .LINK
        https://github.com/SitecorePowerShell/Console/issues/218

    .LINK
        Get-Item

    .EXAMPLE
        PS master:\> Get-ItemClone -Path master:\content\home
#>
