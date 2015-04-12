# ConvertTo-CliXml 
 
Returns an XML-based representation of an object or objects. 
 
## Syntax 
 
ConvertTo-CliXml [-InputObject] &lt;PSObject&gt; 
 
 
## Detailed Description 
 
The ConvertTo-CliXml cmdlet returns an XML-based representation of an object or objects provided as InputObject parameter. You can then use the ConvertFrom-CliXml cmdlet to re-create the saved object based on the contents of that XML.

This cmdlet is similar to ConvertTo-XML, except that ConvertTo-CliXml stores the resulting XML in a string. ConvertTo-XML returns the XML, so you can continue to process it in Windows PowerShell. 
 
© 2010-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -InputObject&nbsp; &lt;PSObject&gt; 
 
Specifies the object to be converted. Enter a variable that contains the objects, or type a command or expression that gets the objects. You can also pipe objects to ConvertTo-CliXml. 
 
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
 
* object 
 
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
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>
* ConvertFrom-CliXml
* ConvertFrom-Xml
* ConvertTo-Xml
* Export-CliXml
* Import-CliXml


