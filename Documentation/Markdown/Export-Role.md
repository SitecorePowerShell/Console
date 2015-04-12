# Export-Role 
 
Exports (serializes) Sitecore roles to server disk drive. 
 
## Syntax 
 
Export-Role [-Identity] &lt;AccountIdentity&gt; [-Root &lt;String&gt;] 
 
Export-Role [-Identity] &lt;AccountIdentity&gt; -Path &lt;String&gt; 
 
Export-Role -Filter &lt;String&gt; [-Root &lt;String&gt;] 
 
Export-Role [-Role] &lt;Role&gt; -Path &lt;String&gt; 
 
Export-Role [-Role] &lt;Role&gt; [-Root &lt;String&gt;] 
 
 
## Detailed Description 
 
Exports (serializes) Sitecore roles to server disk drive. 
 
- 
 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Identity&nbsp; &lt;AccountIdentity&gt; 
 
Specifies the Sitecore role by providing one of the following values.

    Local Name
        Example: developer
    Fully Qualified Name
        Example: sitecore\developer
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue) |
| Accept Wildcard Characters? | false | 
 
### -Filter&nbsp; &lt;String&gt; 
 
Specifies a simple pattern to match Sitecore roles.

Examples:
The following examples show how to use the filter syntax.

To get all the roles, use the asterisk wildcard:
Export-Role -Filter *

To get all the roles in a domain use the following command:
Export-Role -Filter "sitecore\*"
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Role&nbsp; &lt;Role&gt; 
 
Specifies the role to be exported
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue) |
| Accept Wildcard Characters? | false | 
 
### -Path&nbsp; &lt;String&gt; 
 
Path to the file the role should be saved to.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Root&nbsp; &lt;String&gt; 
 
Specifies the serialization root directory. If this parameter is not specified - the default Sitecore serialization folder will be used (unless you're saving to an explicit location with the -Path parameter).
 

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
Sitecore.Security.Accounts.Role 
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* System.String
Sitecore.Security.Accounts.Role 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 1 
 
 
 
```powershell   
 
PS master:\> Export-Role -Identity sitecore\Author 
 
``` 
 
### EXAMPLE 2 
 
 
 
```powershell   
 
PS master:\> Export-Role -Filter sitecore\* 
 
``` 
 
### EXAMPLE 3 
 
 
 
```powershell   
 
PS master:\> Export-Role -Root C:\my\Serialization\Folder\ -Filter *\* 
 
``` 
 
### EXAMPLE 4 
 
 
 
```powershell   
 
PS master:\> Export-Role -Path C:\my\Serialization\Folder\Authors.role -Identity sitecore\Author 
 
``` 
 
## Related Topics 
 
* Import-Role 
 
* Export-User 
 
* Import-User 
 
* Export-Item 
 
* Import-Item 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>

