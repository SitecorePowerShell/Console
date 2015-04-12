# Show-Result 
 
Shows a Sheer dialog with text results showing the output of the script or another control selected by the user based on either control name or Url to the control. 
 
## Syntax 
 
Show-Result -Control &lt;String&gt; [-Parameters &lt;String[]&gt;] [-Title &lt;String&gt;] [-Width &lt;Int32&gt;] [-Height &lt;Int32&gt;] 
 
Show-Result -Url &lt;String&gt; [-Title &lt;String&gt;] [-Width &lt;Int32&gt;] [-Height &lt;Int32&gt;] 
 
Show-Result [-Text] [-Title &lt;String&gt;] [-Width &lt;Int32&gt;] [-Height &lt;Int32&gt;] 
 
 
## Detailed Description 
 
Shows a Sheer dialog with text results showing the output of the script or another control selected by the user based on either control name or Url to the control. 
 
- 
 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Control&nbsp; &lt;String&gt; 
 
Name of the Sheer control to execute.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Url&nbsp; &lt;String&gt; 
 
Url to the Sheer control to execute.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Parameters&nbsp; &lt;String[]&gt; 
 
Parameters to be passed to the executed control when executing with the -Control parameter specified.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Text&nbsp; &lt;SwitchParameter&gt; 
 
Shows the default text dialog.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Title&nbsp; &lt;String&gt; 
 
Title of the window containing the control.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Width&nbsp; &lt;Int32&gt; 
 
Width of the window containing the control.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Height&nbsp; &lt;Int32&gt; 
 
Height of the window containing the control.
 

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
 
Show results of script executio 
 
```powershell   
 
PS master:\> Show-Result -Text 
 
``` 
 
### EXAMPLE 2 
 
Show the Control Panel control in a Window of specified size. 
 
```powershell   
 
PS master:\> Show-Result -Control "ControlPanel" -Width 1024 -Height 640 
 
``` 
 
### EXAMPLE 3 
 
 
 
```powershell   
 
Shows a new instance of ISE
Show-Result -Url "/sitecore/shell/Applications/PowerShell/PowerShellIse" 
 
``` 
 
### EXAMPLE 4 
 
 
 
```powershell   
 
 
 
``` 
 
## Related Topics 
 
* Read-Variable 
 
* Show-Alert 
 
* Show-Application 
 
* Show-Confirm 
 
* Show-FieldEditor 
 
* Show-Input 
 
* Show-ListView 
 
* Show-ModalDialog 
 
* Show-YesNoCancel 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>

