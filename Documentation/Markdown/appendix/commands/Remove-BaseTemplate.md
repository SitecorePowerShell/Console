# Remove-BaseTemplate 
 
Remove one or more base templates from a template item. 
 
## Syntax 
 
Remove-BaseTemplate -Item &lt;Item&gt; -TemplateItem &lt;TemplateItem[]&gt; 
 
Remove-BaseTemplate -Item &lt;Item&gt; -Template &lt;String[]&gt; 
 
Remove-BaseTemplate -Path &lt;String&gt; -TemplateItem &lt;TemplateItem[]&gt; 
 
Remove-BaseTemplate -Path &lt;String&gt; -Template &lt;String[]&gt; 
 
Remove-BaseTemplate -Id &lt;String&gt; -TemplateItem &lt;TemplateItem[]&gt; 
 
Remove-BaseTemplate -Id &lt;String&gt; -Template &lt;String[]&gt; 
 
Remove-BaseTemplate [-Database &lt;String&gt;] 
 
 
## Detailed Description 
 
The Remove-BaseTemplate command removes one or more base templates from a template item. 
 
© 2010-2017 Adam Najmanowicz, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Item&nbsp; &lt;Item&gt; 
 
The item to remove the base template from. 
 
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
 
Path to the item to remove the base template from. 
 
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
 
Id of the item to remove the base template from. 
 
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
 
### -TemplateItem&nbsp; &lt;TemplateItem[]&gt; 
 
Sitecore item or list of items of base templates to remove. 
 
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
 
### -Template&nbsp; &lt;String[]&gt; 
 
Path representing the template item to remove as a base template. This must be of the same database as the item to be altered.
Note that this parameter only supports a single template. 
 
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
 
Database containing the item to remove the base template from - required if item is specified with Id. 
 
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
 
Remove base template of /sitecore/templates/User Defined/BaseTemplate from a template, using a path. 
 
```powershell   
 
PS master:\> Remove-BaseTemplate -Path "master:/sitecore/content/User Defined/Page" -Template "/sitecore/templates/User Defined/BaseTemplate" 
 
``` 
 
### EXAMPLE 2 
 
Remove multiple base templates from a template, using items. 
 
```powershell   
 
PS master:\> $baseA = Get-Item -Path master:/sitecore/content/User Defined/BaseTemplateA
       PS master:\> $baseB = Get-Item -Path master:/sitecore/content/User Defined/BaseTemplateB
       PS master:\> Remove-BaseTemplate -Path "master:/sitecore/content/User Defined/Page" -TemplateItem @($baseA, $baseB) 
 
``` 
 
## Related Topics 
 
* [Add-BaseTemplate](/appendix/commands/Add-BaseTemplate.md)* [Get-ItemTemplate](/appendix/commands/Get-ItemTemplate.md)* [Set-ItemTemplate](/appendix/commands/Set-ItemTemplate.md)* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>
