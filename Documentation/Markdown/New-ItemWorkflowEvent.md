# New-ItemWorkflowEvent 
 
Creates new entry in the history store notifying of workflow state change. 
 
## Syntax 
 
New-ItemWorkflowEvent [-Language &lt;String[]&gt;] -Id &lt;String&gt; [-Database &lt;Database&gt;] [-OldState &lt;String&gt;] [-NewState &lt;String&gt;] [-Text &lt;String&gt;] 
 
New-ItemWorkflowEvent [-Language &lt;String[]&gt;] [-Path] &lt;String&gt; [-OldState &lt;String&gt;] [-NewState &lt;String&gt;] [-Text &lt;String&gt;] 
 
New-ItemWorkflowEvent [-Language &lt;String[]&gt;] [-Item] &lt;Item&gt; [-OldState &lt;String&gt;] [-NewState &lt;String&gt;] [-Text &lt;String&gt;] 
 
 
## Detailed Description 
 
Creates new entry in the history store notifying of workflow state change. 
 
- 
 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -OldState&nbsp; &lt;String&gt; 
 
Id of the old state. If not provided - current item workflow state will be used.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -NewState&nbsp; &lt;String&gt; 
 
Id of the old state. If not provided - current item workflow state will be used.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Text&nbsp; &lt;String&gt; 
 
Action comment.
 

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
 
The item to have the history event attached.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue, ByPropertyName) |
| Accept Wildcard Characters? | false | 
 
### -Path&nbsp; &lt;String&gt; 
 
Path to the item to have the history event attached - can work with Language parameter to narrow the publication scope.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Id&nbsp; &lt;String&gt; 
 
Id of the item to have the history event attached - can work with Language parameter to narrow the publication scope.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Database&nbsp; &lt;Database&gt; 
 
Database containing the item to have the history event attached - can work with Language parameter to narrow the publication scope.
 

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
 
PS master:\> New-ItemWorkflowEvent -Path master:\content\home -lanuage "en" -Text "Just leaving a note" 
 
``` 
 
## Related Topics 
 
* Get-ItemWorkflowEvent 
 
* Execute-Workflow 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>

