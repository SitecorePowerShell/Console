<#
    .SYNOPSIS
        Install-UpdatePackage.

    .DESCRIPTION
        Install-UpdatePackage.


    .PARAMETER Path
        Path to the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER RollbackPackagePath
        TODO: Provide description for this parameter

    .PARAMETER UpgradeAction
        TODO: Provide description for this parameter

    .PARAMETER InstallMode
        TODO: Provide description for this parameter    
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Update.Installer.ContingencyEntry

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Install-UpdatePackage -Path master:\content\home
#>
