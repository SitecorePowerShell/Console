# Publish-Item 
 
Publishes a Sitecore item. 
 
## Syntax 
 
Publish-Item [-Language &lt;String[]&gt;] -Id &lt;String&gt; [-Database &lt;Database&gt;] [-Recurse] [-Target &lt;String[]&gt;] [-PublishMode &lt;Unknown | Full | Incremental | SingleItem | Smart&gt;] 
 
Publish-Item [-Language &lt;String[]&gt;] [-Path] &lt;String&gt; [-Recurse] [-Target &lt;String[]&gt;] [-PublishMode &lt;Unknown | Full | Incremental | SingleItem | Smart&gt;] 
 
Publish-Item [-Language &lt;String[]&gt;] [-Item] &lt;Item&gt; [-Recurse] [-Target &lt;String[]&gt;] [-PublishMode &lt;Unknown | Full | Incremental | SingleItem | Smart&gt;] 
 
 
## Detailed Description 
 
The Publish-Item cmdlet publishes the Sitecore item and optionally subitems. Allowing for granular control over languages and modes of publishing. 
 
© 2010-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Recurse&nbsp; &lt;SwitchParameter&gt; 
 
Specifies that subitems should also get published with the root item.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Target&nbsp; &lt;String[]&gt; 
 
Specifies the publishing targets. The default target database is "web".
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -PublishMode&nbsp; &lt;PublishMode&gt; 
 
Specified the Publish mode. Valid values are: 
- Full
- Incremental
- SingleItem
- Smart
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Language&nbsp; &lt;String[]&gt; 
 
Language of the item that should be published. Supports globbing/wildcards.
Allows for more than one language to be provided at once. e.g. "en*", "pl-pl"
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Item&nbsp; &lt;Item&gt; 
 

 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue, ByPropertyName) |
| Accept Wildcard Characters? | false | 
 
### -Path&nbsp; &lt;String&gt; 
 
Path to the item that should be published - can work with Language parameter to narrow the publication scope.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Id&nbsp; &lt;String&gt; 
 
Id of the item that should be published - can work with Language parameter to narrow the publication scope.
 

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
 
Help Author: Michael West, Adam Najmanowicz 
 
## Examples 
 
### EXAMPLE 1 
 
 
 
```powershell   
 
PS master:\> Publish-Item -Path master:\content\home -Target Internet 
 
``` 
 
### EXAMPLE 2 
 
 
 
```powershell   
 
PS master:\> Get-Item -Path master:\content\home | Publish-Item -Recurse -PublishMode Incremental 
 
``` 
 
### EXAMPLE 3 
 
 
 
```powershell   
 
PS master:\> Get-Item -Path master:\content\home | Publish-Item -Recurse -Language "en*" 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>

