<#
    .SYNOPSIS
        Imports a function script from the script library's "Functions" folder.

    .DESCRIPTION
        The Import-Function command imports a function script from the script library's "Functions" folder.

    .PARAMETER Name
        Name of the script in the "Functions" library or one of its sub-libraries.

    .PARAMETER Library
        Name of the library withing the "Functions" library. Provide this name to disambiguate a script from other scripts of the same name that might exist in multiple sub-librarties of the Functions library.

    .PARAMETER Module
        Name of the module "Functions" are going to be taken from. Provide this name to disambiguate a script from other scripts of the same name that might exist in multiple Modules.
        
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        System.Object

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Invoke-Script
    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
	# The following imports a Resolve-Error function that you may later use to get a deeper understanding of a problem with script should one occur by xecuting the "Resolve-Error" command 
        # that was imported as a result of the execution of the following line
        PS master:\> Import-Function -Name Resolve-Error
#>
