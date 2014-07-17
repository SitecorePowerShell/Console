<#
    .SYNOPSIS
        Get-Package.

    .DESCRIPTION
        Get-Package.


    .PARAMETER Path
        Path to the item to be processed - can work with Language parameter to narrow the publication scope.    
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Install.PackageProject

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Get-Package -Path master:\content\home
#>
