<#
    .SYNOPSIS
        Retrieves the object that describes a Sitecore PowerShell Extensions Module

    .DESCRIPTION
        Retrieves the object that describes a Sitecore PowerShell Extensions Module.


    .PARAMETER Item
        A script or library item that is defined within the module to be returned.

    .PARAMETER Path
        Path to a script or library item that is defined within the module to be returned.

    .PARAMETER Id
        Id of a script or library item that is defined within the module to be returned.

    .PARAMETER Database
        Database containing the module to be returned.

    .PARAMETER Name
        Name fo the module to return. Supports wildcards.
    
    .INPUTS
        Sitecore.Data.Items.Item
        System.String
    
    .OUTPUTS
        Cognifide.PowerShell.Core.Modules.Module

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Get-SpeModuleFeatureRoot

    .LINK
        http://blog.najmanowicz.com/2014/11/01/sitecore-powershell-extensions-3-0-modules-proposal/

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE        
        #Return all modules defined in the provided database
        PS master:\> Get-SpeModule -Database (Get-Database "master")

    .EXAMPLE        
        #Return all modules defined in the master database Matching the "Content*" wildcard
        PS master:\> Get-SpeModule -Database (Get-Database "master")

    .EXAMPLE        
        #Return the module the piped script belongs to
        PS master:\> Get-item "master:\system\Modules\PowerShell\Script Library\Copy Renderings\Content Editor\Context Menu\Layout\Copy Renderings" |  Get-SpeModule
        
#>
