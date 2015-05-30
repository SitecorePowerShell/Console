<#
    .SYNOPSIS
        Gets all available domains or the specified domain.

    .DESCRIPTION
        The Get-Domain command returns all the domains or the specified domain.

    .PARAMETER Name
        The name of the domain    
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Security.Domains.Domain

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .LINK
        Remove-Domain

    .LINK
        New-Domain

    .EXAMPLE
        PS master:\> Get-Domain -Path master:\content\home
#>
