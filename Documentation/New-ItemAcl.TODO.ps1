<#
    .SYNOPSIS
        New-ItemAcl.

    .DESCRIPTION
        New-ItemAcl.


    .PARAMETER Identity
        User name including domain. If no domain is specified - 'sitecore' will be used as the default value

    .PARAMETER PropagationType
        TODO: Provide description for this parameter

    .PARAMETER SecurityPermission
        TODO: Provide description for this parameter

    .PARAMETER AccessRight
        TODO: Provide description for this parameter    
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Security.AccessControl.AccessRule

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> New-ItemAcl -Path master:\content\home
#>
