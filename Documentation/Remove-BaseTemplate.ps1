<#
    .SYNOPSIS
        Remove one or more base templates from a template item.

    .DESCRIPTION
        The Remove-BaseTemplate command removes one or more base templates from a template item.

    .PARAMETER Item
        The item to remove the base template from.

    .PARAMETER Path
        Path to the item to remove the base template from.

    .PARAMETER Id
        Id of the item to remove the base template from.

    .PARAMETER Database
        Database containing the item to remove the base template from - required if item is specified with Id.
    
    .PARAMETER TemplateItem
        Sitecore item or list of items of base templates to remove.
        
    .PARAMETER Template
        Path representing the template item to remove as a base template. This must be of the same database as the item to be altered.
        Note that this parameter only supports a single template.       
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS

    .NOTES
        Help Author: Adam Najmanowicz, Michael West, Alex Washtell

    .LINK
        Add-BaseTemplate
        
    .LINK
        Get-ItemTemplate
        
    .LINK
        Set-ItemTemplate

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
	#Remove base template of /sitecore/templates/User Defined/BaseTemplate from a template, using a path.

        PS master:\> Remove-BaseTemplate -Path "master:/sitecore/content/User Defined/Page" -Template "/sitecore/templates/User Defined/BaseTemplate"

    .EXAMPLE
	#Remove multiple base templates from a template, using items.
    
        PS master:\> $baseA = Get-Item -Path master:/sitecore/content/User Defined/BaseTemplateA
        PS master:\> $baseB = Get-Item -Path master:/sitecore/content/User Defined/BaseTemplateB
        PS master:\> Remove-BaseTemplate -Path "master:/sitecore/content/User Defined/Page" -TemplateItem @($baseA, $baseB)

#>
