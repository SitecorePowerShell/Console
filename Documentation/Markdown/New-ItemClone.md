# New-ItemClone 
 
Creates a new item clone based on the item provided. 
 
## Syntax 
 
New-ItemClone [-Item] &lt;Item&gt; -Destination &lt;Item&gt; [-Name &lt;String&gt;] [-Recursive] 
 
New-ItemClone [-Path] &lt;String&gt; -Destination &lt;Item&gt; [-Name &lt;String&gt;] [-Recursive] 
 
New-ItemClone -Id &lt;String&gt; [-Database &lt;Database&gt;] -Destination &lt;Item&gt; [-Name &lt;String&gt;] [-Recursive] 
 
 
## Detailed Description 
 
Creates a new item clone based on the item provided. 
 
© 2010-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Destination&nbsp; &lt;Item&gt; 
 
Parent item under which the clone should be created.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Name&nbsp; &lt;String&gt; 
 
Name of the item clone.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Recursive&nbsp; &lt;SwitchParameter&gt; 
 
Add the parameter to clone thw whole branch rather than a single item.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Item&nbsp; &lt;Item&gt; 
 
The item to be cloned.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue, ByPropertyName) |
| Accept Wildcard Characters? | false | 
 
### -Path&nbsp; &lt;String&gt; 
 
Path to the item to be cloned.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Id&nbsp; &lt;String&gt; 
 
Id of the item to be cloned
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Database&nbsp; &lt;Database&gt; 
 
Database of the item to be cloned if item is specified through its ID.
 

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
 
Clone /sitecore/content/home/ under /sitecore/content/new-target/ with the "New Home" name. 
 
```powershell   
 
PS master:\> $newTarget = Get-Item master:\content\new-target\
PS master:\> New-ItemClone -Path master:\content\home -Destination $newTarget -Name "New Home" 
 
``` 
 
## Related Topics 
 
* Get-ItemClone 
 
* ConvertFrom-ItemClone 
 
* New-Item 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/> 
 
* <a href='https://github.com/SitecorePowerShell/Console/issues/218' target='_blank'>https://github.com/SitecorePowerShell/Console/issues/218</a><br/>

