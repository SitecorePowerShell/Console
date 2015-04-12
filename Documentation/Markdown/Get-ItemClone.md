# Get-ItemClone 
 
Returns clones of item provided. 
 
## Syntax 
 
Get-ItemClone [-Item] &lt;Item&gt; 
 
Get-ItemClone [-Path] &lt;String&gt; 
 
Get-ItemClone -Id &lt;String&gt; [-Database &lt;Database&gt;] 
 
 
## Detailed Description 
 
Gets all clones of item provided. 
 
- 
 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Item&nbsp; &lt;Item&gt; 
 
The item to be analysed for clones presence.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue, ByPropertyName) |
| Accept Wildcard Characters? | false | 
 
### -Path&nbsp; &lt;String&gt; 
 
Path to the item to be analysed for clones presence.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Id&nbsp; &lt;String&gt; 
 
Id of the item to be analysed for clones presence.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Database&nbsp; &lt;Database&gt; 
 
Database containing the item to be processed - if item is being provided through Id.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
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
 
### EXAMPLE 
 
 
 
```powershell   
 
PS master:\> Get-ItemClone -Path master:\content\home 
 
``` 
 
## Related Topics 
 
* New-ItemClone 
 
* ConvertFrom-ItemClone 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/> 
 
* <a href='https://github.com/SitecorePowerShell/Console/issues/218' target='_blank'>https://github.com/SitecorePowerShell/Console/issues/218</a><br/> 
 
* Get-Item

