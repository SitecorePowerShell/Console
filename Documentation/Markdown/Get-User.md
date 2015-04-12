# Get-User 
 
Gets one or more Sitecore users. 
 
## Syntax 
 
Get-User [-Identity] &lt;AccountIdentity&gt; [-Authenticated] 
 
Get-User -Filter &lt;String&gt; 
 
Get-User -Current 
 
 
## Detailed Description 
 
The Get-User cmdlet gets a user or performs a search to retrieve multiple users from Sitecore.

The Identity parameter specifies the Sitecore user to get. You can specify a user by its local name or fully qualified name.
You can also specify user object variable, such as $&lt;user&gt;.

To search for and retrieve more than one user, use the Filter parameter. 
 
© 2010-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Identity&nbsp; &lt;AccountIdentity&gt; 
 
Specifies the Sitecore user by providing one of the following values.

Local Name:
  
    admin

Fully Qualified Name:

    sitecore\admi
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue) |
| Accept Wildcard Characters? | false | 
 
### -Filter&nbsp; &lt;String&gt; 
 
Specifies a simple pattern to match Sitecore users.

Examples:
The following examples show how to use the filter syntax.

To get all the users, use the asterisk wildcard:  

    Get-User -Filter *

To get all the users in a domain use the following command:  

    Get-User -Filter "sitecore\*"
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Current&nbsp; &lt;SwitchParameter&gt; 
 

 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Authenticated&nbsp; &lt;SwitchParameter&gt; 
 

 

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
Represents the identity of a user. 
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* System.String
Represents the identity of a user. 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 1 
 
 
 
```powershell   
 
PS master:\> Get-User -Identity admin

Name                     Domain       IsAdministrator IsAuthenticated
----                     ------       --------------- ---------------
sitecore\admin           sitecore     True            False 
 
``` 
 
### EXAMPLE 2 
 
 
 
```powershell   
 
PS master:\> "admin","michael" | Get-User

Name                     Domain       IsAdministrator IsAuthenticated
----                     ------       --------------- ---------------
sitecore\Admin           sitecore     True            False
sitecore\michael         sitecore     False           False 
 
``` 
 
### EXAMPLE 3 
 
 
 
```powershell   
 
PS master:\> Get-User -Filter *

Name                     Domain       IsAdministrator IsAuthenticated
----                     ------       --------------- ---------------
default\Anonymous        default      False           False
extranet\Anonymous       extranet     False           False
sitecore\Admin           sitecore     True            False
sitecore\michael         sitecore     False           False 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/> 
 
* Set-User 
 
* New-User 
 
* Remove-User 
 
* Unlock-User

