# Show-YesNoCancel 
 
Shows Yes/No/Cancel dialog to the user and returns user choice. 
 
## Syntax 
 
Show-YesNoCancel [-Title &lt;String&gt;] [-Width &lt;Int32&gt;] [-Height &lt;Int32&gt;] 
 
 
## Detailed Description 
 
Shows Yes/No/Cancel dialog to the user and returns user choice as a string value.

Depending on the user response one of the 2 strings is returned:
- yes
- no
- cancel 
 
© 2010-2017 Adam Najmanowicz, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Title&nbsp; &lt;String&gt; 
 
Question to ask the user in the dialog 
 
<table>
    <thead></thead>
    <tbody>
        <tr>
            <td>Aliases</td>
            <td></td>
        </tr>
        <tr>
            <td>Required?</td>
            <td>false</td>
        </tr>
        <tr>
            <td>Position?</td>
            <td>named</td>
        </tr>
        <tr>
            <td>Default Value</td>
            <td></td>
        </tr>
        <tr>
            <td>Accept Pipeline Input?</td>
            <td>false</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
### -Width&nbsp; &lt;Int32&gt; 
 
Width of the dialog. 
 
<table>
    <thead></thead>
    <tbody>
        <tr>
            <td>Aliases</td>
            <td></td>
        </tr>
        <tr>
            <td>Required?</td>
            <td>false</td>
        </tr>
        <tr>
            <td>Position?</td>
            <td>named</td>
        </tr>
        <tr>
            <td>Default Value</td>
            <td></td>
        </tr>
        <tr>
            <td>Accept Pipeline Input?</td>
            <td>false</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
### -Height&nbsp; &lt;Int32&gt; 
 
Height of the dialog. 
 
<table>
    <thead></thead>
    <tbody>
        <tr>
            <td>Aliases</td>
            <td></td>
        </tr>
        <tr>
            <td>Required?</td>
            <td>false</td>
        </tr>
        <tr>
            <td>Position?</td>
            <td>named</td>
        </tr>
        <tr>
            <td>Default Value</td>
            <td></td>
        </tr>
        <tr>
            <td>Accept Pipeline Input?</td>
            <td>false</td>
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
 
PS master:\> Show-YesNoCancel "Should we delete those 2 items?"

cancel 
 
``` 
 
## Related Topics 
 
* [Read-Variable](/appendix/commands/Read-Variable.md)* [Show-Alert](/appendix/commands/Show-Alert.md)* [Show-Application](/appendix/commands/Show-Application.md)* [Show-Confirm](/appendix/commands/Show-Confirm.md)* [Show-FieldEditor](/appendix/commands/Show-FieldEditor.md)* [Show-Input](/appendix/commands/Show-Input.md)* [Show-ListView](/appendix/commands/Show-ListView.md)* [Show-ModalDialog](/appendix/commands/Show-ModalDialog.md)* [Show-Result](/appendix/commands/Show-Result.md)* [Show-YesNoCancel](/appendix/commands/Show-YesNoCancel.md)* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>
