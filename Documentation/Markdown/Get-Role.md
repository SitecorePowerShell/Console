# Get-Role 
 
Gets one or more Sitecore roles. 
 
## Syntax 
 
Get-Role [-Identity] &lt;AccountIdentity&gt; 
 
Get-Role -Filter &lt;String&gt; 
 
 
## Detailed Description 
 
The Get-Role cmdlet gets a role or performs a search to retrieve multiple roles from Sitecore.

The Identity parameter specifies the Sitecore role to get. You can specify a role by its local name or fully qualified name.
You can also specify role object variable, such as $&lt;role&gt;.

To search for and retrieve more than one role, use the Filter parameter. 
 
- 
 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Identity&nbsp; &lt;AccountIdentity&gt; 
 
Specifies the Sitecore role by providing one of the following values.

    Local Name
        Example: developer
    Fully Qualified Name
        Example: sitecore\developer
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue) |
| Accept Wildcard Characters? | false | 
 
### -Filter&nbsp; &lt;String&gt; 
 
Specifies a simple pattern to match Sitecore roles.

Examples:
The following examples show how to use the filter syntax.

To get all the roles, use the asterisk wildcard:
Get-Role -Filter *

To get all the roles in a domain use the following command:
Get-Role -Filter "sitecore\*"
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
## Inputs 
 
The input type is the type of the objects that you can pipe to the cmdlet. 
 
* System.String
Represents the identity of a role. 
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* System.String
Represents the identity of a role. 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 1 
 
 
 
```powershell   
 
PS master:\> Get-Role -Identity developer

Name                                     Domain       IsEveryone
----                                     ------       ----------
sitecore\developer                       sitecore     False 
 
``` 
 
### EXAMPLE 2 
 
 
 
```powershell   
 
PS master:\> "developer","author" | Get-Role

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
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/> 
 
* Get-RoleMember

