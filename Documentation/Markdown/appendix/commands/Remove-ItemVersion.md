# Remove-ItemVersion 
 
Removes Language/Version from a single item or a branch of items 
 
## Syntax 
 
Remove-ItemVersion -Language &lt;String[]&gt; [-Version &lt;String[]&gt;] [-ExcludeLanguage &lt;String[]&gt;] [-Path] &lt;String&gt; [-Recurse] [-MaxRecentVersions &lt;Int32&gt;] 
 
Remove-ItemVersion -Language &lt;String[]&gt; [-Version &lt;String[]&gt;] [-ExcludeLanguage &lt;String[]&gt;] -Id &lt;String&gt; [-Database &lt;String&gt;] [-Recurse] [-MaxRecentVersions &lt;Int32&gt;] 
 
Remove-ItemVersion [-Language &lt;String[]&gt;] [-Version &lt;String[]&gt;] [-ExcludeLanguage &lt;String[]&gt;] [-Item] &lt;Item&gt; [-Recurse] [-MaxRecentVersions &lt;Int32&gt;] 
 
 
## Detailed Description 
 
Removes Language/Version from a an Item either sent from pipeline or defined with Path or ID. A single language or a list of languages can be defined using the Language parameter. 
Language  parameter supports globbing so you can delete whole language groups using wildcards. 
 
© 2010-2017 Adam Najmanowicz, Michael West. All rights reserved. Sitecore PowerShell Extensions## Aliases
The following abbreviations are aliases for this cmdlet:  
* Remove-ItemLanguage 
 
## Parameters 
 
### -Recurse&nbsp; &lt;SwitchParameter&gt; 
 
Deleted language versions from the item and all of its children. 
 
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
 
Language(s) that should be deleted form the provided item(s).
A single language or a list of languages can be defined using the parameter. 
Language parameter supports globbing so you can delete whole language groups using wildcards. 
 
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
 
### -Version&nbsp; &lt;String[]&gt; 
 
Version(s) that should be deleted form the provided item(s).
A single version or a list of versions can be defined using the parameter. 
Version parameter supports globbing so you can delete whole version groups using wildcards. 
 
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
 
### -ExcludeLanguage&nbsp; &lt;String[]&gt; 
 
Language(s) that should NOT be deleted form the provided item(s).
A single language or a list of languages can be defined using the parameter. 
Language parameter supports globbing so you can delete whole language groups using wildcards.

If Language parameter is not is not specified but ExcludeLanguage is provided, the default value of "*" is assumed for Language parameter. 
 
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
 
### -MaxRecentVersions&nbsp; &lt;Int32&gt; 
 
If provided - trims the selected language to value specified by this parameter. 
 
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
 
The item/version to be processed. You can pipe a specific version of the item for it to be removed. 
 
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
 
Path to the item to be processed - can work with Language parameter to narrow the publication scope. 
 
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
 
Id of the item to be processed - can work with Language parameter to narrow the publication scope. 
 
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
 
Database containing the item to be processed - can work with Language parameter to narrow the publication scope. 
 
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
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 1 
 
Remove Polish and Spanish language from /sitecore/content/home item in the master database 
 
```powershell   
 
PS master:\> Remove-ItemVersion -Path master:\content\home -Language "pl-pl", "es-es" 
 
``` 
 
### EXAMPLE 2 
 
Remove all english based languages defined in /sitecore/content/home item and all of its children in the master database 
 
```powershell   
 
PS master:\> Remove-ItemVersion -Path master:\content\home -Language "en-*" -Recurse 
 
``` 
 
### EXAMPLE 3 
 
Remove all languages except those that are "en" based defined in /sitecore/content/home item and all of its children in the master database 
 
```powershell   
 
PS master:\> Remove-ItemVersion -Path master:\content\home -ExcludeLanguage "en*" -Recurse 
 
``` 
 
### EXAMPLE 4 
 
Trim all languages to 3 latest versions for /sitecore/content/home item and all of its children in the master database 
 
```powershell   
 
PS master:\> Remove-ItemVersion -Path master:\content\home -Language * -Recurse 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>* [Add-ItemVersion](/appendix/commands/Add-ItemVersion.md)* Remove-Item* <a href='https://gist.github.com/AdamNaj/b36ea095e3668c22c07e' target='_blank'>https://gist.github.com/AdamNaj/b36ea095e3668c22c07e</a><br/>
