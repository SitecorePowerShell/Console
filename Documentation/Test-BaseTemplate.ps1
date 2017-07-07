<#
    .SYNOPSIS
        Determines if the item inherits from the specified base template.

    .DESCRIPTION
        Returns a true value if the item inherits from the specified base template.

    .PARAMETER Item
        The item to be processed.

    .PARAMETER Path
        Path to the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Id
        Id of the item to be processed - can work with Language parameter to specify the language other than current session language. Requires the Database parameter to be specified.

    .PARAMETER TemplateItem
        The template instance to use when evaluating the inheritance.

    .PARAMETER Template
        The template path to use when evaluating the inheritance.

    .PARAMETER Database
        Database containing the item to be fetched with Id parameter. 
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Boolean

    .LINK
        Add-BaseTemplate

    .LINK
        Remove-BaseTemplate

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .EXAMPLE
        PS master:\> Test-BaseTemplate -Path "master:/sitecore/content/Home" -Template "Common/Folder"

        False
#>
