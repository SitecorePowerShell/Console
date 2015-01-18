<#
    .SYNOPSIS
        Installs .update that are used by Sitecore CMS updates, TDS and are created by Courier

    .DESCRIPTION
        Installs .update that are used by Sitecore CMS updates, TDS and are created by Courier

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
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Install-UpdatePackage -Path "C:\Projects\LaunchSitecore.TDSMaster.update" 
            -UpgradeAction Preview -InstallMode Install
#>
