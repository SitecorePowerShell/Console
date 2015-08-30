<#
    .SYNOPSIS
        Sets item layout for a device.

    .DESCRIPTION
        Sets item layout for a specific device provided


    .PARAMETER Device
        Device for which to set layout.

    .PARAMETER Layout
        Sitecore item defining the layout.

    .PARAMETER Item
        The item to be processed.

    .PARAMETER Path
        Path to the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Id
        Id of the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Database
        Database containing the item to be processed - can work with Language parameter to narrow the publication scope.
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

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
        Get-Layout

    .LINK
        Reset-Layout

    .EXAMPLE
        PS master:\> Set-Layout -Path master:\content\home
#>
