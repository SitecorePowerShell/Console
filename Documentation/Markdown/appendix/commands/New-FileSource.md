# New-FileSource 
 
Creates new File source that can be added to a Sitecore package. 
 
## Syntax 
 
New-FileSource [-Name] &lt;String&gt; [-Root] &lt;DirectoryInfo&gt; [[-IncludeFilter] &lt;String&gt;] [[-ExcludeFilter] &lt;String&gt;] [-InstallMode &lt;String&gt;] 
 
 
## Detailed Description 
 
Creates new File source that can be added to a Sitecore package. Folder provided as Root will be added as well as all of its content provided it matches the filters. 
 
© 2010-2017 Adam Najmanowicz, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Name&nbsp; &lt;String&gt; 
 
Name of the file source. 
 
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
 
### -Root&nbsp; &lt;DirectoryInfo&gt; 
 
Root folder to include in the package 
 
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
            <td>2</td>
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
 
### -IncludeFilter&nbsp; &lt;String&gt; 
 
Filter that defines which files will be included. 
 
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
            <td>3</td>
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
 
### -ExcludeFilter&nbsp; &lt;String&gt; 
 
Filter that defines which files will NOT be included. 
 
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
            <td>4</td>
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
 
### -InstallMode&nbsp; &lt;String&gt; 
 
 
 
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
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* Sitecore.Install.Files.FileSource 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 
 
Following example creates a new package, adds content of the Console folder under the site folder
saves it in the Sitecore Package folder + gives you an option to download the saved package. 
 
```powershell   
 
# Create package
       $package = new-package "Sitecore PowerShell Extensions";

# Set package metadata
       $package.Sources.Clear();

       $package.Metadata.Author = "Adam Najmanowicz - Cognifide, Michael West";
       $package.Metadata.Publisher = "Cognifide Limited";
       $package.Metadata.Version = "2.7";
       $package.Metadata.Readme = 'This text will be visible to people installing your package'
       
# Add content of the Console folder in the site folder to the package
       $source = New-FileSource -Name "Console Assets" -Root "$AppPath\Console"
       $package.Sources.Add($source);

# Save package
       Export-Package -Project $package -Path "$($package.Name)-$($package.Metadata.Version).zip" -Zip

# Offer the user to download the package
       Download-File "$SitecorePackageFolder\$($package.Name)-$($package.Metadata.Version).zip" 
 
``` 
 
## Related Topics 
 
* [Export-Package](/appendix/commands/Export-Package.md)* [Get-Package](/appendix/commands/Get-Package.md)* Import-Package* [Install-UpdatePackage](/appendix/commands/Install-UpdatePackage.md)* [New-ExplicitFileSource](/appendix/commands/New-ExplicitFileSource.md)* [New-ExplicitItemSource](/appendix/commands/New-ExplicitItemSource.md)* [New-ItemSource](/appendix/commands/New-ItemSource.md)* [New-Package](/appendix/commands/New-Package.md)* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>* <a href='http://blog.najmanowicz.com/2011/12/19/continuous-deployment-in-sitecore-with-powershell/' target='_blank'>http://blog.najmanowicz.com/2011/12/19/continuous-deployment-in-sitecore-with-powershell/</a><br/>* <a href='https://gist.github.com/AdamNaj/f4251cb2645a1bfcddae' target='_blank'>https://gist.github.com/AdamNaj/f4251cb2645a1bfcddae</a><br/>* <a href='https://www.youtube.com/watch?v=60BGRDNONo0&list=PLph7ZchYd_nCypVZSNkudGwPFRqf1na0b&index=7' target='_blank'>https://www.youtube.com/watch?v=60BGRDNONo0&list=PLph7ZchYd_nCypVZSNkudGwPFRqf1na0b&index=7</a><br/>
