<#
    .SYNOPSIS
        Get-ItemAcl.

    .DESCRIPTION
        Get-ItemAcl.


    .PARAMETER Identity
        User name including domain. If no domain is specified - 'sitecore' will be used as the default value

    .PARAMETER Filter
        TODO: Provide description for this parameter

    .PARAMETER Item
        The item to be processed.

    .PARAMETER Path
        Path to the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Id
        Id of the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Database
        Database containing the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Language
        If you need the item in specific Language You can specify it with this parameter. Globbing/wildcard supported.    
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Security.AccessControl.AccessRule

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Get-ItemAcl -Path master:\content\home
#>
