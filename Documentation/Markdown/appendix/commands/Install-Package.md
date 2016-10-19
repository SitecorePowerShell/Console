# Install-Package 
 
Installs a Sitecore package from the specified path. 
 
## Syntax 
 
Install-Package [[-Path] &lt;String&gt;] [-InstallMode &lt;Undefined | Overwrite | Merge | Skip | SideBySide&gt;] [-MergeMode &lt;Undefined | Clear | Append | Merge&gt;] [-DisableIndexing] 
 
 
## Detailed Description 
 
Installs Sitecore package with the ability to provide default responses for merge and overwrite actions. The alias for the command is Import-Package. 
 
© 2010-2016 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions## Aliases
The following abbreviations are aliases for this cmdlet:  
* Import-Package 
 
## Parameters 
 
### -Path&nbsp; &lt;String&gt; 
 
Path to the package file. 
 
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
 
### -InstallMode&nbsp; &lt;InstallMode&gt; 
 
Undefined, Overwrite, Merge, Skip, SideBySide 
 
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
 
### -MergeMode&nbsp; &lt;MergeMode&gt; 
 
Undefined, Clear, Append, Merge 
 
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
 
### -DisableIndexing&nbsp; &lt;SwitchParameter&gt; 
 
 
 
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
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 
 
 
 
```powershell   
 
PS master:\> Install-Package -Path SitecorePowerShellConsole.zip - InstallMode Merge -MergeMode Merge 
 
``` 
 
## Related Topics 
 
* [Export-Package](/appendix/commands/Export-Package.md)* [Get-Package](/appendix/commands/Get-Package.md)* [Install-UpdatePackage](/appendix/commands/Install-UpdatePackage.md)* [New-ExplicitFileSource](/appendix/commands/New-ExplicitFileSource.md)* [New-ExplicitItemSource](/appendix/commands/New-ExplicitItemSource.md)* [New-FileSource](/appendix/commands/New-FileSource.md)* [New-ItemSource](/appendix/commands/New-ItemSource.md)* [New-Package](/appendix/commands/New-Package.md)* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>* <a href='http://blog.najmanowicz.com/2011/12/19/continuous-deployment-in-sitecore-with-powershell/' target='_blank'>http://blog.najmanowicz.com/2011/12/19/continuous-deployment-in-sitecore-with-powershell/</a><br/>* <a href='https://gist.github.com/AdamNaj/f4251cb2645a1bfcddae' target='_blank'>https://gist.github.com/AdamNaj/f4251cb2645a1bfcddae</a><br/>* <a href='https://www.youtube.com/watch?v=60BGRDNONo0&list=PLph7ZchYd_nCypVZSNkudGwPFRqf1na0b&index=7' target='_blank'>https://www.youtube.com/watch?v=60BGRDNONo0&list=PLph7ZchYd_nCypVZSNkudGwPFRqf1na0b&index=7</a><br/>
