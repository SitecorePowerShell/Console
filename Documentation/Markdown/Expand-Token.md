# Expand-Token 
 
Expands tokens on items. 
 
## Syntax 
 
Expand-Token [-Language &lt;String[]&gt;] -Id &lt;String&gt; [-Database &lt;Database&gt;] 
 
Expand-Token [-Language &lt;String[]&gt;] [-Path] &lt;String&gt; 
 
Expand-Token [-Language &lt;String[]&gt;] [-Item] &lt;Item&gt; 
 
 
## Detailed Description 
 
Expands tokens like $name in fields on items. 
 
- 
 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Language&nbsp; &lt;String[]&gt; 
 

 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Item&nbsp; &lt;Item&gt; 
 
The item to be processed.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue, ByPropertyName) |
| Accept Wildcard Characters? | false | 
 
### -Path&nbsp; &lt;String&gt; 
 

 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Id&nbsp; &lt;String&gt; 
 

 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Database&nbsp; &lt;Database&gt; 
 

 

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
 
PS master:\> Get-Item master:\content\home | Expand-Token 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/> 
 
* <a href='http://sitecorejunkie.com/2014/05/27/launch-powershell-scripts-in-the-item-context-menu-using-sitecore-powershell-extensions/' target='_blank'>http://sitecorejunkie.com/2014/05/27/launch-powershell-scripts-in-the-item-context-menu-using-sitecore-powershell-extensions/</a><br/> 
 
* <a href='http://sitecorejunkie.com/2014/06/02/make-bulk-item-updates-using-sitecore-powershell-extensions/' target='_blank'>http://sitecorejunkie.com/2014/06/02/make-bulk-item-updates-using-sitecore-powershell-extensions/</a><br/>

