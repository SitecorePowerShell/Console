# Install-Package 
 
Installs Sitecore package.
This command used to be named Import-Package - a matching alias added for compatibility with older scripts. 
 
## Syntax 
 
Install-Package [[-Path] &lt;String&gt;] [-InstallMode &lt;Undefined | Overwrite | Merge | Skip | SideBySide&gt;] [-MergeMode &lt;Undefined | Clear | Append | Merge&gt;] 
 
 
## Detailed Description 
 
Installs Sitecore package with the ability to provide default responses for merge and overwrite actions. 
 
- 
 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions## Aliases
The following abbreviations are aliases for this cmdlet:  
* Import-Package 
 
## Parameters 
 
### -Path&nbsp; &lt;String&gt; 
 
Path to the package file.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -InstallMode&nbsp; &lt;InstallMode&gt; 
 
Undefined, Overwrite, Merge, Skip, SideBySide
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -MergeMode&nbsp; &lt;MergeMode&gt; 
 
Undefined, Clear, Append, Merge
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 
 
 
 
```powershell   
 
PS master:\> Install-Package -Path SitecorePowerShellConsole.zip - InstallMode Merge -MergeMode Merge 
 
``` 
 
## Related Topics 
 
* Export-Package 
 
* Get-Package 
 
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

