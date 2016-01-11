<#
    .SYNOPSIS
        Creates a version of the item in a new language based on an existing language version.

    .DESCRIPTION
        Creates a new version of the item in a specified language based on an existing language/version.
        Based on parameters you can make the command bahave differently when a version in the target language already exists and define which fields if any should be copied over from the original language.

    .PARAMETER Recurse
        Process the item and all of its children.

    .PARAMETER IfExist
        Default vaule is Append
        Accepts one of 3 pretty self explanatory actions: 
	- Append - [Default] if language version exists create a new version with values copied from the original language
	- Skip - if language version exists don't do anything
	- OverwriteLatest - if language version exists overwrite the last version with values copied from the original language

    .PARAMETER TargetLanguage
        Language or a list of languages that should be created

    .PARAMETER DoNotCopyFields
        Creates a new version in the target language but does not copy field values from the original language

    .PARAMETER IgnoredFields
        List of fields that should not be copied over from original item. As an example, use "__Security" if you don't want the new version to have the same restrictions as the original version.
        
        In addition to the fields in -IgnoredFields the following fields are ignored as configured in Cognifide.PowerShell.config file in the following location:
	configuration/sitecore/powershell/translation/ignoredFields.
	
    Fields ignored out of the box include:
    - __Archive date
    - __Archive Version date
    - __Lock
    - __Owner
    - __Page Level Test Set Definition
    - __Reminder date
    - __Reminder recipients
    - __Reminder text

    .PARAMETER Item
        The item / version to be processed.

    .PARAMETER Path
        Path to the item to be processed - additionally specify Language parameter to fetch different item language than the current user language.

    .PARAMETER Id
        Id of the item to be processed - additionally specify Language parameter to fetch different item language than the current user language.

    .PARAMETER Database
        Database containing the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Language
        Language that will be used as source language. If not specified the current user language will be used.
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Data.Items.Item

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .LINK
        Remove-ItemVersion

    .LINK
        New-Item

    .LINK
        https://gist.github.com/AdamNaj/b36ea095e3668c22c07e

    .EXAMPLE
        # Translate the Home Item from English to US and Polish leaving the "Title" field blank. If a version exists don't do anything
        PS master:\> Add-ItemVersion -Path "master:\content\home" -Language "en" -TargetLanguage "pl-pl", "en-us" -IfExist Skip -IgnoredFields "Title"

    .EXAMPLE
        Add a Japanese version to /sitecore/content/home item in the master database based on itself
        PS master:\> Add-ItemVersion -Path "master:\content\home" -Language ja-JP -IfExist Append

    .EXAMPLE
        # Translate the children of Home item (but only those of Template Name "Sample Item") from English to US and Polish. If a version exists create a new version for that language. Display results in a table listing item name, language and created version number.

        Get-ChildItem "master:\content\home" -Language "en" -Recurse | `
            Where-Object { $_.TemplateName -eq "Sample Item" } | `
            Add-ItemVersion -TargetLanguage "pl-pl" -IfExist Append | `
            Format-Table Name, Language, Version -auto                   
#>
