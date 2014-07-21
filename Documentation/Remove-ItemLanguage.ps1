<#
    .SYNOPSIS
        Removes Language from a single item or a branch of items

    .DESCRIPTION
        Removes Language version from a an Item either sent from pipeline or defined with Path or ID. A single language or a list of languages can be defined using the Language parameter. 
	Language  parameter supports globbing so you can delete whole language groups using wildcards.


    .PARAMETER Recurse
        Deleted language versions from the item and all of its children.

    .PARAMETER Language
        Language(s) that should be deleted form the provided item(s).
        A single language or a list of languages can be defined using the parameter. 
	Language parameter supports globbing so you can delete whole language groups using wildcards.

    .PARAMETER Item
        The item to be processed.

    .PARAMETER Path
        Path to the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Id
        Id of the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Database
        Database containing the item to be processed - can work with Language parameter to narrow the publication scope.    
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .LINK
        Add-ItemLanguage

    .LINK
        Remove-Item

    .EXAMPLE
        # Remove Polish and Spanish language from /sitecore/content/home item in the master database
        PS master:\> Remove-ItemLanguage -Path master:\content\home -Language "pl-pl", "es-es"

    .EXAMPLE
        # Remove all english based languages defined in /sitecore/content/home item and all of its children in the master database
        PS master:\> Remove-ItemLanguage -Path master:\content\home -Language "en-*" -Recurse

#>
