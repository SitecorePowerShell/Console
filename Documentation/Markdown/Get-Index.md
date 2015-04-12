# Get-Index 
 
Returns Sitecore database indices. 
 
## Syntax 
 
Get-Index [[-Database] &lt;Database&gt;] [[-Name] &lt;String&gt;] 
 
 
## Detailed Description 
 
Returns Sitecore indices in context of a particular database. 
 
- 
 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Database&nbsp; &lt;Database&gt; 
 
Database for which the indices should be retrieved.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | 2 |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue) |
| Accept Wildcard Characters? | false | 
 
### -Name&nbsp; &lt;String&gt; 
 
Name of the index to retrieve.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
## Inputs 
 
The input type is the type of the objects that you can pipe to the cmdlet. 
 
* Sitecore.Data.Database 
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* Sitecore.Data.Database 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 
 
 
 
```powershell   
 
PS master:\> Get-Index -Database "master" 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>

