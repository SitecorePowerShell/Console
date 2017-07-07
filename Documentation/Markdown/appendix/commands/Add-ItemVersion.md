# Add-ItemVersion 
 
Creates a version of the item in a new language based on an existing language version. 
 
## Syntax 
 
Add-ItemVersion [-Item] &lt;Item&gt; [-Recurse] [-IfExist &lt;Append | Skip | OverwriteLatest&gt;] [-TargetLanguage &lt;String[]&gt;] [-DoNotCopyFields] [-IgnoredFields &lt;String[]&gt;] [-Language &lt;String[]&gt;] 
 
Add-ItemVersion [-Path] &lt;String&gt; [-Recurse] [-IfExist &lt;Append | Skip | OverwriteLatest&gt;] [-TargetLanguage &lt;String[]&gt;] [-DoNotCopyFields] [-IgnoredFields &lt;String[]&gt;] [-Language &lt;String[]&gt;] 
 
Add-ItemVersion -Id &lt;String&gt; [-Database &lt;String&gt;] [-Recurse] [-IfExist &lt;Append | Skip | OverwriteLatest&gt;] [-TargetLanguage &lt;String[]&gt;] [-DoNotCopyFields] [-IgnoredFields &lt;String[]&gt;] [-Language &lt;String[]&gt;] 
 
 
## Detailed Description 
 
Creates a new version of the item in a specified language based on an existing language/version.
Based on parameters you can make the command bahave differently when a version in the target language already exists and define which fields if any should be copied over from the original language. 
 
© 2010-2017 Adam Najmanowicz, Michael West. All rights reserved. Sitecore PowerShell Extensions## Aliases
The following abbreviations are aliases for this cmdlet:  
* Add-ItemLanguage 
 
## Parameters 
 
### -Recurse&nbsp; &lt;SwitchParameter&gt; 
 
Process the item and all of its children. 
 
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
 
### -IfExist&nbsp; &lt;ActionIfExists&gt; 
 
Default vaule is Append
Accepts one of 3 pretty self explanatory actions: 
- Append - [Default] if language version exists create a new version with values copied from the original language
- Skip - if language version exists don't do anything
- OverwriteLatest - if language version exists overwrite the last version with values copied from the original language 
 
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
 
### -TargetLanguage&nbsp; &lt;String[]&gt; 
 
Language or a list of languages that should be created 
 
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
 
### -DoNotCopyFields&nbsp; &lt;SwitchParameter&gt; 
 
Creates a new version in the target language but does not copy field values from the original language 
 
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
 
### -IgnoredFields&nbsp; &lt;String[]&gt; 
 
List of fields that should not be copied over from original item. As an example, use "__Security" if you don't want the new version to have the same restrictions as the original version.

In addition to the fields in -IgnoredFields the following fields are ignored as configured in Cognifide.PowerShell.config file in the following location:
configuration/sitecore/powershell/translation/ignoredFields.

Fields ignored out of the box include:
- __Archive date
- __Archive Version date
- __Lock
- __Owner
- __Page Level Test Set Definition
- __Reminder date
- __Reminder recipients
- __Reminder text 
 
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
 
Language that will be used as source language. If not specified the current user language will be used. 
 
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
 
The item / version to be processed. 
 
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
 
Path to the item to be processed - additionally specify Language parameter to fetch different item language than the current user language. 
 
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
 
Id of the item to be processed - additionally specify Language parameter to fetch different item language than the current user language. 
 
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
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* Sitecore.Data.Items.Item 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 1 
 
Translate the Home Item from English to US and Polish leaving the "Title" field blank. If a version exists don't do anything 
 
```powershell   
 
PS master:\> Add-ItemVersion -Path "master:\content\home" -Language "en" -TargetLanguage "pl-pl", "en-us" -IfExist Skip -IgnoredFields "Title" 
 
``` 
 
### EXAMPLE 2 
 
 
 
```powershell   
 
Add a Japanese version to /sitecore/content/home item in the master database based on itself
PS master:\> Add-ItemVersion -Path "master:\content\home" -Language ja-JP -IfExist Append 
 
``` 
 
### EXAMPLE 3 
 
Translate the children of Home item (but only those of Template Name "Sample Item") from English to US and Polish. If a version exists create a new version for that language. Display results in a table listing item name, language and created version number. 
 
```powershell   
 
Get-ChildItem "master:\content\home" -Language "en" -Recurse | `
    Where-Object { $_.TemplateName -eq "Sample Item" } | `
    Add-ItemVersion -TargetLanguage "pl-pl" -IfExist Append | `
    Format-Table Name, Language, Version -auto 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>* [Remove-ItemVersion](/appendix/commands/Remove-ItemVersion.md)* New-Item* <a href='https://gist.github.com/AdamNaj/b36ea095e3668c22c07e' target='_blank'>https://gist.github.com/AdamNaj/b36ea095e3668c22c07e</a><br/>
