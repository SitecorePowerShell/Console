# Import-Item 
 
Deserializes sitecore item from server disk drive. This command used to be named Deserialize-Item - a matching alias added for compatibility with older scripts. 
 
## Syntax 
 
Import-Item [-Database &lt;Database&gt;] [-Root &lt;String&gt;] [-UseNewId] [-DisableEvents] [-ForceUpdate] 
 
Import-Item [-Item &lt;Item&gt;] [-Recurse] [-Root &lt;String&gt;] [-UseNewId] [-DisableEvents] [-ForceUpdate] 
 
Import-Item [-Preset &lt;IncludeEntry&gt;] [-Root &lt;String&gt;] [-UseNewId] [-DisableEvents] [-ForceUpdate] 
 
Import-Item [-Path &lt;String&gt;] [-Recurse] [-Root &lt;String&gt;] [-UseNewId] [-DisableEvents] [-ForceUpdate] 
 
 
## Detailed Description 
 
Deserialization of items with Sitecore Powershell Extensions uses Import-Item command. The simplest syntax requires 2 parameters:
-Path - which is a path to the item on the drive but without .item extension. If the item does not exist in the Sitecore tree yet, you need to pass the parent item path.
-Root - the directory which is the root of serialization. Trailing slash \ character is required, 

e.g.:

Import-Item -Path "c:\project\data\serialization\master\sitecore\content\articles" -Root "c:\project\data\serialization\" 
 
© 2010-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions## Aliases
The following abbreviations are aliases for this cmdlet:  
* Deserialize-Item 
 
## Parameters 
 
### -Database&nbsp; &lt;Database&gt; 
 
Database to contain the item to be deserialized. 
 
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
            <td>true (ByValue, ByPropertyName)</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
### -Item&nbsp; &lt;Item&gt; 
 
The item to be serialized. 
 
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
            <td>true (ByValue, ByPropertyName)</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
### -Preset&nbsp; &lt;IncludeEntry&gt; 
 
Name of the preset to be deserialized. 
 
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
            <td>true (ByValue, ByPropertyName)</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
### -Path&nbsp; &lt;String&gt; 
 
Path to the item on the drive but without .item extension. If the item does not exist in the Sitecore tree yet, you need to pass the parent item path. 
 
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
 
### -Recurse&nbsp; &lt;SwitchParameter&gt; 
 
If included in the execution - dederializes both the item and all of its children. 
 
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
 
### -Root&nbsp; &lt;String&gt; 
 
The directory which is the root of serialization. Trailing slash \ character is required. if not specified the default root will be used. 
 
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
 
### -UseNewId&nbsp; &lt;SwitchParameter&gt; 
 
Tells Sitecore if each of the items should be created with a newly generated ID, e.g.
Import-Item -path "c:\project\data\serialization\master\sitecore\content\articles" -root "c:\project\data\serialization\" -usenewid -recurse 
 
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
 
### -DisableEvents&nbsp; &lt;SwitchParameter&gt; 
 
If set Sitecore will use EventDisabler during deserialization, e.g.:
Import-Item -path "c:\project\data\serialization\master\sitecore\content\articles" -root "c:\project 
 
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
 
### -ForceUpdate&nbsp; &lt;SwitchParameter&gt; 
 
Forces item to be updated even if it has not changed. 
 
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
 
## Notes 
 
Help Author: Marek Musielak, Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 1 
 
 
 
```powershell   
 
PS master:\> Import-Item -path "c:\project\data\serialization\master\sitecore\content\articles" -root "c:\project\data\serialization\" 
 
``` 
 
### EXAMPLE 2 
 
 
 
```powershell   
 
PS master:\> Import-Item -path "c:\project\data\serialization\master\sitecore\content\articles" -root "c:\project\data\serialization\" -recurse 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>
* Export-Item
* Get-Preset
* <a href='http://www.cognifide.com/blogs/sitecore/serialization-and-deserialization-with-sitecore-powershell-extensions/' target='_blank'>http://www.cognifide.com/blogs/sitecore/serialization-and-deserialization-with-sitecore-powershell-extensions/</a><br/>
* <a href='https://www.youtube.com/watch?v=60BGRDNONo0&list=PLph7ZchYd_nCypVZSNkudGwPFRqf1na0b&index=7' target='_blank'>https://www.youtube.com/watch?v=60BGRDNONo0&list=PLph7ZchYd_nCypVZSNkudGwPFRqf1na0b&index=7</a><br/>
* <a href='https://gist.github.com/AdamNaj/6c86f61510dc3d2d8b2f' target='_blank'>https://gist.github.com/AdamNaj/6c86f61510dc3d2d8b2f</a><br/>
* <a href='http://stackoverflow.com/questions/20266841/sitecore-powershell-deserialization' target='_blank'>http://stackoverflow.com/questions/20266841/sitecore-powershell-deserialization</a><br/>
* <a href='http://stackoverflow.com/questions/20195718/sitecore-serialization-powershell' target='_blank'>http://stackoverflow.com/questions/20195718/sitecore-serialization-powershell</a><br/>
* <a href='http://stackoverflow.com/questions/20283438/sitecore-powershell-deserialization-core-db' target='_blank'>http://stackoverflow.com/questions/20283438/sitecore-powershell-deserialization-core-db</a><br/>


