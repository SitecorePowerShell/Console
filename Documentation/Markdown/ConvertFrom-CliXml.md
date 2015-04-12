# ConvertFrom-CliXml 
 
Imports a CLIXML string and creates corresponding objects within PowerShell. 
 
## Syntax 
 
ConvertFrom-CliXml [-InputObject] &lt;String&gt; 
 
 
## Detailed Description 
The ConvertFrom-CliXml cmdlet imports a CLIXML string with data that represents Microsoft .NET Framework objects and creates the objects in PowerShell. 
- 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -InputObject&nbsp; &lt;String&gt; 
 
String containing the XML with serialized objects.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue) |
| Accept Wildcard Characters? | false | 
 
## Inputs 
 
The input type is the type of the objects that you can pipe to the cmdlet. 
 
* System.String 
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* System.String 
 
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
 
* ConvertTo-CliXml 
 
* ConvertTo-Xml 
 
* ConvertFrom-Xml 
 
* Export-CliXml 
 
* Import-CliXml 
 
* <a href='https://github.com/SitecorePowerShell/Console/issues/218' target='_blank'>https://github.com/SitecorePowerShell/Console/issues/218</a><br/>

