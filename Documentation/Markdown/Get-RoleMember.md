# Get-RoleMember 
 
Gets the Sitecore users in the role. 
 
## Syntax 
 
Get-RoleMember [-Identity] &lt;AccountIdentity&gt; [-UsersOnly] [-Recursive] 
 
Get-RoleMember [-Identity] &lt;AccountIdentity&gt; [-Recursive] 
 
Get-RoleMember [-Identity] &lt;AccountIdentity&gt; [-RolesOnly] [-Recursive] 
 
 
## Detailed Description 
 
The Get-RoleMember cmdlet gets a role and returns the members of the Sitecore role.

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
 
### -Recursive&nbsp; &lt;SwitchParameter&gt; 
 

 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -UsersOnly&nbsp; &lt;SwitchParameter&gt; 
 

 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -RolesOnly&nbsp; &lt;SwitchParameter&gt; 
 

 

| | |
| - | - |
| Aliases |  |
| Required? | false |
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
 
PS master:\> Get-RoleMember -Identity developer

Name                     Domain       IsAdministrator IsAuthenticated
----                     ------       --------------- ---------------
sitecore\michael         sitecore     False           False 
 
``` 
 
### EXAMPLE 2 
 
 
 
```powershell   
 
PS master:\> Get-RoleMember -Identity author

Name                     Domain       IsAdministrator IsAuthenticated
----                     ------       --------------- ---------------
sitecore\michael         sitecore     False           False

Domain      : sitecore
IsEveryone  : False
IsGlobal    : False
AccountType : Role
Description : Role
DisplayName : sitecore\Developer
LocalName   : sitecore\Developer
Name        : sitecore\Developer 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/> 
 
* Get-Role 
 
* Remove-RoleMember 
 
* Add-RoleMember

