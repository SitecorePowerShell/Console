<#
    .SYNOPSIS
        Returns the layout for the specified device.

    .DESCRIPTION
        The Get-LayoutDevice command returns the layout for the specified device.


    .PARAMETER Name
        Name of the device to return.

    .PARAMETER Default
        Determines that a default system layout device should be returned.
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Data.Items.DeviceItem

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
        Remove-Rendering

    .LINK
        Get-Layout

    .LINK
        Set-Layout

    .EXAMPLE
        # Get Print device
        PS master:\> Get-LayoutDevice "Print"

    .EXAMPLE
        # Get default device
        PS master:\> Get-LayoutDevice -Default

    .EXAMPLE
        # Get all layout devices
        PS master:\> Get-LayoutDevice *
#>
