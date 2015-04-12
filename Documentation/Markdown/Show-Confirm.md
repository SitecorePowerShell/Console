# Show-Confirm 
 
Shows a user a confirmation request message box. 
 
## Syntax 
 
Show-Confirm [-Title] &lt;String&gt; 
 
 
## Detailed Description 
 
Shows a user a confirmation request message box.
Returns "yes" or "no" based on user's response.
The buttons that are shown to the user are "OK" and "Cancel". 
 
© 2010-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Title&nbsp; &lt;String&gt; 
 
Text to show the user in the dialog.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue) |
| Accept Wildcard Characters? | false | 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 
 
 
 
```powershell   
 
PS master:\> Show-Confirm -Title "Do you like Sitecore PowerShell Extensions?"

yes 
 
``` 
 
## Related Topics 
 
* Read-Variable 
 
* Show-Alert 
 
* Show-Application 
 
* Show-FieldEditor 
 
* Show-Input 
 
* Show-ListView 
 
* Show-ModalDialog 
 
* Show-Result 
 
* Show-YesNoCancel 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>

