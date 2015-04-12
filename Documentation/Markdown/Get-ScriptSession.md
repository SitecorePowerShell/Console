# Get-ScriptSession 
 
Returns the list of PowerShell Extension Sessions running in background. 
 
## Syntax 
 
Get-ScriptSession -Id &lt;String&gt; 
 
Get-ScriptSession -Current 
 
Get-ScriptSession -Type &lt;String[]&gt; 
 
 
## Detailed Description 
 
Returns the list of PowerShell Extension Sessions running in background. 
 
- 
 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Id&nbsp; &lt;String&gt; 
 
Id of the session.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Current&nbsp; &lt;SwitchParameter&gt; 
 
Returns current script session if the session is run in a background job.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Type&nbsp; &lt;String[]&gt; 
 

 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 1 
 
 
 
```powershell   
 
PS master:\>Get-ScriptSession
 
Type         Key                                                                              Location                                 Auto Disposed
----         ---                                                                              --------                                 -------------
Console      $scriptSession$|zwlyrcmmzwisv22djsv0ej2a|8d5c3e63-3fed-0532-e7c5-761760567b83                                             False
Context      $scriptSession$|zwlyrcmmzwisv22djsv0ej2a|renderingCopySession                    master:\content\Home                     False
Context      $scriptSession$|zwlyrcmmzwisv22djsv0ej2a|packageBuilder                          master:\content\Home                     False 
 
``` 
 
### EXAMPLE 2 
 
 
 
```powershell   
 
PS master:\>Get-ScriptSession -Current
 
Type         Key                                                                              Location                                 Auto Disposed
----         ---                                                                              --------                                 -------------
Console      $scriptSession$|zwlyrcmmzwisv22djsv0ej2a|8d5c3e63-3fed-0532-e7c5-761760567b83                                             False 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>

