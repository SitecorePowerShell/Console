<#
    .SYNOPSIS
        Resets item fields, specified as either names, fields or template fields.

    .DESCRIPTION
        Resets item fields, specified as either names, fields or template fields.

    .PARAMETER IncludeStandardFields
        Includes fields that are defined on "Standard template"

    .PARAMETER Name
        Array of field names to include - supports wildcards.

    .PARAMETER Item
        The item to be analysed.

    .PARAMETER Path
        Path to the item to be analysed.

    .PARAMETER Id
        Id of the item to be analysed.

    .PARAMETER Path
        Path to the item to be analysed - additionally specify Language parameter to fetch different item language than the current user language.

    .PARAMETER Id
        Id of the the item to be analysed - additionally specify Language parameter to fetch different item language than the current user language.

    .PARAMETER Database
        Database containing the item to be analysed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Language
        Language that will be analysed. If not specified the current user language will be used. Globbing/wildcard supported.

    .INPUTS
        Sitecore.Data.Items.Item

    .OUTPUTS
        None

    .NOTES
        Help Author: Adam Najmanowicz, Michael West, Alex Washtell

    .LINK
        Get-ItemTemplate

    .LINK
        Get-ItemField

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        # Reset all item fields, excluding standard fields.
        PS master:\> Reset-ItemField -Path master:\content\home

    .EXAMPLE
        # Reset all item fields, including standard fields.
        PS master:\> Reset-ItemField -Path master:\content\home -IncludeStandardFields

    .EXAMPLE
        # Reset all item fields with names beginning with "a", excluding standard fields.
        PS master:\> Get-Item master:\content\home | Reset-ItemField -Name "a*

#>
