<#
    .SYNOPSIS
        Show-FieldEditor.

    .DESCRIPTION
        Show-FieldEditor.


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

    .PARAMETER PreserveSections
        TODO: Provide description for this parameter

    .PARAMETER SectionTitle
        TODO: Provide description for this parameter

    .PARAMETER SectionIcon
        TODO: Provide description for this parameter

    .PARAMETER FieldName
        TODO: Provide description for this parameter

    .PARAMETER Title
        TODO: Provide description for this parameter

    .PARAMETER Width
        TODO: Provide description for this parameter

    .PARAMETER Height
        TODO: Provide description for this parameter    
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        System.String

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Show-FieldEditor -Path master:\content\home
# >
