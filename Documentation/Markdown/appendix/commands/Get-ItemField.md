# Get-ItemField 
 
Retrieves item fields as either names or fields or template fields. 
 
## Syntax 
 
Get-ItemField [-Language &lt;String[]&gt;] [-Path] &lt;String&gt; [-IncludeStandardFields] [-ReturnType &lt;Name | Field | TemplateField&gt;] [-Name &lt;String[]&gt;] 
 
Get-ItemField [-Language &lt;String[]&gt;] -Id &lt;String&gt; [-Database &lt;String&gt;] [-IncludeStandardFields] [-ReturnType &lt;Name | Field | TemplateField&gt;] [-Name &lt;String[]&gt;] 
 
Get-ItemField [-Language &lt;String[]&gt;] [-Item] &lt;Item&gt; [-IncludeStandardFields] [-ReturnType &lt;Name | Field | TemplateField&gt;] [-Name &lt;String[]&gt;] 
 
 
## Detailed Description 
 
Retrieves item fields as either names or fields or template fields. 
 
© 2010-2016 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -IncludeStandardFields&nbsp; &lt;SwitchParameter&gt; 
 
Includes fields that are defined on "Standard template" 
 
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
 
### -ReturnType&nbsp; &lt;ReturnValue&gt; 
 
Determines type returned. The possible values include:
- Name - strings with field names.
- Field - fields on the item
- TemplateField - template fields. 
 
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
 
### -Name&nbsp; &lt;String[]&gt; 
 
Array of names to include - supports wildcards. 
 
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
 
### -Language&nbsp; &lt;String[]&gt; 
 
Language that will be analysed. If not specified the current user language will be used. Globbing/wildcard supported. 
 
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
 
### -Item&nbsp; &lt;Item&gt; 
 
The item to be analysed. 
 
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
 
### -Path&nbsp; &lt;String&gt; 
 
Path to the item to be analysed. 
 
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
 
### -Id&nbsp; &lt;String&gt; 
 
Id of the item to be analysed. 
 
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
 
### -Database&nbsp; &lt;String&gt; 
 
Database containing the item to be analysed - can work with Language parameter to narrow the publication scope. 
 
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
 
* Sitecore.Data.Items.Item* Sitecore.Data.Templates.TemplateField* Sitecore.Data.Fields.Field 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 1 
 
Get list of names of non standard fields from /sitecore/content/home item 
 
```powershell   
 
PS master:\> Get-ItemField -Path master:\content\home

Text
Title
Image 
 
``` 
 
### EXAMPLE 2 
 
Get list of fields including standard fields from /sitecore/content/home item and list their Name, DisplayName, SectionDisplayName and Description in a table. 
 
```powershell   
 
PS master:\> Get-Item master:\content\home | Get-ItemField -IncludeStandardFields -ReturnType Field -Name "*" | ft Name, DisplayName, SectionDisplayName, Description -auto

Name                                DisplayName                        SectionDisplayName Description
----                                -----------                        ------------------ -----------
__Revision                          Revision                           Statistics
__Standard values                   __Standard values                  Advanced
__Updated by                        Updated by                         Statistics
__Validate Button Validation Rules  Validation Button Validation Rules Validation Rules
__Created                           Created                            Statistics
__Thumbnail                         Thumbnail                          Appearance
__Insert Rules                      Insert Rules                       Insert Options
__Short description                 Short description                  Help
__Created by                        Created by                         Statistics
__Presets                           Presets                            Layout
Text                                Text                               Data               The text is the main content of the document. 
 
``` 
 
## Related Topics 
 
* [Get-ItemTemplate](/appendix/commands/Get-ItemTemplate.md)* [Reset-ItemField](/appendix/commands/Reset-ItemField.md)* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>
