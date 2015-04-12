# Show-Application 
 
Executes Sitecore Sheer application. 
 
## Syntax 
 
Show-Application [-Application] &lt;String&gt; [[-Parameter] &lt;Hashtable&gt;] [-Icon &lt;String&gt;] [-Modal] [-Title &lt;String&gt;] [-Width &lt;Int32&gt;] [-Height &lt;Int32&gt;] 
 
 
## Detailed Description 
 
Executes Sitecore Sheer application, allows for passing additional parameters, launching it on desktop in cooperative or in Modal mode. 
 
- 
 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Application&nbsp; &lt;String&gt; 
 
Name of the Application to be executed. Application must be defined in the Core databse.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Parameter&nbsp; &lt;Hashtable&gt; 
 
Additional parameters passed to the application.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | 2 |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Icon&nbsp; &lt;String&gt; 
 
Icon of the executed application (used for titlebar and in the Sitecore taskbar on the desktop)
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Modal&nbsp; &lt;SwitchParameter&gt; 
 
Causes the application to show in new browser modal window or modal overlay if used in Sitecore 7.2 or later.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Title&nbsp; &lt;String&gt; 
 
Title of the window the app opens in.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Width&nbsp; &lt;Int32&gt; 
 
Width of the modal window.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Height&nbsp; &lt;Int32&gt; 
 
Height of the modal window.
 

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
 
Show Content Editor in new window (or as an overlay in modal mode in Sitecore 7.2+) with "/sitecore/templates" item selected. 
 
```powershell   
 
$item = gi master:\templates

Show-Application `
    -Application "Content Editor" `
    -Parameter @{id ="$($item.ID)"; fo="$($item.ID)";la="$($item.Language.Name)"; vs="$($item.Version.Number)";sc_content="$($item.Database.Name)"} `
    -Modal -Width 1600 -Height 800 
 
``` 
 
### EXAMPLE 2 
 
Show Content Editor as a new application on desktop with "/sitecore/content/home" item selected. 
 
```powershell   
 
$item = gi master:\content\home

Show-Application `
    -Application "Content Editor" `
    -Parameter @{id ="$($item.ID)"; fo="$($item.ID)";la="$($item.Language.Name)"; vs="$($item.Version.Number)";sc_content="$($item.Database.Name)"} ` 
 
``` 
 
## Related Topics 
 
* Read-Variable 
 
* Show-Alert 
 
* Show-Confirm 
 
* Show-FieldEditor 
 
* Show-Input 
 
* Show-ListView 
 
* Show-ModalDialog 
 
* Show-Result 
 
* Show-YesNoCancel 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>

