# Write-Log 
 
Writes text to the Sitecore event log. 
 
## Syntax 
 
Write-Log [[-Object] &lt;Object&gt;] [-Separator &lt;Object&gt;] [-Log &lt;Debug | Info | Warning | Error | Fatal | None&gt;] 
 
 
## Detailed Description 
 
The Write-Log cmdlet writes text to the Sitecore event log with the specified logging level. 
 
© 2010-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Object&nbsp; &lt;Object&gt; 
 
Specifies the object to write to the log.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue) |
| Accept Wildcard Characters? | false | 
 
### -Separator&nbsp; &lt;Object&gt; 
 
Strings the output together with the specified text.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Log&nbsp; &lt;LogNotificationLevel&gt; 
 
Specifies the Sitecore logging level.
 

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
 
### EXAMPLE 
 
 
 
```powershell   
 
PS master:\> Write-Log "Information." 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>

