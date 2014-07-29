<#
    .SYNOPSIS
        Retrieves Layout of an item.

    .DESCRIPTION
        Returns a Layout set on an item for a specific device.

    .PARAMETER Device
        Layout Device for which the item should be returned. If not specified All layouts used will be returned. 
        If Device is specified but no layout is specified the commandlet will return an error that can be silenced

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
        Sitecore.Data.Items.Item

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
        Set-Layout


    .EXAMPLE
        PS master:\> Get-Layout -Path master:\content\home
#>
