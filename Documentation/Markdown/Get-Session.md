# Get-Session 
 
Gets one or more Sitecore user sessions. 
 
## Syntax 
 
Get-Session [-Identity &lt;AccountIdentity&gt;] 
 
Get-Session -InstanceId &lt;String[]&gt; 
 
 
## Detailed Description 
 
The Get-Session cmdlet gets user sessions in Sitecore.

The Identity parameter specifies the Sitecore user to get. You can specify a user by its local name or fully qualified name. 
 
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
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
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
 
## Inputs 
 
The input type is the type of the objects that you can pipe to the cmdlet. 
 
* None. 
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* None. 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 1 
 
 
 
```powershell   
 
PS master:\> Get-Session

Created                LastRequest            SessionID                  UserName
-------                -----------            ---------                  --------
7/3/2014 3:30:39 PM    7/3/2014 3:44:27 PM    tekipna1lk0ccr2z1bdjsua2   sitecore\admin
7/3/2014 4:13:55 PM    7/3/2014 4:13:55 PM    wq4bfivfm2tbgkgdccpyzczp   sitecore\michael 
 
``` 
 
### EXAMPLE 2 
 
 
 
```powershell   
 
PS master:\> Get-Session -Identity admin

Created                LastRequest            SessionID                  UserName
-------                -----------            ---------                  --------
7/3/2014 3:30:39 PM    7/3/2014 3:44:27 PM    tekipna1lk0ccr2z1bdjsua2   sitecore\admin 
 
``` 
 
### EXAMPLE 3 
 
 
 
```powershell   
 
PS master:\> Get-Session -InstanceId tekipna1lk0ccr2z1bdjsua2,wq4bfivfm2tbgkgdccpyzczp

Created                LastRequest            SessionID                  UserName
-------                -----------            ---------                  --------
7/3/2014 3:30:39 PM    7/3/2014 3:44:27 PM    tekipna1lk0ccr2z1bdjsua2   sitecore\admin 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/> 
 
* Remove-Session

