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
 
* [Export-Package](/appendix/commands/Export-Package.md)* Import-Package* [Install-UpdatePackage](/appendix/commands/Install-UpdatePackage.md)* [New-ExplicitFileSource](/appendix/commands/New-ExplicitFileSource.md)* [New-ExplicitItemSource](/appendix/commands/New-ExplicitItemSource.md)* [New-FileSource](/appendix/commands/New-FileSource.md)* [New-ItemSource](/appendix/commands/New-ItemSource.md)* [New-Package](/appendix/commands/New-Package.md)* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>* <a href='http://blog.najmanowicz.com/2011/12/19/continuous-deployment-in-sitecore-with-powershell/' target='_blank'>http://blog.najmanowicz.com/2011/12/19/continuous-deployment-in-sitecore-with-powershell/</a><br/>* <a href='https://gist.github.com/AdamNaj/f4251cb2645a1bfcddae' target='_blank'>https://gist.github.com/AdamNaj/f4251cb2645a1bfcddae</a><br/>* <a href='https://www.youtube.com/watch?v=60BGRDNONo0&list=PLph7ZchYd_nCypVZSNkudGwPFRqf1na0b&index=7' target='_blank'>https://www.youtube.com/watch?v=60BGRDNONo0&list=PLph7ZchYd_nCypVZSNkudGwPFRqf1na0b&index=7</a><br/>
