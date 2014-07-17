<#
    .SYNOPSIS
        Add-ItemLanguage.

    .DESCRIPTION
        Add-ItemLanguage.


    .PARAMETER Recurse
        Process the item and all of its children.

    .PARAMETER IfExist
        TODO: Provide description for this parameter

    .PARAMETER TargetLanguage
        TODO: Provide description for this parameter

    .PARAMETER DoNotCopyFields
        TODO: Provide description for this parameter

    .PARAMETER IgnoredFields
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
        PS master:\> Add-ItemLanguage -Path master:\content\home
# >
