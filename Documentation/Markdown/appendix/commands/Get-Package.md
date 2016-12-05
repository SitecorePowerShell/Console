# Get-Package 
 
Loads the package definition (xml) from the specified path. 
 
## Syntax 
 
Get-Package [-Path] &lt;String&gt; 
 
 
## Detailed Description 
 
The Get-Package commands loads the package definition (xml) and returns the package project. Package definitions can be created by PowerShell scripts using the Export-Package command (without the -Zip parameter) 
 
© 2010-2016 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Path&nbsp; &lt;String&gt; 
 
Path to the package file. If the path is not absolute the path needs to be relative to the Sitecore Package path defined in the "PackagePath" setting and later exposed in the Sitecore.Shell.Applications.Install.PackageProjectPath 
 
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
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* Sitecore.Install.PackageProject 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 
 
 
 
```powershell   
 
PS master:\> Get-Package -Path master:\content\home 
 
``` 
 
## Related Topics 
 
