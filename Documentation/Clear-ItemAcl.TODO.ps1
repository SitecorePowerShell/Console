<#
    .SYNOPSIS
        Clear-ItemAcl.

    .DESCRIPTION
        Clear-ItemAcl.


    .PARAMETER Language
        If you need the item in specific Language You can specify it with this parameter. Globbing/wildcard supported.

    .PARAMETER Item
        The item to be processed.

    .PARAMETER Path
        Path to the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Id
        Id of the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Database
        Database containing the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER WhatIf
        TODO: Provide description for this parameter

    .PARAMETER Confirm
        TODO: Provide description for this parameter    
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Data.Items.Item

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Clear-ItemAcl -Path master:\content\home
#>
