# Remove-RoleMember 
 
Removes one or more Sitecore users from the specified role. 
 
## Syntax 
 
Remove-RoleMember [-Identity] &lt;AccountIdentity&gt; -Members &lt;AccountIdentity[]&gt; 
 
 
## Detailed Description 
 
The Remove-RoleMember cmdlet gets a role and removes members of the Sitecore role.

The Identity parameter specifies the Sitecore role to get. You can specify a role by its local name or fully qualified name. 
 
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
 
### -Members&nbsp; &lt;AccountIdentity[]&gt; 
 

 

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
 
### EXAMPLE 
 
 
 
```powershell   
 
PS master:\> Remove-RoleMember -Identity developer -Members "michael","adam","mike" 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/> 
 
* Add-RoleMember 
 
* Get-RoleMember

