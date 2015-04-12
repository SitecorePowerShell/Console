# Get-LayoutDevice 
 
Returns Sitecore Layout device. 
 
## Syntax 
 
Get-LayoutDevice [-Name] &lt;String&gt; 
 
Get-LayoutDevice [-Default] 
 
 
## Detailed Description 
 
Returns Sitecore Layout device associated with a name or a default instance layout device/ 
 
- 
 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Name&nbsp; &lt;String&gt; 
 
Name of the device to return.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Default&nbsp; &lt;SwitchParameter&gt; 
 
Determines that a default system layout device should be returned.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
## Inputs 
 
The input type is the type of the objects that you can pipe to the cmdlet. 
 
* Sitecore.Data.Items.Item 
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* Sitecore.Data.Items.Item 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 1 
 
Get Print device 
 
```powershell   
 
PS master:\> Get-LayoutDevice "Print" 
 
``` 
 
### EXAMPLE 2 
 
Get default device 
 
```powershell   
 
PS master:\> Get-LayoutDevice -Default 
 
``` 
 
### EXAMPLE 3 
 
Get all layout devices 
 
```powershell   
 
PS master:\> Get-LayoutDevice * 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/> 
 
* Add-Rendering 
 
* New-Rendering 
 
* Set-Rendering 
 
* Get-Rendering 
 
* Remove-Rendering 
 
* Get-Layout 
 
* Set-Layout

