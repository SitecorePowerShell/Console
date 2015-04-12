# Show-ModalDialog 
 
Shows Sitecore Sheer control as a modal dialog. 
 
## Syntax 
 
Show-ModalDialog -Control &lt;String&gt; [-Parameters &lt;Hashtable&gt;] [-Title &lt;String&gt;] [-Width &lt;Int32&gt;] [-Height &lt;Int32&gt;] 
 
Show-ModalDialog -Url &lt;String&gt; [-Title &lt;String&gt;] [-Width &lt;Int32&gt;] [-Height &lt;Int32&gt;] 
 
 
## Detailed Description 
 
Shows Sitecore Sheer control as a modal dialog. If control returns a value - the value will be passed back as the result of the commandlet execution. 
 
© 2010-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Control&nbsp; &lt;String&gt; 
 
Name of the Sitecore Sheer control to show
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Url&nbsp; &lt;String&gt; 
 
A fully formed URL that constitutes a control execution request.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Parameters&nbsp; &lt;Hashtable&gt; 
 
Hashtable of parameters to pass to the control in the url.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Title&nbsp; &lt;String&gt; 
 
Title of the control dialog.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Width&nbsp; &lt;Int32&gt; 
 
Width of the control dialog.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Height&nbsp; &lt;Int32&gt; 
 
Height of the control dialog.
 

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
 
PS master:\> Show-ModalDialog -Control "ConfirmChoice" -Parameters @{btn_0="Yes (returns btn_0)"; btn_1="No (returns btn_1)"; btn_2="return btn_2"; te="Message Text"; cp="My Caption"} -Height 120 -Width 400 
 
``` 
 
## Related Topics 
 
* Read-Variable 
 
* Show-Alert 
 
* Show-Application 
 
* Show-Confirm 
 
* Show-FieldEditor 
 
* Show-Input 
 
* Show-ListView 
 
* Show-Result 
 
* Show-YesNoCancel 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>

