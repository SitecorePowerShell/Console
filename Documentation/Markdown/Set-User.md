# Set-User 
 
Sets the Sitecore user properties. 
 
## Syntax 
 
Set-User [-Identity] &lt;AccountIdentity&gt; [-Email &lt;String&gt;] [-FullName &lt;String&gt;] [-Comment &lt;String&gt;] [-ProfileItemId &lt;ID&gt;] [-StartUrl &lt;String&gt;] [-Enabled] [-CustomProperties &lt;Hashtable&gt;] 
 
Set-User -Instance &lt;User&gt; [-Email &lt;String&gt;] [-FullName &lt;String&gt;] [-Comment &lt;String&gt;] [-ProfileItemId &lt;ID&gt;] [-StartUrl &lt;String&gt;] [-Enabled] [-CustomProperties &lt;Hashtable&gt;] 
 
 
## Detailed Description 
The Set-User cmdlet sets a user profile properties in Sitecore.

The Identity parameter specifies the Sitecore user to set. You can specify a user by its local name or fully qualified name. 
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
 
### -Instance&nbsp; &lt;User&gt; 
 

 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue) |
| Accept Wildcard Characters? | false | 
 
### -Email&nbsp; &lt;String&gt; 
 
Specifies the Sitecore user email address. The value is validated for a properly formatted address.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -FullName&nbsp; &lt;String&gt; 
 

 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Comment&nbsp; &lt;String&gt; 
 

 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -ProfileItemId&nbsp; &lt;ID&gt; 
 

 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -StartUrl&nbsp; &lt;String&gt; 
 
Specifies the url to navigate to once the user is logged in. The values are validated with a pretermined set.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Enabled&nbsp; &lt;SwitchParameter&gt; 
 
Specifies whether the Sitecore user should be enabled.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -CustomProperties&nbsp; &lt;Hashtable&gt; 
 
Specifies a hashtable of custom properties to assign to the Sitecore user profile.
 

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
 
PS master:\> Set-User -Identity michael -Email michaellwest@gmail.com 
 
``` 
 
### EXAMPLE 2 
 
 
 
```powershell   
 
PS master:\> "michael","adam","mike" | Set-User -Enable $false 
 
``` 
 
### EXAMPLE 3 
 
 
 
```powershell   
 
PS master:\> Get-User -Filter * | Set-User -Comment "Sitecore user" 
 
``` 
 
### EXAMPLE 4 
 
 
 
```powershell   
 
PS master:\> Set-User -Identity michael -CustomProperties @{"Date"=(Get-Date)}
PS master:\>(Get-User michael).Profile.GetCustomProperty("Date")

7/3/2014 4:40:02 PM 
 
``` 
 
### EXAMPLE 5 
 
 
 
```powershell   
 
PS master:\> Set-User -Identity michael -IsAdministrator -CustomProperties @{"HireDate"="03/17/2010"}
PS master:\>$user = Get-User -Identity michael
PS master:\>$user.Profile.GetCustomProperty("HireDate")

03/17/2010 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/> 
 
* Get-User 
 
* New-User 
 
* Remove-User 
 
* Unlock-User

