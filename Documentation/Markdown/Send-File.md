# Send-File 
 
Allows users to download files from server and file items from media library. 
 
## Syntax 
 
Send-File [-Path] &lt;String&gt; [-Message &lt;String&gt;] [-NoDialog] [-Title &lt;String&gt;] [-Width &lt;Int32&gt;] [-Height &lt;Int32&gt;] 
 
Send-File [-Message &lt;String&gt;] [-Item] &lt;Item&gt; [-NoDialog] [-Title &lt;String&gt;] [-Width &lt;Int32&gt;] [-Height &lt;Int32&gt;] 
 
 
## Detailed Description 
 
Executing this commandlet with file path on the server provides script users with means to download a file to their computer.
Executing it for an Item located in Sitecore Media library allows the user to download the blob stored in that item.
If the file has been downloaded the dialog returns "downloaded" string, otherwise "cancelled" is returned. 
 
- 
 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions## Aliases
The following abbreviations are aliases for this cmdlet:  
* Download-File 
 
## Parameters 
 
### -Path&nbsp; &lt;String&gt; 
 
Path to the file to be downloaded. The file has to exist in the Data folder. Files from outside the Data folder are not downloadable.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue, ByPropertyName) |
| Accept Wildcard Characters? | false | 
 
### -Message&nbsp; &lt;String&gt; 
 
Message to show the user in the download dialog.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Item&nbsp; &lt;Item&gt; 
 
The item to be downloaded.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue) |
| Accept Wildcard Characters? | false | 
 
### -NoDialog&nbsp; &lt;SwitchParameter&gt; 
 
If this parameter is used the Dialog will not be shown but instead the file download will begin immediately.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Title&nbsp; &lt;String&gt; 
 
Download dialog title.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Width&nbsp; &lt;Int32&gt; 
 
Download dialog width.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Height&nbsp; &lt;Int32&gt; 
 
Download dialog height.
 

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
 
Download File from server disk drive 
 
```powershell   
 
PS master:\> Send-File -Path "C:\Projects\ZenGarden\Data\packages\Sitecore PowerShell Extensions-2.6.zip" 
 
``` 
 
### EXAMPLE 2 
 
Download item from media library 
 
```powershell   
 
PS master:\> Get-Item "master:/media library/Showcase/cognifide_logo" | Send-File -Message "Cognifide Logo" 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>

