# Get-SpeModule 
 
Retrieves the object that describes a Sitecore PowerShell Extensions Module 
 
## Syntax 
 
Get-SpeModule -Item &lt;Item&gt; 
 
Get-SpeModule -Path &lt;String&gt; 
 
Get-SpeModule -Id &lt;String&gt; -Database &lt;Database&gt; 
 
Get-SpeModule -Database &lt;Database&gt; 
 
Get-SpeModule [-Database &lt;Database&gt;] -Name &lt;String&gt; 
 
 
## Detailed Description 
 
Retrieves the object that describes a Sitecore PowerShell Extensions Module. 
 
- 
 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Item&nbsp; &lt;Item&gt; 
 
A script or library item that is defined within the module to be returned.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue, ByPropertyName) |
| Accept Wildcard Characters? | false | 
 
### -Path&nbsp; &lt;String&gt; 
 
Path to a script or library item that is defined within the module to be returned.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Id&nbsp; &lt;String&gt; 
 
Id of a script or library item that is defined within the module to be returned.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Database&nbsp; &lt;Database&gt; 
 
Database containing the module to be returned.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue, ByPropertyName) |
| Accept Wildcard Characters? | false | 
 
### -Name&nbsp; &lt;String&gt; 
 
Name fo the module to return. Supports wildcards.
 

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
 
Return all modules defined in the provided database 
 
```powershell   
 
PS master:\> Get-SpeModule -Database (Get-Database "master") 
 
``` 
 
### EXAMPLE 2 
 
Return all modules defined in the master database Matching the "Content*" wildcard 
 
```powershell   
 
PS master:\> Get-SpeModule -Database (Get-Database "master") 
 
``` 
 
### EXAMPLE 3 
 
Return the module the piped script belongs to 
 
```powershell   
 
PS master:\> Get-item "master:\system\Modules\PowerShell\Script Library\Copy Renderings\Content Editor\Context Menu\Layout\Copy Renderings" |  Get-SpeModule 
 
``` 
 
## Related Topics 
 
* Get-SpeModuleFeatureRoot 
 
* <a href='http://blog.najmanowicz.com/2014/11/01/sitecore-powershell-extensions-3-0-modules-proposal/' target='_blank'>http://blog.najmanowicz.com/2014/11/01/sitecore-powershell-extensions-3-0-modules-proposal/</a><br/> 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>

