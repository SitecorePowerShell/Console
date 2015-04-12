# Remove-ItemLanguage 
 
Removes Language from a single item or a branch of items 
 
## Syntax 
 
Remove-ItemLanguage -Language &lt;String[]&gt; [-ExcludeLanguage &lt;String[]&gt;] -Id &lt;String&gt; [-Database &lt;Database&gt;] [-Recurse] 
 
Remove-ItemLanguage -Language &lt;String[]&gt; [-ExcludeLanguage &lt;String[]&gt;] [-Item] &lt;Item&gt; [-Recurse] 
 
Remove-ItemLanguage -Language &lt;String[]&gt; [-ExcludeLanguage &lt;String[]&gt;] [-Path] &lt;String&gt; [-Recurse] 
 
 
## Detailed Description 
 
Removes Language version from a an Item either sent from pipeline or defined with Path or ID. A single language or a list of languages can be defined using the Language parameter. 
Language  parameter supports globbing so you can delete whole language groups using wildcards. 
 
© 2010-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Recurse&nbsp; &lt;SwitchParameter&gt; 
 
Deleted language versions from the item and all of its children.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Language&nbsp; &lt;String[]&gt; 
 
Language(s) that should be deleted form the provided item(s).
A single language or a list of languages can be defined using the parameter. 
Language parameter supports globbing so you can delete whole language groups using wildcards.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -ExcludeLanguage&nbsp; &lt;String[]&gt; 
 
Language(s) that should NOT be deleted form the provided item(s).
A single language or a list of languages can be defined using the parameter. 
Language parameter supports globbing so you can delete whole language groups using wildcards.

If Language parameter is not is not specified but ExcludeLanguage is provided, the default value of "*" is assumed for Language parameter.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Item&nbsp; &lt;Item&gt; 
 
The item to be processed.
 

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
 
Id of the item to be processed - can work with Language parameter to narrow the publication scope.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Database&nbsp; &lt;Database&gt; 
 
Database containing the item to be processed - can work with Language parameter to narrow the publication scope.
 

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
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 1 
 
Remove Polish and Spanish language from /sitecore/content/home item in the master database 
 
```powershell   
 
PS master:\> Remove-ItemLanguage -Path master:\content\home -Language "pl-pl", "es-es" 
 
``` 
 
### EXAMPLE 2 
 
Remove all english based languages defined in /sitecore/content/home item and all of its children in the master database 
 
```powershell   
 
PS master:\> Remove-ItemLanguage -Path master:\content\home -Language "en-*" -Recurse 
 
``` 
 
### EXAMPLE 3 
 
Remove all languages except those that are "en" based defined in /sitecore/content/home item and all of its children in the master database 
 
```powershell   
 
PS master:\> Remove-ItemLanguage -Path master:\content\home -ExcludeLanguage "en*" -Recurse 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/> 
 
* Add-ItemLanguage 
 
* Remove-Item

