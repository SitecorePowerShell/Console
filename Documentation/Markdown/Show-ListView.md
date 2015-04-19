# Show-ListView 
 
Sends output to an interactive table in a separate window. 
 
## Syntax 
 
Show-ListView [-PageSize &lt;Int32&gt;] [-Icon &lt;String&gt;] [-InfoTitle &lt;String&gt;] [-InfoDescription &lt;String&gt;] [-Modal] [-ActionData &lt;Object&gt;] [-ViewName &lt;String&gt;] [-MissingDataMessage &lt;String&gt;] [-ActionsInSession] -Data &lt;Object&gt; [-Property &lt;Object[]&gt;] [-Title &lt;String&gt;] [-Width &lt;Int32&gt;] [-Height &lt;Int32&gt;] 
 
 
## Detailed Description 
 
The Show-ListView cmdlet sends the output from a command to a grid view window where the output is displayed in an interactive table.
Because this cmdlet requires a user interface, it does not work in a non-interactive scenarios like within web service calls.
You can use the following features of the table to examine your data:
-- Sort. To sort the data, click a column header. Click again to toggle from ascending to descending order.
-- Quick Filter. Use the "Filter" box at the top of the window to search the text in the table. You can search for text in a particular column, search for literals, and search for multiple words.
-- Execute actions on selected items. To execute action on the data from Show-ListView, Ctrl+click the items you want to be included in the action and press the desired action in the "Actions" chunk in the ribbon.
-- Export contents of the view in XML, CSV, Json, HTML or Excel file and download that onto the user's computer. The downloaded results will take into account current filter and order of the items. 
 
© 2010-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -PageSize&nbsp; &lt;Int32&gt; 
 
Number of results shown per page. 
 
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
 
### -Icon&nbsp; &lt;String&gt; 
 
Icon of the result window. (Shows in the top/left corner and on the Sitecore taskbar). 
 
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
 
### -InfoTitle&nbsp; &lt;String&gt; 
 
Title on the panel that appears below the ribbon in the results window. 
 
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
 
### -InfoDescription&nbsp; &lt;String&gt; 
 
Description that appears on the panel below the ribbon in the results window. 
 
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
 
### -Modal&nbsp; &lt;SwitchParameter&gt; 
 
If this parameter is provided Results will show in a new window (in Sitecore 6.x up till Sitecore 7.1) or in a modal overlay (Sitecore 7.2+) 
 
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
 
### -ActionData&nbsp; &lt;Object&gt; 
 
Additional data what will be passed to the view. All actions that are executed from that view window will have that data accessible to them as $actionData variable. 
 
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
 
### -ViewName&nbsp; &lt;String&gt; 
 
View signature name - this can be used by action commandlets to determine whether to show an action or not using the Show/Enable rules. 
 
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
 
### -MissingDataMessage&nbsp; &lt;String&gt; 
 
If no Items were provided for -Data parameter the message provided in this parameter will be shown in the middle of the List View dialog to notify users of the lack of items to display. 
 
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
 
### -ActionsInSession&nbsp; &lt;SwitchParameter&gt; 
 
If this parameter is specified actions will be executed in the same session as the one in which the commandlet is executed. 
 
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
 
### -Data&nbsp; &lt;Object&gt; 
 
Data to be displayed in the view. 
 
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
            <td>true (ByValue)</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
### -Property&nbsp; &lt;Object[]&gt; 
 
Specifies the object properties that appear in the display and the order in which they appear. Type one or more property names (separated by commas), or use a hash table to display a calculated property.

The value of the Property parameter can be a new calculated property. To create a calculated, property, use a hash table. Valid keys are:
-- Name (or Label) &lt;string&gt;
-- Expression &lt;string&gt; or &lt;script block&gt; 
 
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
 
Title of the results window. 
 
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
 
Width of the results window. 
 
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
 
Height of the results window. 
 
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
 
* System.Management.Automation.PSObject 
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* System.Management.Automation.PSObject 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 1 
 
This command formats information about Sitecore items in a table. The Get-ChildItem cmdlet gets objects representing the items. 
The pipeline operator (|) passes the object to the Show-ListView command. Show-ListView displays the objects in a table. 
 
```powershell   
 
PS master:\> Get-Item -path master:\* | Show-ListView -Property Name, DisplayName, ProviderPath, TemplateName, Language 
 
``` 
 
### EXAMPLE 2 
 
This command formats information about Sitecore items in a table. The Get-ItemReferrer cmdlet gets all references of the "Sample Item" template. 
The pipeline operator (|) passes the object to the Show-ListView command. Show-ListView displays the objects in a table.
The Properties are not displaying straight properties but use the Name/Expression scheme to provide a nicely named values that 
like in the case of languages which are aggregarde form the "Languages" property. 
 
```powershell   
 
PS master:\> Get-ItemReferrer -path 'master:\templates\Sample\Sample Item' | 
                 Show-ListView -Property `
                     @{Label="Name"; Expression={$_.DisplayName} }, 
                     @{Label="Path"; Expression={$_.Paths.Path} }, 
                     @{Label="Languages"; Expression={$_.Languages | % { $_.Name + ", "} } } 
 
``` 
 
## Related Topics 
 
* Update-ListView
* Out-GridView
* Format-Table
* Read-Variable
* Show-Alert
* Show-Application
* Show-Confirm
* Show-FieldEditor
* Show-Input
* Show-ModalDialog
* Show-Result
* Show-YesNoCancel
* <a href='http://blog.najmanowicz.com/2014/10/25/creating-beautiful-sitecore-reports-easily-with-powershell-extensions/' target='_blank'>http://blog.najmanowicz.com/2014/10/25/creating-beautiful-sitecore-reports-easily-with-powershell-extensions/</a><br/>
* <a href='http://michaellwest.blogspot.com/2014/04/reports-with-sitecore-powershell.html' target='_blank'>http://michaellwest.blogspot.com/2014/04/reports-with-sitecore-powershell.html</a><br/>
* <a href='http://sitecorejunkie.com/2014/05/28/create-a-custom-report-in-sitecore-powershell-extensions/' target='_blank'>http://sitecorejunkie.com/2014/05/28/create-a-custom-report-in-sitecore-powershell-extensions/</a><br/>
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>


