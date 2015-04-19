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
 
<table>
    <thead></thead>
    <tbody>
        <tr>
            <td>Aliases</td>
            <td></td>
        </tr>
        <tr>
            <td>Required?</td>
            <td>true</td>
        </tr>
        <tr>
            <td>Position?</td>
            <td>1</td>
        </tr>
        <tr>
            <td>Default Value</td>
            <td></td>
        </tr>
        <tr>
            <td>Accept Pipeline Input?</td>
            <td>true (ByValue)</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
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


