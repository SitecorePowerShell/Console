# Add-ItemLanguage 
 
Creates a version of the item in a new language based on a language version already existing. 
 
## Syntax 
 
Add-ItemLanguage [-Language &lt;String[]&gt;] -Id &lt;String&gt; [-Database &lt;Database&gt;] [-Recurse] [-IfExist &lt;Skip | Append | OverwriteLatest&gt;] -TargetLanguage &lt;String[]&gt; [-DoNotCopyFields] [-IgnoredFields &lt;String[]&gt;] 
 
Add-ItemLanguage [-Language &lt;String[]&gt;] [-Path] &lt;String&gt; [-Recurse] [-IfExist &lt;Skip | Append | OverwriteLatest&gt;] -TargetLanguage &lt;String[]&gt; [-DoNotCopyFields] [-IgnoredFields &lt;String[]&gt;] 
 
Add-ItemLanguage [-Language &lt;String[]&gt;] [-Item] &lt;Item&gt; [-Recurse] [-IfExist &lt;Skip | Append | OverwriteLatest&gt;] -TargetLanguage &lt;String[]&gt; [-DoNotCopyFields] [-IgnoredFields &lt;String[]&gt;] 
 
 
## Detailed Description 
 
Creates a version of the item in a new language based on a language version already existing. 
Based on parameters you can make the commandlet bahave differently when a version in the target language already exists and define which fields if any should be copied over from the original language. 
 
- 
 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Recurse&nbsp; &lt;SwitchParameter&gt; 
 
Process the item and all of its children.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -IfExist&nbsp; &lt;ActionIfExists&gt; 
 
Accepts one of 3 pretty self explanatory actions: 
- Skip - if language version exists don't do anything
- Append - if language version exists create a new version with values copied from the original language
- OverwriteLatest - if language version exists overwrite the last version with values copied from the original language
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -TargetLanguage&nbsp; &lt;String[]&gt; 
 
Language or a list of languages that should be created
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -DoNotCopyFields&nbsp; &lt;SwitchParameter&gt; 
 
Creates a version in the target language but does not copy field values from the original language
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -IgnoredFields&nbsp; &lt;String[]&gt; 
 
List of fields that should not be copied over from original item this can contain e.g. "__Security" if you don't want the new version to have the same restrictions as the original version.
On top of the ignored fields in the -IgnoredFields the following fields are ignored as configured within the Cognifide.PowerShell.config file in the following location:
configuration/sitecore/powershell/translation/ignoredFields.
Fields ignored out of the box include:__Archive date, __Archive Version date, __Lock, __Owner, __Page Level Test Set Definition, __Reminder date, __Reminder recipients, __Reminder text
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Language&nbsp; &lt;String[]&gt; 
 
If specified - language that will be used as source language.
 

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
 
Translate the Home Item from English to US and Polish leaving the "Title" field blank. If a version exists don't do anything 
 
```powershell   
 
PS master:\> Add-ItemLanguage -Path "master:\content\home" -Language "en" -TargetLanguage "pl-pl", "en-us" -IfExist Skip -IgnoredFields "Title" 
 
``` 
 
### EXAMPLE 2 
 
Translate the children of Home item (but only those of Template Name "Sample Item") from English to US and Polish. If a version exists create a new version for that language. Display results in a table listing item name, language and created version number. 
 
```powershell   
 
Get-ChildItem "master:\content\home" -Language "en" -Recurse | `
    Where-Object { $_.TemplateName -eq "Sample Item" } | `
    Add-ItemLanguage -TargetLanguage "pl-pl" -IfExist Append | `
    Format-Table Name, Language, Version -auto 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/> 
 
* Remove-ItemLanguage 
 
* New-Item

