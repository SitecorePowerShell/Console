# Read-Variable 
 
Prompts user to provide values for variables required by the script to perform its operation. 
 
## Syntax 
 
Read-Variable [-Parameters &lt;Object[]&gt;] [-Description &lt;String&gt;] [-CancelButtonName &lt;String&gt;] [-OkButtonName &lt;String&gt;] [-ShowHints] [-Title &lt;String&gt;] [-Width &lt;Int32&gt;] [-Height &lt;Int32&gt;] 
 
 
## Detailed Description 
 
Prompts user to provide values for variables required by the script to perform its operation.
If user selects the "OK" button the command will return 'ok' as its value.
If user selects the "Cancel" button or closes the window with the "x" button at the top-right corner of the dialog the command will return 'cancel' as its value. 
 
© 2010-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Parameters&nbsp; &lt;Object[]&gt; 
 
Specifies the variables that value should be provided by the user. Each variable definition can have the following structure:
- Name - the name of the PowerShell variable - without the $ sign
- Value - the initial value of the variable - if the variable have not been created prior to launching the dialog - this will be its value unless the user changes it. if Value is not specified - the existing variable name will be used.
- Title - The title for the variable shown above the variable editor.
- Tooltip - The hint describing the parameter further - if the -ShowHints parameter is provided this value will show between the Variable Title and the variable editor.
- Editor - If the default editor selected does not provide the functionality expected - you can specify this value to customize it (see examples)
- Tab - if this parameter is specified on any Variable the multi-tab dialog will be used instead of a simple one. Provide the tab name on which the variable editor should appear.

Variable type specific:
- Root - for some Item selecting editors you can provide this to limit the selection to only part of the tree
- Source - for some Item selecting editors you can provide this to parametrize the item selection editor. (Refer to examples for some sample usages)
- Lines - for String variable you can select this parameter if you want to present the user with the multiline editor. The for this parameter is the number of lines that the editor will be configured with.
- Domain - for user and role selectors you can limit the users &amp; roles presented to only the domain - specified) 
 
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
 
### -ShowHints&nbsp; &lt;SwitchParameter&gt; 
 
Specifies whether the variable hints should be displayed. Hints are shown below each the variable title but above the variable editing control. 
 
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
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* System.String 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 1 
 
For a good understanding of all the Property types the Read-Variable command accepts open the script located in the following item:
/Examples/User Interaction/Read-Variable - Sample
the script is located in the Script Library in the Master database. 
 
```powershell   
 
 
 
``` 
 
### EXAMPLE 2 
 
Following is an example of a simple dialog asking user for various variable types.

The type of some of the controls displayed to the user are iferred from the variable type (like the $item variable or DateTime)
The editors for some other are set by providing the "editor" value 
 
```powershell   
 
$item = Get-Item master:\content\home
$result = Read-Variable -Parameters `
    @{ Name = "someText"; Value="Some Text"; Title="Single Line Text"; Tooltip="Tooltip for singleline"}, 
    @{ Name = "multiText"; Value="Multiline Text"; Title="Multi Line Text"; lines=3; Tooltip="Tooltip for multiline"}, 
    @{ Name = "from"; Value=[System.DateTime]::Now.AddDays(-5); Title="Start Date"; Tooltip="Date since when you want the report to run"; Editor="date time"}, 
    @{ Name = "user"; Value=$me; Title="Select User"; Tooltip="Tooltip for user"; Editor="user multiple"},
    @{ Name = "item"; Title="Start Item"; Root="/sitecore/content/"} `
    -Description "This Dialog shows less editors, it doesn't need tabs as there is less of the edited variables" `
    -Title "Initialise various variable types (without tabs)" -Width 500 -Height 480 -OkButtonName "Proceed" -CancelButtonName "Abort" 
 
``` 
 
### EXAMPLE 3 
 
Following is an example of a multi tabbed dialog asking user for various variable types.

The type of some of the controls displayed to the user are iferred from the variable type (like the $item variable or DateTime)
The editors for some other are set by providing the "editor" value 
 
```powershell   
 
$item = Get-Item master:\content\home
$result = Read-Variable -Parameters `
    @{ Name = "silent"; Value=$true; Title="Proceed Silently"; Tooltip="Check this if you don't want to be interrupted"; Tab="Simple"}, 
    @{ Name = "someText"; Value="Some Text"; Title="Text"; Tooltip="Just a single line of Text"; Tab="Simple"}, 
    @{ Name = "multiText"; Value="Multiline Text"; Title="Longer Text"; lines=3; Tooltip="You can put multi line text here"; Tab="Simple"}, 
    @{ Name = "number"; Value=110; Title="Integer"; Tooltip="I need this number too"; Tab="Simple"}, 
    @{ Name = "fraction"; Value=1.1; Title="Float"; Tooltip="I'm just a bit over 1"; Tab="Simple"}, 
    @{ Name = "from"; Value=[System.DateTime]::Now.AddDays(-5); Title="Start Date"; Tooltip="Date since when you want the report to run"; Editor="date time"; Tab="Time"}, 
    @{ Name = "fromDate"; Value=[System.DateTime]::Now.AddDays(-5); Title="Start Date"; Tooltip="Date since when you want the report to run"; Editor="date"; Tab="Time"}, 
    @{ Name = "item"; Title="Start Item"; Root="/sitecore/content/"; Tab="Items"}, 
    @{ Name = "items"; Title="Bunch of Templates"; 
        Source="DataSource=/sitecore/templates&DatabaseName=master&IncludeTemplatesForDisplay=Node,Folder,Template,Template Folder&IncludeTemplatesForSelection=Template"; 
        editor="treelist"; Tab="Items"}, 
    @{ Name = "items2"; Title="Bunch of Templates"; 
        Source="DataSource=/sitecore/templates&DatabaseName=master&IncludeTemplatesForDisplay=Node,Folder,Template,Template Folder&IncludeTemplatesForSelection=Template"; 
        editor="multilist"; Tab="More Items"}, 
    @{ Name = "items3"; Title="Pick One Template"; 
        Source="DataSource=/sitecore/templates&DatabaseName=master&IncludeTemplatesForDisplay=Node,Folder,Template,Template Folder&IncludeTemplatesForSelection=Template"; 
        editor="droplist"; Tab="More Items"}, 
    @{ Name = "user"; Value=$me; Title="Select User"; Tooltip="Tooltip for user"; Editor="user multiple"; Tab="Rights"}, 
    @{ Name = "role"; Title="Select Role"; Tooltip="Tooltip for role"; Editor="role multiple"; Domain="sitecore"; Tab="Rights"}, `
    @{ Name = "userOrRole"; Title="Select User or Role"; Tooltip="Tooltip for role"; Editor="user role multiple"; Domain="sitecore"; Tab="Rights" } `
    -Description "This Dialog shows all available editors in some configurations, the properties are groupped into tabs" `
    -Title "Initialise various variable types (with tabs)" -Width 600 -Height 640 -OkButtonName "Proceed" -CancelButtonName "Abort" -ShowHints
if($result -ne "ok")
{
    Exit
} 
 
``` 
 
## Related Topics 
 
* Show-Alert* Show-Application* Show-Confirm* Show-FieldEditor* Show-Input* Show-ListView* Show-ModalDialog* Show-YesNoCancel* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>
