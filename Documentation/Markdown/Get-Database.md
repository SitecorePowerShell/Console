# Get-Database 
 
Retrieve Sitecore Database. 
 
## Syntax 
 
Get-Database [[-Name] &lt;String&gt;] [-Item &lt;Item&gt;] 
 
 
## Detailed Description 
Retrieves Sitecore Database object based on name or item passed to it. 
- 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Name&nbsp; &lt;String&gt; 
 
Name of the database to be returned.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue) |
| Accept Wildcard Characters? | false | 
 
### -Item&nbsp; &lt;Item&gt; 
 
Database returned will be taken from the item passed to the commandlet.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue) |
| Accept Wildcard Characters? | false | 
 
## Inputs 
 
The input type is the type of the objects that you can pipe to the cmdlet. 
 
* Sitecore.Data.Items.Item
System.String 
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* Sitecore.Data.Items.Item
System.String 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 1 
 
 
 
```powershell   
 
PS master:\> Get-Database
Name                 Languages                      Protected  Read Only
----                 ---------                      ---------  ---------
core                 {da, pl-PL, ja-JP, en...}      False      False
master               {en, de-DE, es-ES, pt-BR...}   False      False
web                  {es-ES, de-DE, pt-BR, pl-PL... False      False
filesystem           {en, en-US}                    False      True 
 
``` 
 
### EXAMPLE 2 
 
 
 
```powershell   
 
PS master:\> Get-Database -Name "master"

Name                 Languages                      Protected  Read Only
----                 ---------                      ---------  ---------
master               {en, de-DE, es-ES, pt-BR...}   False      False 
 
``` 
 
### EXAMPLE 3 
 
 
 
```powershell   
 
PS master:\> Get-Item . | Get-Database

Name                 Languages                      Protected  Read Only
----                 ---------                      ---------  ---------
master               {en, de-DE, es-ES, pt-BR...}   False      False 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>

