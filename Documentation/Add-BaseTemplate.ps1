<#
    .SYNOPSIS
        Add one or more base templates to a template item.

    .DESCRIPTION
        The Add-BaseTemplate command adds one or more base templates to a template item.

    .PARAMETER Item
        The item to add the base template to.

    .PARAMETER Path
        Path to the item to add the base template to.

    .PARAMETER Id
        Id of the item to add the base template to.

    .PARAMETER Database
        Database containing the item to add the base template to - required if item is specified with Id.
    
    .PARAMETER TemplateItem
        Sitecore item or list of items of base templates to add.
        
    .PARAMETER Template
        Path representing the template item to add as a base template. This must be of the same database as the item to be altered.
        Note that this parameter only supports a single template.       
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS

    .NOTES
        Help Author: Adam Najmanowicz, Michael West, Alex Washtell

    .LINK
        Remove-BaseTemplate
        
    .LINK
        Get-ItemTemplate
        
    .LINK
        Set-ItemTemplate    

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
	#Add base template of /sitecore/templates/User Defined/BaseTemplate to a template, using a path.

        PS master:\> Add-BaseTemplate -Path "master:/sitecore/content/User Defined/Page" -Template "/sitecore/templates/User Defined/BaseTemplate"

    .EXAMPLE
	#Add multiple base templates to a template, using items.
    
        PS master:\> $baseA = Get-Item -Path master:/sitecore/content/User Defined/BaseTemplateA
        PS master:\> $baseB = Get-Item -Path master:/sitecore/content/User Defined/BaseTemplateB
        PS master:\> Add-BaseTemplate -Path "master:/sitecore/content/User Defined/Page" -TemplateItem @($baseA, $baseB)

#>
