# Export-Item 
 
Serializes sitecore item to server disk drive. This command used to be named Serialize-Item - a matching alias added for compatibility with older scripts. 
 
## Syntax 
 
Export-Item [-Entry &lt;IncludeEntry&gt;] [-Recurse] [-ItemPathsAbsolute] [-Root &lt;String&gt;] 
 
Export-Item [-Item] &lt;Item&gt; [-Recurse] [-ItemPathsAbsolute] [-Root &lt;String&gt;] 
 
Export-Item [-Path] &lt;String&gt; [-Recurse] [-ItemPathsAbsolute] [-Root &lt;String&gt;] 
 
Export-Item -Id &lt;String&gt; [-Database &lt;Database&gt;] [-Recurse] [-ItemPathsAbsolute] [-Root &lt;String&gt;] 
 
 
## Detailed Description 
 
Serializes sitecore item to server disk drive.
The simplest command syntax is:
Export-Item -path "master:\content"

or

Get-Item "master:\content" | Export-Item

Both of them will serialize the content item in the master database. In first case we pass the path to the item as a parameter, in second case we serialize the items which come from the pipeline. 
You can send more items from the pipeline to the Export-Item command, e.g. if you need to serialize all the descendants of the home item created by sitecore\admin, you can use:

Get-Childitem "master:\content\home" -recurse | Where-Object { $_."__Created By" -eq "sitecore\admin" } | Export-Item 
 
© 2010-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions## Aliases
The following abbreviations are aliases for this cmdlet:  
* Serialize-Item 
 
## Parameters 
 
### -Entry&nbsp; &lt;IncludeEntry&gt; 
 
Serialization preset to be serialized. Obtain the preset through the use of Get-Preset commandlet
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue, ByPropertyName) |
| Accept Wildcard Characters? | false | 
 
### -Recurse&nbsp; &lt;SwitchParameter&gt; 
 
Process the item and all of its children - switch which decides if serialization concerns only the single item or the whole tree below the item, e.g.

Export-Item -path "master:\content\articles" -recurse

Root - directory where the serialized files should be saved, e.g.
Export-Item -path "master:\content" -Root "c:\tmp"
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -ItemPathsAbsolute&nbsp; &lt;SwitchParameter&gt; 
 
Works only with Root parameter and decides if folder structure starting from "sitecore\content" should be created, e.g. if you want to serialize articles item in directory c:\tmp\sitecore\content you can use. For example:
Export-Item -Path "master:\content\articles" -ItemPathsAbsolute -Root "c:\tmp"
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Root&nbsp; &lt;String&gt; 
 
Directory where the serialized files should be saved, e.g.

Export-Item -Path "master:\content" -Root "c:\tmp"
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Item&nbsp; &lt;Item&gt; 
 
The item to be serialized.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue, ByPropertyName) |
| Accept Wildcard Characters? | false | 
 
### -Path&nbsp; &lt;String&gt; 
 
Path to the item to be processed - can work with Language parameter to narrow the publication scope.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Id&nbsp; &lt;String&gt; 
 
You can pass the id of serialized item instead of path, e.g.
Export-Item -id "{0DE95AE4-41AB-4D01-9EB0-67441B7C2450}"
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Database&nbsp; &lt;Database&gt; 
 

 

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
 
* Sitecore.Data.Items.Item 
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* Sitecore.Data.Items.Item 
 
## Notes 
 
Help Author: Marek Musielak, Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 
 
 
 
```powershell   
 
PS master:\> Export-Item -Path master:\content\home 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/> 
 
* Get-Preset 
 
* Import-Item 
 
* <a href='http://www.cognifide.com/blogs/sitecore/serialization-and-deserialization-with-sitecore-powershell-extensions/' target='_blank'>http://www.cognifide.com/blogs/sitecore/serialization-and-deserialization-with-sitecore-powershell-extensions/</a><br/> 
 
* <a href='https://www.youtube.com/watch?v=60BGRDNONo0&list=PLph7ZchYd_nCypVZSNkudGwPFRqf1na0b&index=7' target='_blank'>https://www.youtube.com/watch?v=60BGRDNONo0&list=PLph7ZchYd_nCypVZSNkudGwPFRqf1na0b&index=7</a><br/> 
 
* <a href='https://gist.github.com/AdamNaj/6c86f61510dc3d2d8b2f' target='_blank'>https://gist.github.com/AdamNaj/6c86f61510dc3d2d8b2f</a><br/> 
 
* <a href='http://stackoverflow.com/questions/20266841/sitecore-powershell-deserialization' target='_blank'>http://stackoverflow.com/questions/20266841/sitecore-powershell-deserialization</a><br/> 
 
* <a href='http://stackoverflow.com/questions/20195718/sitecore-serialization-powershell' target='_blank'>http://stackoverflow.com/questions/20195718/sitecore-serialization-powershell</a><br/> 
 
* <a href='http://stackoverflow.com/questions/20283438/sitecore-powershell-deserialization-core-db' target='_blank'>http://stackoverflow.com/questions/20283438/sitecore-powershell-deserialization-core-db</a><br/>

