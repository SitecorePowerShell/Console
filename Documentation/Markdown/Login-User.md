# Login-User 
 
Logs a user in and performs further script instructions in the context of the user. 
 
## Syntax 
 
Login-User [-Identity] &lt;GenericIdentity&gt; [-Password] &lt;String&gt; 
 
 
## Detailed Description 
 
Logs a user in and performs further script instructions in the context of the user. 
 
- 
 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Identity&nbsp; &lt;GenericIdentity&gt; 
 
User name including domain. If no domain is specified - 'sitecore' will be used as the default value
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Password&nbsp; &lt;String&gt; 
 
Password for the account provided using the -Identity parameter.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 2 |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 
 
 
 
```powershell   
 
PS master:\> Login-User -Identity "sitecore\admin" -Password "b" 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>

