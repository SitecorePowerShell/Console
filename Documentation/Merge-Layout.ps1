<#
    .SYNOPSIS
        Merges the Final Layout into the Shared Layout.

    .DESCRIPTION
        Merges the Final Layout on the item with the Shared Layout. The Final Layout will then be reset.

    .PARAMETER Language
        If you need the item in specific Language You can specify it with this parameter. Globbing/wildcard supported.

    .PARAMETER Item
        The item to be processed.

    .PARAMETER Path
        Path to the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Id
        Id of the item to be processed - can work with Language parameter to specify the language other than current session language. Requires the Database parameter to be specified.

    .PARAMETER Database
        Database containing the item to be fetched with Id parameter.
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        None

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .EXAMPLE
        PS master:\> Get-Item -Path master:\content\home | Merge-Layout
#>
