# Remove-User 
 
Removes the Sitecore user. 
 
## Syntax 
 
Remove-User [-Identity] &lt;AccountIdentity&gt; 
 
Remove-User -Instance &lt;User&gt; 
 
 
## Detailed Description 
 
The Remove-User cmdlet removes a user from Sitecore.

The Identity parameter specifies the Sitecore user to remove. You can specify a user by its local name or fully qualified name. 
 
© 2010-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Identity&nbsp; &lt;AccountIdentity&gt; 
 
Specifies the Sitecore user by providing one of the following values.

    Local Name
        Example: admin
    Fully Qualified Name
        Example: sitecore\admi
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue) |
| Accept Wildcard Characters? | false | 
 
### -Instance&nbsp; &lt;User&gt; 
 

 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue) |
| Accept Wildcard Characters? | false | 
 
## Inputs 
 
The input type is the type of the objects that you can pipe to the cmdlet. 
 
* System.String
Represents the identity of a user.

Sitecore.Security.Accounts.User
Represents the instance of a user. 
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* System.String
Represents the identity of a user.

Sitecore.Security.Accounts.User
Represents the instance of a user. 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 1 
 
 
 
```powershell   
 
PS master:\> Remove-User -Identity michael 
 
``` 
 
### EXAMPLE 2 
 
 
 
```powershell   
 
PS master:\> "michael","adam","mike" | Remove-User 
 
``` 
 
### EXAMPLE 3 
 
 
 
```powershell   
 
PS master:\> Get-User -Filter sitecore\m* | Remove-User 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/> 
 
* Get-User 
 
* New-User 
 
* Set-User 
 
* Unlock-User

