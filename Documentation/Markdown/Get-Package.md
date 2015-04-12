# Get-Package 
 
Loads package from the package definition (xml file). 
 
## Syntax 
 
Get-Package [-Path] &lt;String&gt; 
 
 
## Detailed Description 
 
Loads package from the package definition (xml file). Package definitions can be created by PowerShell scripts by using Export-Package commandlet (without the -Zip parameter) 
 
- 
 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Path&nbsp; &lt;String&gt; 
 
Path to the package file. If the path is not absolute the path needs to be relative to the Sitecore Package path defined in the "PackagePath" setting and later exposed in the Sitecore.Shell.Applications.Install.PackageProjectPath
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 
 
 
 
```powershell   
 
PS master:\> Get-Package -Path master:\content\home 
 
``` 
 
## Related Topics 
 
* Export-Package 
 
* Import-Package 
 
* Install-UpdatePackage 
 
* New-ExplicitFileSource 
 
* New-ExplicitItemSource 
 
* New-FileSource 
 
* New-ItemSource 
 
* New-Package 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/> 
 
* <a href='http://blog.najmanowicz.com/2011/12/19/continuous-deployment-in-sitecore-with-powershell/' target='_blank'>http://blog.najmanowicz.com/2011/12/19/continuous-deployment-in-sitecore-with-powershell/</a><br/> 
 
* <a href='https://gist.github.com/AdamNaj/f4251cb2645a1bfcddae' target='_blank'>https://gist.github.com/AdamNaj/f4251cb2645a1bfcddae</a><br/> 
 
* <a href='https://www.youtube.com/watch?v=60BGRDNONo0&list=PLph7ZchYd_nCypVZSNkudGwPFRqf1na0b&index=7' target='_blank'>https://www.youtube.com/watch?v=60BGRDNONo0&list=PLph7ZchYd_nCypVZSNkudGwPFRqf1na0b&index=7</a><br/>

