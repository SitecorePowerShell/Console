# Export-Item 
 
Exports (serializes) the Sitecore item to the filesystem. 
 
## Syntax 
 
Export-Item [-Entry &lt;IncludeEntry&gt;] [-Recurse] [-ItemPathsAbsolute] [-Root &lt;String&gt;] 
 
Export-Item [-Item] &lt;Item&gt; [-Recurse] [-ItemPathsAbsolute] [-Root &lt;String&gt;] 
 
Export-Item [-Path] &lt;String&gt; [-Recurse] [-ItemPathsAbsolute] [-Root &lt;String&gt;] 
 
Export-Item -Id &lt;String&gt; [-Database &lt;String&gt;] [-Recurse] [-ItemPathsAbsolute] [-Root &lt;String&gt;] 
 
 
## Detailed Description 
 
The Export-Item command serializes Sitecore items to the filesystem. The alias for the command is Serialize-Item.
The simplest command syntax is:
Export-Item -path "master:\content"

or

Get-Item "master:\content" | Export-Item

Both of them will serialize the content item in the master database. In first case we pass the path to the item as a parameter, in second case we serialize the items which come from the pipeline. 
You can send more items from the pipeline to the Export-Item command, e.g. if you need to serialize all the descendants of the home item created by sitecore\admin, you can use:

Get-Childitem "master:\content\home" -recurse | Where-Object { $_."__Created By" -eq "sitecore\admin" } | Export-Item 
 
© 2010-2016 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions## Aliases
The following abbreviations are aliases for this cmdlet:  
* Serialize-Item 
 
## Parameters 
 
### -Entry&nbsp; &lt;IncludeEntry&gt; 
 
Serialization preset to be serialized. Obtain the preset through the use of Get-Preset command. 
 
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
 
### -Recurse&nbsp; &lt;SwitchParameter&gt; 
 
Process the item and all of its children - switch which decides if serialization concerns only the single item or the whole tree below the item, e.g.

Export-Item -path "master:\content\articles" -recurse

Root - directory where the serialized files should be saved, e.g.
Export-Item -path "master:\content" -Root "c:\tmp" 
 
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
 
### -ItemPathsAbsolute&nbsp; &lt;SwitchParameter&gt; 
 
Works only with Root parameter and decides if folder structure starting from "sitecore\content" should be created, e.g. if you want to serialize articles item in directory c:\tmp\sitecore\content you can use. For example:
Export-Item -Path "master:\content\articles" -ItemPathsAbsolute -Root "c:\tmp" 
 
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
 
Directory where the serialized files should be saved, e.g.

Export-Item -Path "master:\content" -Root "c:\tmp" 
 
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
 
You can pass the id of serialized item instead of path, e.g.
Export-Item -id "{0DE95AE4-41AB-4D01-9EB0-67441B7C2450}" 
 
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
 
Help Author: Marek Musielak, Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 
 
 
 
```powershell   
 
PS master:\> Export-Item -Path master:\content\home 
 
``` 
 
## Related Topics 
 
