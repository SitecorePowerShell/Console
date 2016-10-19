# ConvertFrom-CliXml 
 
Imports a CliXml string with data that represents Microsoft .NET objects and creates the objects within PowerShell. 
 
## Syntax 
 
ConvertFrom-CliXml [-InputObject] &lt;String&gt; 
 
 
## Detailed Description 
 
The ConvertFrom-CliXml command imports a CliXml string with data that represents Microsoft .NET Framework objects and creates the objects in PowerShell. 
 
© 2010-2016 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -InputObject&nbsp; &lt;String&gt; 
 
String containing the Xml with serialized objects. 
 
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
            <td>true (ByValue)</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
## Inputs 
 
The input type is the type of the objects that you can pipe to the cmdlet. 
 
* System.String 
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* object 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 
 
 
 
```powershell   
 
PS master:\> #Convert original item to xml
PS master:\> $myCliXmlItem = Get-Item -Path master:\content\home | ConvertTo-CliXml 
PS master:\> #print the CliXml
PS master:\> $myCliXmlItem
PS master:\> #print the Item converted back from CliXml
PS master:\> $myCliXmlItem | ConvertFrom-CliXml 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>* [ConvertTo-CliXml](/appendix/commands/ConvertTo-CliXml.md)* ConvertTo-Xml* ConvertFrom-Xml* Export-CliXml* Import-CliXml* <a href='https://github.com/SitecorePowerShell/Console/issues/218' target='_blank'>https://github.com/SitecorePowerShell/Console/issues/218</a><br/>
