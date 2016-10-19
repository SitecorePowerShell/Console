# Show-Alert 
 
Pauses the script and shows an alert to the user. 
 
## Syntax 
 
Show-Alert [-Title] &lt;String&gt; 
 
 
## Detailed Description 
 
Pauses the script and shows an alert specified in the -Title to the user. Once user clicks the OK button - script execution resumes. 
 
© 2010-2016 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Title&nbsp; &lt;String&gt; 
 
Text to show the user in the alert dialog. 
 
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
 
PS master:\> Show-Alert "Hello world." 
 
``` 
 
## Related Topics 
 
* [Read-Variable](/appendix/commands/Read-Variable.md)* [Show-Application](/appendix/commands/Show-Application.md)* [Show-Confirm](/appendix/commands/Show-Confirm.md)* [Show-FieldEditor](/appendix/commands/Show-FieldEditor.md)* [Show-Input](/appendix/commands/Show-Input.md)* [Show-ListView](/appendix/commands/Show-ListView.md)* [Show-ModalDialog](/appendix/commands/Show-ModalDialog.md)* [Show-Result](/appendix/commands/Show-Result.md)* [Show-YesNoCancel](/appendix/commands/Show-YesNoCancel.md)* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>
