# Invoke-Workflow 
 
Executes Workflow action for an item.
This command used to be named Execute-Workflow - a matching alias added for compatibility with older scripts. 
 
## Syntax 
 
Invoke-Workflow [-Language &lt;String[]&gt;] -Id &lt;String&gt; [-Database &lt;Database&gt;] [-CommandName &lt;String&gt;] [-Comments &lt;String&gt;] 
 
Invoke-Workflow [-Language &lt;String[]&gt;] [-Path] &lt;String&gt; [-CommandName &lt;String&gt;] [-Comments &lt;String&gt;] 
 
Invoke-Workflow [-Language &lt;String[]&gt;] [-Item] &lt;Item&gt; [-CommandName &lt;String&gt;] [-Comments &lt;String&gt;] 
 
 
## Detailed Description 
Executes Workflow action for an item. If the workflow action could not be performed for any reason - an appropriate error will be raised. 
- 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions## Aliases
The following abbreviations are aliases for this cmdlet:  
* Execute-Workflow 
 
## Parameters 
 
### -CommandName&nbsp; &lt;String&gt; 
 
Namer of the workflow command.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Comments&nbsp; &lt;String&gt; 
 
Comment to be saved in the history table for the action.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Language&nbsp; &lt;String[]&gt; 
 
If you need the item in specific Language You can specify it with this parameter. Globbing/wildcard supported.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Item&nbsp; &lt;Item&gt; 
 
The item to have the workflow action executed.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue, ByPropertyName) |
| Accept Wildcard Characters? | false | 
 
### -Path&nbsp; &lt;String&gt; 
 
Path to the item to have the workflow action executed - can work with Language parameter to narrow the publication scope.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Id&nbsp; &lt;String&gt; 
 
Id of the item to have the workflow action executed - can work with Language parameter to narrow the publication scope.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Database&nbsp; &lt;Database&gt; 
 
Database containing the item to have the workflow action executed - can work with Language parameter to narrow the publication scope.
 

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
 
Submit item to approval, item gotten from path 
 
```powershell   
 
PS master:\> Invoke-Workflow -Path master:/content/home -CommandName "Submit" -Comments "Automated" 
 
``` 
 
### EXAMPLE 2 
 
Reject item, item gotten from pipeline 
 
```powershell   
 
PS master:\> Get-Item master:/content/home | Invoke-Workflow -CommandName "Reject" -Comments "Automated" 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>

