# Remove-Session 
 
Removes one or more Sitecore user sessions. 
 
## Syntax 
 
Remove-Session -InstanceId &lt;String[]&gt; 
 
Remove-Session [-Instance] &lt;Session&gt; 
 
 
## Detailed Description 
 
The Remove-Session cmdlet removes user sessions in Sitecore. 
 
© 2010-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -InstanceId&nbsp; &lt;String[]&gt; 
 
Specifies the Sitecore SessionID.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Instance&nbsp; &lt;Session&gt; 
 
Specifies the Sitecore user sessions.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue) |
| Accept Wildcard Characters? | false | 
 
## Inputs 
 
The input type is the type of the objects that you can pipe to the cmdlet. 
 
* Sitecore.Web.Authentication.DomainAccessGuard.Session
Accepts a user session. 
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* Sitecore.Web.Authentication.DomainAccessGuard.Session
Accepts a user session. 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 1 
 
 
 
```powershell   
 
PS master:\> Remove-Session -InstanceId tekipna1lk0ccr2z1bdjsua2,wq4bfivfm2tbgkgdccpyzczp 
 
``` 
 
### EXAMPLE 2 
 
 
 
```powershell   
 
PS master:\> Get-Session -Identity michael | Remove-Session 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/> 
 
* Get-Session

