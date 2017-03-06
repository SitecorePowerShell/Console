<#
    .SYNOPSIS
        Sets the item template.

    .DESCRIPTION
        The Set-ItemTemplate command sets the template for an item.

    .PARAMETER Item
        The item to set the template for.

    .PARAMETER Path
        Path to the item to set the template for.

    .PARAMETER Id
        Id of the item to set the template for.

    .PARAMETER Database
        Database containing the item to set the template for - required if item is specified with Id.
    
    .PARAMETER TemplateItem
        Sitecore item representing the template.
        
    .PARAMETER Template
        Path representing the template item. This must be of the same database as the item to be altered.

    .PARAMETER FieldsToCopy
        Hashtable of key value pairs mapping the old template field to a new template field.

        @{"Title"="Headline";"Text"="Copy"}
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS

    .NOTES
        Help Author: Adam Najmanowicz, Michael West, Alex Washtell

    .LINK
        Get-ItemTemplate
        
    .LINK
        Add-BaseTemplate
        
    .LINK
        Remove-BaseTemplate        

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
	#Set template of /sitecore/content/home item using a Template path.

        PS master:\> Set-ItemTemplate -Path master:/sitecore/content/home -Template "/sitecore/templates/User Defined/Page"

    .EXAMPLE
	# Set template of /sitecore/content/home item using a TemplateItem.
    
        PS master:\> $template = Get-ItemTemplate -Path master:\content\home\page1
        PS master:\> Set-ItemTemplate -Path master:\content\home\page2 -TemplateItem $template

    .EXAMPLE
    # Set the template and remap fields to their new name.
        Set-ItemTemplate -Path "master:\content\home\Page1" `
        -Template "User Defined/Target" `
        -FieldsToCopy @{Field1="Field4"; Field2="Field5"; Field3="Field6"}

#>
