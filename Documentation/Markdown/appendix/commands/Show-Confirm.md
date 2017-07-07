# Show-Confirm 
 
Shows a user a confirmation request message box. 
 
## Syntax 
 
Show-Confirm [-Title] &lt;String&gt; 
 
 
## Detailed Description 
 
Shows a user a confirmation request message box.
Returns "yes" or "no" based on user's response.
The buttons that are shown to the user are "OK" and "Cancel". 
 
© 2010-2017 Adam Najmanowicz, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
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
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* System.String 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 
 
 
 
```powershell   
 
PS master:\> Show-Confirm -Title "Do you like Sitecore PowerShell Extensions?"

yes 
 
``` 
 
## Related Topics 
 
* [Read-Variable](/appendix/commands/Read-Variable.md)* [Show-Alert](/appendix/commands/Show-Alert.md)* [Show-Application](/appendix/commands/Show-Application.md)* [Show-FieldEditor](/appendix/commands/Show-FieldEditor.md)* [Show-Input](/appendix/commands/Show-Input.md)* [Show-ListView](/appendix/commands/Show-ListView.md)* [Show-ModalDialog](/appendix/commands/Show-ModalDialog.md)* [Show-Result](/appendix/commands/Show-Result.md)* [Show-YesNoCancel](/appendix/commands/Show-YesNoCancel.md)* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>
