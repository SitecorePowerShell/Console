# Invoke-ShellCommand 
 
Executes Sitecore Shell command for an item.
This command used to be named Execute-ShellCommand - a matching alias added for compatibility with older scripts. 
 
## Syntax 
 
Invoke-ShellCommand [-Language &lt;String[]&gt;] -Id &lt;String&gt; [-Database &lt;Database&gt;] [-Name] &lt;String&gt; 
 
Invoke-ShellCommand [-Language &lt;String[]&gt;] [-Path] &lt;String&gt; [-Name] &lt;String&gt; 
 
Invoke-ShellCommand [-Language &lt;String[]&gt;] [-Item] &lt;Item&gt; [-Name] &lt;String&gt; 
 
 
## Detailed Description 
 
Executes Sitecore Shell command for an item. e.g. opening dialogs or performing commands that you can find in the Content Editor ribbon or context menu. 
 
- 
 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions## Aliases
The following abbreviations are aliases for this cmdlet:  
* Execute-ShellCommand 
 
## Parameters 
 
### -Name&nbsp; &lt;String&gt; 
 
Name of the sitecore command e.g. "item:publishingviewer"
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Language&nbsp; &lt;String[]&gt; 
 
If specified - language that will be used as source language.
 

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
 
Path to the item to be processed - can work with Language parameter to narrow the publication scope.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Id&nbsp; &lt;String&gt; 
 
Id of the item to be processed - can work with Language parameter to narrow the publication scope.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Database&nbsp; &lt;Database&gt; 
 
Database containing the item to be processed - can work with Language parameter to narrow the publication scope.
 

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
 
### EXAMPLE 1 
 
Launch Publishing Viewer for /sitecore/content/home item. 
 
```powershell   
 
PS master:\> Get-Item master:\content\home\ | Invoke-ShellCommand "item:publishingviewer" 
 
``` 
 
### EXAMPLE 2 
 
Initiate /sitecore/content/home item duplication. 
 
```powershell   
 
PS master:\> Get-Item master:/content/home | Invoke-ShellCommand "item:duplicate" 
 
``` 
 
### EXAMPLE 3 
 
Show properties of the /sitecore/content/home item. 
 
```powershell   
 
PS master:\> Get-Item master:/content/home | Invoke-ShellCommand "contenteditor:properties" 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>

