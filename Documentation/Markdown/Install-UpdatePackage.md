# Install-UpdatePackage 
 
Installs .update that are used by Sitecore CMS updates, TDS and are created by Courier 
 
## Syntax 
 
Install-UpdatePackage [-Path] &lt;String&gt; [[-RollbackPackagePath] &lt;String&gt;] -UpgradeAction &lt;Preview | Upgrade&gt; -InstallMode &lt;Install | Update&gt; 
 
 
## Detailed Description 
 
Installs .update that are used by Sitecore CMS updates, TDS and are created by Courier

Install-UpdatePackage.
    Install-UpdatePackage -Path "C:\Projects\LaunchSitecore.TDSMaster.update" 
    -UpgradeAction {Preview or Upgrade}
    -InstallMode {Install or Update} 
 
© 2010-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Path&nbsp; &lt;String&gt; 
 
Path to the .update package on the Sitecore server disk drive.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -RollbackPackagePath&nbsp; &lt;String&gt; 
 
Specify Rollback Package Path - for rolling back if the installation was not functioning as expected.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -UpgradeAction&nbsp; &lt;UpgradeAction&gt; 
 
Preview / Upgrade
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -InstallMode&nbsp; &lt;InstallMode&gt; 
 
Install / Update
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 
 
 
 
```powershell   
 
PS master:\> Install-UpdatePackage -Path "C:\Projects\LaunchSitecore.TDSMaster.update" -UpgradeAction Preview -InstallMode Install 
 
``` 
 
## Related Topics 
 
* Export-UpdatePackage 
 
* Get-UpdatePackageDiff 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>

