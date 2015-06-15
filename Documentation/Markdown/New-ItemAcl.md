# New-ItemAcl 
 
Creates a new access rule for use with Set-ItemAcl and Add-ItemAcl cmdlets. 
 
## Syntax 
 
New-ItemAcl [-Identity] &lt;AccountIdentity&gt; [-PropagationType] &lt;Unknown | Descendants | Entity | Any&gt; [-SecurityPermission] &lt;NotSet | AllowAccess | DenyAccess | AllowInheritance | DenyInheritance&gt; 
 
 
## Detailed Description 
 
Creates a new access rule for use with Set-ItemAcl and Add-ItemAcl cmdlets. 
 
© 2010-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Identity&nbsp; &lt;AccountIdentity&gt; 
 
User name including domain for which the access rule is being created. If no domain is specified - 'sitecore' will be used as the default domain.

Specifies the Sitecore user by providing one of the following values.

    Local Name
        Example: adam
    Fully Qualified Name
        Example: sitecore\adam 
 
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
            <td>false</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
### -PropagationType&nbsp; &lt;PropagationType&gt; 
 
The PropagationType enumeration determines which items will be granted the access right.
 - Any - the item and all items inheriting
 - Descendants - applies rights to inheriting children only
 - Entity - applies right to the item only 
 
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
            <td>3</td>
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
 
### -SecurityPermission&nbsp; &lt;SecurityPermission&gt; 
 
The SecurityPermission determines whether to grant (allow) or deny the access right, and deny or allow inheritance of the right.
       - AllowAccess - 
       - DenyAccess - 
       - AllowInheritance - 
       - DenyInheritance - 
 
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
            <td>4</td>
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
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* Sitecore.Security.AccessControl.AccessRule 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 1 
 
Creates an access rule that allows the "sitecore\adam" user to delete the item to which it will be applied and all of its childre 
 
```powershell   
 
PS master:\> New-ItemAcl -AccessRight item:delete -PropagationType Any -SecurityPermission AllowAccess -Identity "sitecore\adam"

Account           AccessRight    PermissionType   PropagationType  SecurityPermission
-------           -----------    --------------   ---------------  ------------------
sitecore\admin    item:delete    Access           Any              AllowAccess 
 
``` 
 
### EXAMPLE 2 
 
Allows the "sitecore\adam" user to delete the Home item and all of its children.
Denies the "sitecore\mikey" user reading the descendants of the Home item. ;P
The security info is created prior to adding it to the item.
The item is delivered to the Add-ItemAcl from the pipeline and returned to the pipeline after processing due to the -PassThru parameter.
The security information is added to the previously existing security qualifiers. 
 
```powershell   
 
$acl1 = New-ItemAcl -AccessRight item:delete -PropagationType Any -SecurityPermission AllowAccess -Identity "sitecore\adam"
$acl2 = New-ItemAcl -AccessRight item:read -PropagationType Descendants -SecurityPermission DenyAccess -Identity "sitecore\mikey"
Get-Item -Path master:\content\home | Add-ItemAcl -AccessRules $acl1, $acl2 -PassThru

Name   Children Languages                Id                                     TemplateName
----   -------- ---------                --                                     ------------
Home   False    {en, ja-JP, de-DE, da}   {110D559F-DEA5-42EA-9C1C-8A5DF7E70EF9} Sample Item 
 
``` 
 
### EXAMPLE 3 
 
Allows the "sitecore\adam" user to delete the Home item and all of its children.
Denies the "sitecore\mikey" user reading the descendants of the Home item. ;P
The security info is created prior to setting it to the item.
The item is delivered to the Set-ItemAcl from the pipeline and returned to the pipeline after processing due to the -PassThru parameter.
Any previuous security information on the item is removed. 
 
```powershell   
 
$acl1 = New-ItemAcl -AccessRight item:delete -PropagationType Any -SecurityPermission AllowAccess -Identity "sitecore\adam"
$acl2 = New-ItemAcl -AccessRight item:read -PropagationType Descendants -SecurityPermission DenyAccess -Identity "sitecore\mikey"
Get-Item -Path master:\content\home | Set-ItemAcl -AccessRules $acl1, $acl2 -PassThru

Name   Children Languages                Id                                     TemplateName
----   -------- ---------                --                                     ------------
Home   False    {en, ja-JP, de-DE, da}   {110D559F-DEA5-42EA-9C1C-8A5DF7E70EF9} Sample Item 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>* Add-ItemAcl* Clear-ItemAcl* Get-ItemAcl* Set-ItemAcl* Test-ItemAcl* <a href='https://sdn.sitecore.net/upload/sitecore6/security_administrators_cookbook_a4.pdf' target='_blank'>https://sdn.sitecore.net/upload/sitecore6/security_administrators_cookbook_a4.pdf</a><br/>* <a href='https://sdn.sitecore.net/upload/sitecore6/61/security_reference-a4.pdf' target='_blank'>https://sdn.sitecore.net/upload/sitecore6/61/security_reference-a4.pdf</a><br/>* <a href='https://sdn.sitecore.net/upload/sitecore6/64/content_api_cookbook_sc64_and_later-a4.pdf' target='_blank'>https://sdn.sitecore.net/upload/sitecore6/64/content_api_cookbook_sc64_and_later-a4.pdf</a><br/>* <a href='http://www.sitecore.net/learn/blogs/technical-blogs/john-west-sitecore-blog/posts/2013/01/sitecore-security-access-rights.aspx' target='_blank'>http://www.sitecore.net/learn/blogs/technical-blogs/john-west-sitecore-blog/posts/2013/01/sitecore-security-access-rights.aspx</a><br/>* <a href='https://briancaos.wordpress.com/2009/10/02/assigning-security-to-items-in-sitecore-6-programatically/' target='_blank'>https://briancaos.wordpress.com/2009/10/02/assigning-security-to-items-in-sitecore-6-programatically/</a><br/>
