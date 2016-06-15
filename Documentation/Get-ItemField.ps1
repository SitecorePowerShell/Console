<#
    .SYNOPSIS
        Retrieves item fields as either names or fields or template fields.

    .DESCRIPTION
        Retrieves item fields as either names or fields or template fields.


    .PARAMETER IncludeStandardFields
        Includes fields that are defined on "Standard template"

    .PARAMETER ReturnType
        Determines type returned. The possible values include:
        - Name - strings with field names.
        - Field - fields on the item
        - TemplateField - template fields. 

    .PARAMETER Name
        Array of names to include - supports wildcards.

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
        Sitecore.Data.Items.Item
        Sitecore.Data.Templates.TemplateField
        Sitecore.Data.Fields.Field

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Get-ItemTemplate

    .LINK
        Reset-ItemField

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        # Get list of names of non standard fields from /sitecore/content/home item
        PS master:\> Get-ItemField -Path master:\content\home
        
        Text
        Title
        Image

    .EXAMPLE
        # Get list of fields including standard fields from /sitecore/content/home item and list their Name, DisplayName, SectionDisplayName and Description in a table.
        PS master:\> Get-Item master:\content\home | Get-ItemField -IncludeStandardFields -ReturnType Field -Name "*" | ft Name, DisplayName, SectionDisplayName, Description -auto
        
        Name                                DisplayName                        SectionDisplayName Description
        ----                                -----------                        ------------------ -----------
        __Revision                          Revision                           Statistics
        __Standard values                   __Standard values                  Advanced
        __Updated by                        Updated by                         Statistics
        __Validate Button Validation Rules  Validation Button Validation Rules Validation Rules
        __Created                           Created                            Statistics
        __Thumbnail                         Thumbnail                          Appearance
        __Insert Rules                      Insert Rules                       Insert Options
        __Short description                 Short description                  Help
        __Created by                        Created by                         Statistics
        __Presets                           Presets                            Layout
        Text                                Text                               Data               The text is the main content of the document.
#>
