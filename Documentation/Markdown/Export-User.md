# Export-User 
 
Export-User. 
 
## Syntax 
 
Export-User [-Identity] &lt;AccountIdentity&gt; [-Root &lt;String&gt;] 
 
Export-User [-Identity] &lt;AccountIdentity&gt; -Path &lt;String&gt; 
 
Export-User -Filter &lt;String&gt; [-Root &lt;String&gt;] 
 
Export-User [-User] &lt;User&gt; [-Root &lt;String&gt;] 
 
Export-User [-User] &lt;User&gt; -Path &lt;String&gt; 
 
Export-User -Current [-Root &lt;String&gt;] 
 
Export-User -Current -Path &lt;String&gt; 
 
 
## Detailed Description 
The Export-User cmdlet serializes a user to a disk drive on the Sitecore server.

The Identity parameter specifies the Sitecore user to get. You can specify a user by its local name or fully qualified name.
You can also specify user object variable, such as $&lt;user&gt;.

To search for and retrieve more than one user, use the Filter parameter.

You can also pipe a user from the Get-user commandlet. 
- 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Identity&nbsp; &lt;AccountIdentity&gt; 
 
Specifies the Sitecore user by providing one of the following values.

    Local Name
        Example: admin
    Fully Qualified Name
        Example: sitecore\admi
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue) |
| Accept Wildcard Characters? | false | 
 
### -Filter&nbsp; &lt;String&gt; 
 
Specifies a simple pattern to match Sitecore users.

Examples:
The following examples show how to use the filter syntax.

To get all the users, use the asterisk wildcard:
Export-User -Filter *

To get all the users in a domain use the following command:
Export-User -Filter "sitecore\*"
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -User&nbsp; &lt;User&gt; 
 
User object retrieved from the Sitecore API or using the Get-User commandlet.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue) |
| Accept Wildcard Characters? | false | 
 
### -Current&nbsp; &lt;SwitchParameter&gt; 
 
Specifies that the current user should be serialized.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Path&nbsp; &lt;String&gt; 
 
Path to the file the user should be saved to.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Root&nbsp; &lt;String&gt; 
 
Overrides Sitecore Serialization root directory
 

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
 
* Sitecore.Security.Accounts.User 
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* Sitecore.Security.Accounts.User 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 
 
 
 
```powershell   
 
PS master:\> Export-User -Identify sitecore\admin 
 
``` 
 
## Related Topics 
 
* Export-Role 
 
* Import-User 
 
* Export-Item 
 
* Import-Role 
 
* Import-Item 
 
* Get-User 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>

