# Export-UpdatePackage 
 
Saves a sitecore serialization diff list as a Sitecore Update Package. 
 
## Syntax 
 
Export-UpdatePackage [-CommandList] &lt;List`1&gt; [[-Name] &lt;String&gt;] [[-Path] &lt;String&gt;] [-Readme &lt;String&gt;] [-LicenseFileName &lt;String&gt;] [-Tag &lt;String&gt;] 
 
 
## Detailed Description 
 
Saves a sitecore serialization diff list as a Sitecore Update Package. 
 
- 
 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -CommandList&nbsp; &lt;List`1&gt; 
 
List of changes to be included in the package.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue) |
| Accept Wildcard Characters? | false | 
 
### -Name&nbsp; &lt;String&gt; 
 
Name of the package.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | 2 |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Path&nbsp; &lt;String&gt; 
 
Path the update package should be saved under.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | 3 |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Readme&nbsp; &lt;String&gt; 
 
Contents of the "read me" instruction for the package
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -LicenseFileName&nbsp; &lt;String&gt; 
 
file name of the license to be included with the package.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Tag&nbsp; &lt;String&gt; 
 
Package tag.
 

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
 
Create an update package that transforms the serialized database state defined in C:\temp\SerializationSource into into set defined in C:\temp\SerializationTarget 
 
```powershell   
 
$diff = Get-UpdatePackageDiff -SourcePath C:\temp\SerializationSource -TargetPath C:\temp\SerializationTarget
Export-UpdatePackage -Path C:\temp\SerializationDiff.update -CommandList $diff -Name name 
 
``` 
 
## Related Topics 
 
* Get-UpdatePackageDiff 
 
* Install-UpdatePackage 
 
* <a href='http://sitecoresnippets.blogspot.com/2012/10/sitecore-courier-effortless-packaging.html' target='_blank'>http://sitecoresnippets.blogspot.com/2012/10/sitecore-courier-effortless-packaging.html</a><br/> 
 
* <a href='https://github.com/adoprog/Sitecore-Courier' target='_blank'>https://github.com/adoprog/Sitecore-Courier</a><br/> 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>

