# Set-ItemTemplate 
 
Sets the item template. 
 
## Syntax 
 
Set-ItemTemplate -Item &lt;Item&gt; -TemplateItem &lt;TemplateItem&gt; [-FieldsToCopy &lt;Hashtable&gt;] 
 
Set-ItemTemplate -Item &lt;Item&gt; -Template &lt;String&gt; [-FieldsToCopy &lt;Hashtable&gt;] 
 
Set-ItemTemplate -Path &lt;String&gt; -TemplateItem &lt;TemplateItem&gt; [-FieldsToCopy &lt;Hashtable&gt;] 
 
Set-ItemTemplate -Path &lt;String&gt; -Template &lt;String&gt; [-FieldsToCopy &lt;Hashtable&gt;] 
 
Set-ItemTemplate -Id &lt;String&gt; -TemplateItem &lt;TemplateItem&gt; [-FieldsToCopy &lt;Hashtable&gt;] 
 
Set-ItemTemplate -Id &lt;String&gt; -Template &lt;String&gt; [-FieldsToCopy &lt;Hashtable&gt;] 
 
Set-ItemTemplate [-Database &lt;String&gt;] [-FieldsToCopy &lt;Hashtable&gt;] 
 
 
## Detailed Description 
 
The Set-ItemTemplate command sets the template for an item. 
 
© 2010-2017 Adam Najmanowicz, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Item&nbsp; &lt;Item&gt; 
 
The item to set the template for. 
 
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
 
### -Path&nbsp; &lt;String&gt; 
 
Path to the item to set the template for. 
 
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
 
### -Id&nbsp; &lt;String&gt; 
 
Id of the item to set the template for. 
 
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
 
### -TemplateItem&nbsp; &lt;TemplateItem&gt; 
 
Sitecore item representing the template. 
 
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
 
### -Template&nbsp; &lt;String&gt; 
 
Path representing the template item. This must be of the same database as the item to be altered. 
 
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
 
### -FieldsToCopy&nbsp; &lt;Hashtable&gt; 
 
Hashtable of key value pairs mapping the old template field to a new template field.

@{"Title"="Headline";"Text"="Copy"} 
 
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
 
### -Database&nbsp; &lt;String&gt; 
 
Database containing the item to set the template for - required if item is specified with Id. 
 
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
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West, Alex Washtell 
 
## Examples 
 
### EXAMPLE 1 
 
Set template of /sitecore/content/home item using a Template path. 
 
```powershell   
 
PS master:\> Set-ItemTemplate -Path master:/sitecore/content/home -Template "/sitecore/templates/User Defined/Page" 
 
``` 
 
### EXAMPLE 2 
 
Set template of /sitecore/content/home item using a TemplateItem. 
 
```powershell   
 
PS master:\> $template = Get-ItemTemplate -Path master:\content\home\page1
       PS master:\> Set-ItemTemplate -Path master:\content\home\page2 -TemplateItem $template 
 
``` 
 
### EXAMPLE 3 
 
Set the template and remap fields to their new name. 
 
```powershell   
 
Set-ItemTemplate -Path "master:\content\home\Page1" `
    -Template "User Defined/Target" `
    -FieldsToCopy @{Field1="Field4"; Field2="Field5"; Field3="Field6"} 
 
``` 
 
## Related Topics 
 
* [Get-ItemTemplate](/appendix/commands/Get-ItemTemplate.md)* [Add-BaseTemplate](/appendix/commands/Add-BaseTemplate.md)* [Remove-BaseTemplate](/appendix/commands/Remove-BaseTemplate.md)* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>
