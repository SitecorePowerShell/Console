# Get-Domain 
 
Gets all available domains or the specified domain. 
 
## Syntax 
 
Get-Domain [-Name &lt;String&gt;] 
 
 
## Detailed Description 
 
The Get-Domain command returns all the domains or the specified domain. 
 
© 2010-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Name&nbsp; &lt;String&gt; 
 
The name of the domai 
 
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
 
* System.String
Represents the name of a domain. 
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* Sitecore.Security.Domains.Domai 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 

### EXAMPLE 1 
 
 
```powershell   
 
PS master:\> Get-Domain -Name sitecore

Name            AccountPrefix   EnsureAnonymousUser    LocallyManaged
----            -------------   -------------------    --------------
sitecore        sitecore\       False                  False 
 
``` 

 
### EXAMPLE 2
 
 
```powershell   
 
PS master:\> Get-Domain

		
Name            AccountPrefix   EnsureAnonymousUser    LocallyManaged
----            -------------   -------------------    --------------
sitecore        sitecore\       False                  False
extranet        extranet\       True                   False
default         default\        True                   False		
		
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>* Remove-Domain* New-Domain
