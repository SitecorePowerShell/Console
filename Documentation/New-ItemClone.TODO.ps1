<#
    .SYNOPSIS
        New-ItemClone.

    .DESCRIPTION
        New-ItemClone.


    .PARAMETER Destination
        TODO: Provide description for this parameter

    .PARAMETER Name
        TODO: Provide description for this parameter

    .PARAMETER Recursive
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
        Sitecore.Data.Items.Item

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> New-ItemClone -Path master:\content\home
# >
