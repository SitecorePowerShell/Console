<#
    .SYNOPSIS
        Resets the layout for the specified item.

    .DESCRIPTION
        The Reset-Layout command resets the layout for the specified item.

    .PARAMETER Item
        The item to be processed.

    .PARAMETER Path
        Path to the item to be processed.

    .PARAMETER Id
        Id of the item to be processed.

    .PARAMETER Database
        Database containing the item to be processed.

    .INPUTS
        Sitecore.Data.Items.Item

    .OUTPUTS
        None.

    .NOTES
        Help Author: Adam Najmanowicz, Michael West, Alex Washtell

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .LINK
        Add-Rendering

    .LINK
        New-Rendering

    .LINK
        Set-Rendering

    .LINK
        Get-Rendering

    .LINK
        Get-LayoutDevice

    .LINK
        Remove-Rendering

    .LINK
        Set-Layout

    .LINK
        Get-Layout

    .EXAMPLE
        PS master:\> Reset-Layout -Path master:\content\home
#>
