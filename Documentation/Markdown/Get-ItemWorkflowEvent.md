# Get-ItemWorkflowEvent 
 
Retrieves entries from the history store notifying of workflow state change of a specific item. 
 
## Syntax 
 
Get-ItemWorkflowEvent [-Language &lt;String[]&gt;] -Id &lt;String&gt; [-Database &lt;Database&gt;] [-Identity &lt;String&gt;] 
 
Get-ItemWorkflowEvent [-Language &lt;String[]&gt;] [-Path] &lt;String&gt; [-Identity &lt;String&gt;] 
 
Get-ItemWorkflowEvent [-Language &lt;String[]&gt;] [-Item] &lt;Item&gt; [-Identity &lt;String&gt;] 
 
 
## Detailed Description 
 
Retrieves entries from the history store notifying of workflow state change of a specific item. 
 
© 2010-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Identity&nbsp; &lt;String&gt; 
 
User that has been associated with the enteries. Wildcards are supported.
 

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
 
The item to have its history items returned.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue, ByPropertyName) |
| Accept Wildcard Characters? | false | 
 
### -Path&nbsp; &lt;String&gt; 
 
Path to the item to have its history items returned - can work with Language parameter to narrow the publication scope.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Id&nbsp; &lt;String&gt; 
 
Id of the item have its history items returned - can work with Language parameter to narrow the publication scope.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Database&nbsp; &lt;Database&gt; 
 
Database containing the item to have its history items returned - can work with Language parameter to narrow the publication scope.
 

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
 
PS master:\> Get-ItemWorkflowEvent -Path master:\content\home
Date     : 2014-07-27 14:23:33
NewState : {190B1C84-F1BE-47ED-AA41-F42193D9C8FC}
OldState : {46DA5376-10DC-4B66-B464-AFDAA29DE84F}
Text     : Automated
User     : sitecore\admin

Date     : 2014-08-01 15:43:29
NewState : {190B1C84-F1BE-47ED-AA41-F42193D9C8FC}
OldState : {190B1C84-F1BE-47ED-AA41-F42193D9C8FC}
Text     : Just leaving a note
User     : sitecore\admin 
 
``` 
 
## Related Topics 
 
* New-ItemWorkflowEvent 
 
* Execute-Workflow 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>

