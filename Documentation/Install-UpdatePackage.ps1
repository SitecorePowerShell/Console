<#
    .SYNOPSIS
        Installs a Sitecore update package from the specified path.

    .DESCRIPTION
        The Install-UpdatePackage command installs update packages that are used created by Sitecore CMS updates, TDS, and Courier.

        Install-UpdatePackage.
            Install-UpdatePackage -Path "C:\Projects\LaunchSitecore.TDSMaster.update" 
            -UpgradeAction {Preview or Upgrade}
            -InstallMode {Install or Update}

    .PARAMETER Path
        Path to the .update package on the Sitecore server disk drive.

    .PARAMETER RollbackPackagePath
        Specify Rollback Package Path - for rolling back if the installation was not functioning as expected.

    .PARAMETER UpgradeAction
        Preview or Upgrade

    .PARAMETER InstallMode
        Install or Update
    
    .INPUTS
        
    
    .OUTPUTS
        Sitecore.Update.Installer.ContingencyEntry

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Export-UpdatePackage

    .LINK
        Get-UpdatePackageDiff

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Install-UpdatePackage -Path "C:\Projects\LaunchSitecore.TDSMaster.update" -UpgradeAction Preview -InstallMode Install
#>
