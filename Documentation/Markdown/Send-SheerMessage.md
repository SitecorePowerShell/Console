# Send-SheerMessage 
 
Sends a sheer message to the app in which context the script is executing. 
 
## Syntax 
 
Send-SheerMessage [-Name] &lt;String&gt; [-GetResult] 
 
 
## Detailed Description 
 
Sends a sheer message to the app in which context the script is executing. 
 
© 2010-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Name&nbsp; &lt;String&gt; 
 
Name of the Sheer message to execute.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue) |
| Accept Wildcard Characters? | false | 
 
### -GetResult&nbsp; &lt;SwitchParameter&gt; 
 

 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 
 
If you execute the following script in the PowerShell ISE the Save dialog will popup 
 
```powershell   
 
Send-SheerMessage -Name 'ise:save' 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>

