# Get-Role 
 
Returns one or more Sitecore roles using the specified criteria. 
 
## Syntax 
 
Get-Role [-Identity] &lt;AccountIdentity&gt; 
 
Get-Role -Filter &lt;String&gt; 
 
 
## Detailed Description 
 
The Get-Role command returns one or more Sitecore roles using the specified criteria.

The Identity parameter specifies the Sitecore role to get. You can specify a role by its local name or fully qualified name.
You can also specify role object variable, such as $&lt;role&gt;.

To search for and retrieve more than one role, use the Filter parameter. 
 
© 2010-2017 Adam Najmanowicz, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Identity&nbsp; &lt;AccountIdentity&gt; 
 
Specifies the Sitecore role by providing one of the following values.

    Local Name
        Example: developer
    Fully Qualified Name
        Example: sitecore\developer 
 
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
 
### -Filter&nbsp; &lt;String&gt; 
 
Specifies a simple pattern to match Sitecore roles.

Examples:
The following examples show how to use the filter syntax.

To get all the roles, use the asterisk wildcard:
Get-Role -Filter *

To get all the roles in a domain use the following command:
Get-Role -Filter "sitecore\*" 
 
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
 
## Inputs 
 
The input type is the type of the objects that you can pipe to the cmdlet. 
 
* System.String
Represents the identity of a role. 
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* Sitecore.Security.Accounts.Role
Returns one or more roles. 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 1 
 
 
 
```powershell   
 
PS master:\> Get-Role -Identity sitecore\developer

Name                                     Domain       IsEveryone
----                                     ------       ----------
sitecore\developer                       sitecore     False 
 
``` 
 
### EXAMPLE 2 
 
 
 
```powershell   
 
PS master:\> "sitecore\developer","sitecore\author" | Get-Role

Name                                     Domain       IsEveryone
----                                     ------       ----------
sitecore\author                          sitecore     False
sitecore\developer                       sitecore     False 
 
``` 
 
### EXAMPLE 3 
 
 
 
```powershell   
 
PS master:\> Get-Role -Filter sitecore\d*

Name                                     Domain       IsEveryone
----                                     ------       ----------
sitecore\Designer                        sitecore     False
sitecore\Developer                       sitecore     False 
 
``` 
 
### EXAMPLE 4 
 
Expand the MemberOf property to see a list of roles that the specified role is a member. 
 
```powershell   
 
PS master:\> Get-Role -Identity sitecore\developer | Select-Object -ExpandProperty MemberOf

Name                                     Domain       IsEveryone
----                                     ------       ----------
sitecore\Sitecore Client Configuring     sitecore     False
sitecore\Sitecore Client Developing      sitecore     False
sitecore\Designer                        sitecore     False
sitecore\Author                          sitecore     False
sitecore\Sitecore Client Maintaining     sitecore     False 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>* [Get-RoleMember](/appendix/commands/Get-RoleMember.md)
