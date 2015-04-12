# New-User 
 
Creates a new Sitecore user. 
 
## Syntax 
 
New-User [-Identity] &lt;AccountIdentity&gt; [-Password &lt;String&gt;] [-Email &lt;String&gt;] [-FullName &lt;String&gt;] [-Comment &lt;String&gt;] [-Portrait &lt;String&gt;] [-Enabled] [-PassThru] 
 
 
## Detailed Description 
 
The New-User cmdlet creates a new user in Sitecore.

The Identity parameter specifies the Sitecore user to create. You can specify a user by its local name or fully qualified name. 
 
- 
 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Identity&nbsp; &lt;AccountIdentity&gt; 
 
Specifies the Sitecore user by providing one of the following values.

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
| Accept Pipeline Input? | true (ByPropertyName) |
| Accept Wildcard Characters? | false | 
 
### -Password&nbsp; &lt;String&gt; 
 

 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | true (ByPropertyName) |
| Accept Wildcard Characters? | false | 
 
### -Email&nbsp; &lt;String&gt; 
 

 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | true (ByPropertyName) |
| Accept Wildcard Characters? | false | 
 
### -FullName&nbsp; &lt;String&gt; 
 

 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | true (ByPropertyName) |
| Accept Wildcard Characters? | false | 
 
### -Comment&nbsp; &lt;String&gt; 
 

 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | true (ByPropertyName) |
| Accept Wildcard Characters? | false | 
 
### -Portrait&nbsp; &lt;String&gt; 
 

 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | true (ByPropertyName) |
| Accept Wildcard Characters? | false | 
 
### -Enabled&nbsp; &lt;SwitchParameter&gt; 
 
Specifies that the account should be enabled. When enabled, the Password parameter is required.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | true (ByPropertyName) |
| Accept Wildcard Characters? | false | 
 
### -PassThru&nbsp; &lt;SwitchParameter&gt; 
 

 

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
 
PS master:\> New-User -Identity michael 
 
``` 
 
### EXAMPLE 2 
 
 
 
```powershell   
 
PS master:\> New-User -Identity michael -Enabled -Password b -Email michaellwest@gmail.com -FullName "Michael West" 
 
``` 
 
### EXAMPLE 3 
 
 
 
```powershell   
 
PS master:\> New-User -Identity michael -PassThru

Name                     Domain       IsAdministrator IsAuthenticated
----                     ------       --------------- ---------------
sitecore\michael2        sitecore     False           False 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/> 
 
* Get-User 
 
* Set-User 
 
* Remove-User 
 
* Unlock-User

