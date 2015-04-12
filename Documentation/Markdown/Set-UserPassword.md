# Set-UserPassword 
 
Sets the Sitecore user password. 
 
## Syntax 
 
Set-UserPassword [-Identity] &lt;AccountIdentity&gt; -Reset [-NewPassword &lt;String&gt;] 
 
Set-UserPassword [-Identity] &lt;AccountIdentity&gt; -OldPassword &lt;String&gt; [-NewPassword &lt;String&gt;] 
 
 
## Detailed Description 
The Set-UserPassword cmdlet resets or changes a user password.

The Identity parameter specifies the Sitecore user to remove. You can specify a user by its local name or fully qualified name. 
- 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
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
 
### -NewPassword&nbsp; &lt;String&gt; 
 

 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | true (ByPropertyName) |
| Accept Wildcard Characters? | false | 
 
### -OldPassword&nbsp; &lt;String&gt; 
 

 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Reset&nbsp; &lt;SwitchParameter&gt; 
 

 

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
 
PS master:\> Set-UserPassword -Identity michael -NewPassword pass123 -OldPassword b 
 
``` 
 
### EXAMPLE 2 
 
 
 
```powershell   
 
PS master:\> "michael","adam","mike" | Set-UserPassword -NewPassword b -Reset 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/> 
 
* Get-User 
 
* Set-User

