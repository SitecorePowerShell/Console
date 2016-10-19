# Receive-File 
 
Shows a dialog to users allowing to upload files to either server file system or items in media library. 
 
## Syntax 
 
Receive-File [-Description &lt;String&gt;] [-ParentItem] &lt;Item&gt; [-Title &lt;String&gt;] [-CancelButtonName &lt;String&gt;] [-OkButtonName &lt;String&gt;] [-Versioned] [-Language &lt;String&gt;] [-Overwrite] [-Unpack] [-Width &lt;Int32&gt;] [-Height &lt;Int32&gt;] 
 
Receive-File [-Description &lt;String&gt;] [-Path] &lt;String&gt; [-Title &lt;String&gt;] [-CancelButtonName &lt;String&gt;] [-OkButtonName &lt;String&gt;] [-Overwrite] [-Unpack] [-Width &lt;Int32&gt;] [-Height &lt;Int32&gt;] 
 
Receive-File [-ParentItem] &lt;Item&gt; -AdvancedDialog [-Width &lt;Int32&gt;] [-Height &lt;Int32&gt;] 
 
 
## Detailed Description 
 
Executing this command with file path on the server (provided as -Path parameter) provides script users with means to upload a file from their computer.
Executing it for an Item located in Sitecore Media library (provided as -ParentItem) allows the user to upload the file as a child to that item.
If the file has been uploaded the dialog returns path to the file (in case of file system storage) or Item that has been created if the file was uplaoded to media library. 
 
© 2010-2016 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Description&nbsp; &lt;String&gt; 
 
Dialog description displayed below the dialog title. 
 
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
 
### -ParentItem&nbsp; &lt;Item&gt; 
 
The item under which the uploaded media items should be stored. 
 
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
 
### -Path&nbsp; &lt;String&gt; 
 
Path to the folder where uploaded file should be stored. 
 
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
            <td>true (ByValue, ByPropertyName)</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
### -Title&nbsp; &lt;String&gt; 
 
Dialog title - shown at the top of the dialog. 
 
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
 
### -CancelButtonName&nbsp; &lt;String&gt; 
 
Text shown on the cancel button. 
 
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
 
### -OkButtonName&nbsp; &lt;String&gt; 
 
Text shown on the OK button. 
 
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
 
### -Versioned&nbsp; &lt;SwitchParameter&gt; 
 
Indicates that the Media item should be created as a Versioned media item. 
 
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
 
### -Language&nbsp; &lt;String&gt; 
 
Specifies the language in which the media item should be created. if not specified - context language is selected. 
 
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
 
### -Overwrite&nbsp; &lt;SwitchParameter&gt; 
 
indicates that the upload should overwrite a file or a media item if that one exists. Otherwise a file with a non-confilicting name or a sibling media item is created. 
 
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
 
### -Unpack&nbsp; &lt;SwitchParameter&gt; 
 
Indicates that the uplaod is expected to be a ZIP file which should be unpacked when it's received. 
 
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
 
### -AdvancedDialog&nbsp; &lt;SwitchParameter&gt; 
 
Shows advanced dialog where user can upload multiple media items and select if the uploaded items are versioned, overwritten and unpacked. 
 
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
 
Dialog width. 
 
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
 
Dialog width. 
 
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
 
## Inputs 
 
The input type is the type of the objects that you can pipe to the cmdlet. 
 
* Sitecore.Data.Items.Item
System.String 
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* Sitecore.Data.Items.Item
System.String 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 1 
 
Upload text.txt file to server disk drive.
A new file is created with a non-conflicting name and the path to it is returned 
 
```powershell   
 
PS master:\> Receive-File -Folder "C:\temp\upload"
C:\temp\upload\text_029.txt 
 
``` 
 
### EXAMPLE 2 
 
Upload text.txt file to media library under the 'master:\media library\Files' item
A new media item is created and returned 
 
```powershell   
 
PS master:\> Receive-File -ParentItem (get-item "master:\media library\Files") 
Name Children Languages Id                                     TemplateName
---- -------- --------- --                                     ------------
text False    {en}      {7B11CE12-C0FC-4650-916C-2FC76F3DCAAF} File 
 
``` 
 
### EXAMPLE 3 
 
Upload text.txt file to media library under the 'master:\media library\Files' item using advanced dialog.
A new media item is created but "undetermined" is returned as the dialog does not return the results. 
 
```powershell   
 
PS master:\> Receive-File (get-item "master:\media library\Files") -AdvancedDialog
undetermined 
 
``` 
 
### EXAMPLE 4 
 
Upload text.txt file to media library under the 'master:\media library\Files' item.
A new versioned media item in Danish language is created and returned. If the media item existed - it will be overwritten. 
 
```powershell   
 
PS master:\> Receive-File -ParentItem (get-item "master:\media library\Files") -Overwrite -Language "da" -Versioned
Name Children Languages Id                                     TemplateName
---- -------- --------- --                                     ------------
text False    {en, da}  {307BCF7D-27FD-46FC-BE83-D9ED640CB09F} File 
 
``` 
 
## Related Topics 
 
* [Send-File](/appendix/commands/Send-File.md)* [Out-Download](/appendix/commands/Out-Download.md)* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>
