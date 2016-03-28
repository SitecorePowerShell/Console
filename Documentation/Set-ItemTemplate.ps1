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
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS

    .NOTES
        Help Author: Adam Najmanowicz, Michael West, Alex Washtell

    .LINK
        Get-ItemTemplate

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
	#Set template of /sitecore/content/home item using a Template path

        PS master:\> Set-ItemTemplate -Path master:/sitecore/content/home -Template "/sitecore/templates/User Defined/Page"

    .EXAMPLE
	# Set template of /sitecore/content/home item using a TemplateItem
    
        PS master:\> $template = Get-ItemTemplate -Path master:\content\home\page1
        PS master:\> Set-ItemTemplate -Path master:\content\home\page2 -TemplateItem $template

#>
