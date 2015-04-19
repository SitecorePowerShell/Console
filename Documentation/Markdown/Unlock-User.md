# Unlock-User 
 
Unlocks the specified Sitecore user. 
 
## Syntax 
 
Unlock-User [-Identity] &lt;AccountIdentity&gt; 
 
Unlock-User -Instance &lt;User&gt; 
 
 
## Detailed Description 
 
The Unlock-User cmdlet gets a user and unlocks the account in Sitecore.

The Identity parameter specifies the Sitecore user to get. You can specify a user by its local name or fully qualified name.
You can also specify user object variable, such as $&lt;user&gt;. 
 
© 2010-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Identity&nbsp; &lt;AccountIdentity&gt; 
 
Specifies the Sitecore user by providing one of the following values.

    Local Name
        Example: michael
    Fully Qualified Name
        Example: sitecore\michael 
 
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
 
### -Instance&nbsp; &lt;User&gt; 
 
Specifies the Sitecore user by providing an instance of a user. 
 
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
Represents the identity of a user.

Sitecore.Security.Accounts.User
One or more user instances. 
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* System.String
Represents the identity of a user.

Sitecore.Security.Accounts.User
One or more user instances. 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 1 
 
 
 
```powershell   
 
PS master:\> Unlock-User -Identity michael 
 
``` 
 
### EXAMPLE 2 
 
 
 
```powershell   
 
PS master:\> Get-User -Filter * | Unlock-User 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>
* Get-User
* New-User
* Remove-User
* Set-User


