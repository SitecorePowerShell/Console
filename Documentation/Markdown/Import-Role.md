# Import-Role 
 
Imports (deserializes) Sitecore roles from the Sitecore server disk drive. 
 
## Syntax 
 
Import-Role [-Identity] &lt;AccountIdentity&gt; [-Root &lt;String&gt;] 
 
Import-Role -Filter &lt;String&gt; [-Root &lt;String&gt;] 
 
Import-Role [-Role] &lt;User&gt; [-Root &lt;String&gt;] 
 
Import-Role -Path &lt;String&gt; 
 
 
## Detailed Description 
 
Imports (deserializes) Sitecore roles from the Sitecore server disk drive. 
 
© 2010-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Identity&nbsp; &lt;AccountIdentity&gt; 
 
Specifies the Sitecore role to be deserialized by providing one of the following values.

    Local Name
        Example: developer
    Fully Qualified Name
        Example: sitecore\developer 
 
<table>
    <thead></thead>
    <tbody>
        <tr>
            <td>Aliases</td>
            <td></td>
        </tr>
        <tr>
            <td>Required?</td>
            <td>true</td>
        </tr>
        <tr>
            <td>Position?</td>
            <td>1</td>
        </tr>
        <tr>
            <td>Default Value</td>
            <td></td>
        </tr>
        <tr>
            <td>Accept Pipeline Input?</td>
            <td>true (ByValue)</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
### -Filter&nbsp; &lt;String&gt; 
 
Specifies a simple pattern to match Sitecore roles.

Examples:
The following examples show how to use the filter syntax.

To get all the roles, use the asterisk wildcard:
Import-Role -Filter *

To get all the roles in a domain use the following command:
Import-Role -Filter "sitecore\*" 
 
<table>
    <thead></thead>
    <tbody>
        <tr>
            <td>Aliases</td>
            <td></td>
        </tr>
        <tr>
            <td>Required?</td>
            <td>true</td>
        </tr>
        <tr>
            <td>Position?</td>
            <td>named</td>
        </tr>
        <tr>
            <td>Default Value</td>
            <td></td>
        </tr>
        <tr>
            <td>Accept Pipeline Input?</td>
            <td>false</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
### -Role&nbsp; &lt;User&gt; 
 
An existing role object to be restored to the version from disk 
 
<table>
    <thead></thead>
    <tbody>
        <tr>
            <td>Aliases</td>
            <td></td>
        </tr>
        <tr>
            <td>Required?</td>
            <td>true</td>
        </tr>
        <tr>
            <td>Position?</td>
            <td>1</td>
        </tr>
        <tr>
            <td>Default Value</td>
            <td></td>
        </tr>
        <tr>
            <td>Accept Pipeline Input?</td>
            <td>true (ByValue)</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
### -Path&nbsp; &lt;String&gt; 
 
Path to the file the role should be loaded from. 
 
<table>
    <thead></thead>
    <tbody>
        <tr>
            <td>Aliases</td>
            <td></td>
        </tr>
        <tr>
            <td>Required?</td>
            <td>true</td>
        </tr>
        <tr>
            <td>Position?</td>
            <td>named</td>
        </tr>
        <tr>
            <td>Default Value</td>
            <td></td>
        </tr>
        <tr>
            <td>Accept Pipeline Input?</td>
            <td>false</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
### -Root&nbsp; &lt;String&gt; 
 
Specifies the serialization root directory. If this parameter is not specified - the default Sitecore serialization folder will be used (unless you're reading from an explicit location with the -Path parameter). 
 
<table>
    <thead></thead>
    <tbody>
        <tr>
            <td>Aliases</td>
            <td></td>
        </tr>
        <tr>
            <td>Required?</td>
            <td>false</td>
        </tr>
        <tr>
            <td>Position?</td>
            <td>named</td>
        </tr>
        <tr>
            <td>Default Value</td>
            <td></td>
        </tr>
        <tr>
            <td>Accept Pipeline Input?</td>
            <td>false</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
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
 
PS master:\> Import-Role -Identity sitecore\Author 
 
``` 
 
### EXAMPLE 2 
 
 
 
```powershell   
 
PS master:\> Import-Role -Filter sitecore\* 
 
``` 
 
### EXAMPLE 3 
 
 
 
```powershell   
 
PS master:\> Import-Role -Root C:\my\Serialization\Folder\ -Filter *\* 
 
``` 
 
### EXAMPLE 4 
 
 
 
```powershell   
 
PS master:\> Import-Role -Path C:\my\Serialization\Folder\Admins.role 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>


