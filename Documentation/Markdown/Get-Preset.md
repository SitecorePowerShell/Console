# Get-Preset 
 
Returns a serialization preset for use with Serialize-Item. 
 
## Syntax 
 
Get-Preset [[-Name] &lt;String[]&gt;] 
 
 
## Detailed Description 
 
Returns a serialization preset for use with Serialize-Item. 
 
- 
 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Name&nbsp; &lt;String[]&gt; 
 
Name of the serialization preset.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 
 
 
 
```powershell   
 
PS master:\> Get-Preset -Name "PowerShell", "AssetsOptimiser" | ft PresetName, Database, Path -AutoSize

PresetName      Database Path
----------      -------- ----
PowerShell      core     /sitecore/templates/Modules/PowerShell Console
PowerShell      core     /sitecore/system/Modules/PowerShell/Console Colors
PowerShell      core     /sitecore/system/Modules/PowerShell/Script Library
PowerShell      core     /sitecore/layout/Layouts/Applications/PowerShell Console
PowerShell      core     /sitecore/layout/Layouts/Applications/PowerShell ISE Sheer
PowerShell      core     /sitecore/layout/Layouts/Applications/PowerShell ISE
PowerShell      core     /sitecore/layout/Layouts/Applications/PowerShell ListView
PowerShell      core     /sitecore/content/Documents and Settings/All users/Start menu/Right/PowerShell Toolbox
PowerShell      core     /sitecore/content/Applications/PowerShell
PowerShell      core     /sitecore/content/Applications/Content Editor/Context Menues/Default/Context PowerShell Scripts
PowerShell      master   /sitecore/templates/Modules/PowerShell Console
PowerShell      master   /sitecore/system/Modules/PowerShell/Console Colors
PowerShell      master   /sitecore/system/Modules/PowerShell/Rules
PowerShell      master   /sitecore/system/Modules/PowerShell/Script Library
AssetsOptimiser master   /sitecore/templates/Cognifide/Optimiser
AssetsOptimiser master   /sitecore/system/Modules/Optimiser 
 
``` 
 
## Related Topics 
 
* Serialize-Item 
 
* Deserialize-Item 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>

