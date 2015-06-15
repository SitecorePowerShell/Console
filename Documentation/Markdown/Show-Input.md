# Show-Input 
 
Shows prompt message box asking user to provide a text string. 
 
## Syntax 
 
Show-Input [-Prompt] &lt;String&gt; [-DefaultValue &lt;String&gt;] [-Validation &lt;String&gt;] [-ErrorMessage &lt;String&gt;] [-MaxLength &lt;Int32&gt;] 
 
 
## Detailed Description 
 
Shows prompt message box asking user to provide a text string. 
 
© 2010-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Prompt&nbsp; &lt;String&gt; 
 
Prompt message to show in the message box shown to a user. 
 
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
            <td>false</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
### -DefaultValue&nbsp; &lt;String&gt; 
 
Default value to be provided for the text box. 
 
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
 
### -Validation&nbsp; &lt;String&gt; 
 
Regex for value validation. If user enters a value that does not validate - en error message defined with the "ErrorMessage" parameter will be shown and user will be asked to enter the value again. 
 
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
 
### -ErrorMessage&nbsp; &lt;String&gt; 
 
Error message to show when regex validation fails. 
 
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
 
### -MaxLength&nbsp; &lt;Int32&gt; 
 
Maximum length of the string returned. If user enters a longer value - en error message will be shown and user will be asked to enter the value again. 
 
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
 
### EXAMPLE 1 
 
Requests that the user provides an email, validates it against a regular expression snd whows an allert if the format is not valid 
 
```powershell   
 
PS master:\> Show-Input "Please provide your email" -DefaultValue "my@email.com"  -Validation "^[a-zA-Z0-9_-]+(?:\.[a-zA-Z0-9_-]+)*@(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?\.)+[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?$" -ErrorMessage "Not a proper email!" 
 
``` 
 
### EXAMPLE 2 
 
Uses Show-Input command to request user a new name for the content item validating the proper characters are used and assigns the result to $newName variable (nothing gets changed) 
 
```powershell   
 
PS master:\> $contentItem = get-item master:\content
PS master:\> $newName = Show-Input "Please provide the new name for the '$($contentItem.Name)' Item" -DefaultValue $contentItem.Name  -Validation "^[\w\*\$][\w\s\-\$]*(\(\d{1,}\)){0,1}$" -ErrorMessage "Invalid characters in the name"

#print new name
PS master:\> Write-Host "The new name you've chosen is '$($newName)'" 
 
``` 
 
### EXAMPLE 3 
 
Requests that the user provides a string of at most  5 characters 
 
```powershell   
 
Show-Input "Please provide 5 characters at most" -MaxLength 5 
 
``` 
 
## Related Topics 
 
* Read-Variable* Show-Alert* Show-Application* Show-Confirm* Show-FieldEditor* Show-ListView* Show-ModalDialog* Show-Result* Show-YesNoCancel* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>
