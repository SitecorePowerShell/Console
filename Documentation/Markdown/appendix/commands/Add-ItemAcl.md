# Add-ItemAcl 
 
Adds new access rule to an item allowing for the item to have the access granted or denied for a provided role or user. 
 
## Syntax 
 
Add-ItemAcl -PropagationType &lt;Unknown | Descendants | Entity | Any&gt; -SecurityPermission &lt;NotSet | AllowAccess | DenyAccess | AllowInheritance | DenyInheritance&gt; -AccessRight &lt;String&gt; -Identity &lt;AccountIdentity&gt; -Path &lt;String&gt; [-PassThru] 
 
Add-ItemAcl -PropagationType &lt;Unknown | Descendants | Entity | Any&gt; -SecurityPermission &lt;NotSet | AllowAccess | DenyAccess | AllowInheritance | DenyInheritance&gt; -AccessRight &lt;String&gt; -Identity &lt;AccountIdentity&gt; -Id &lt;String&gt; [-Database &lt;String&gt;] [-PassThru] 
 
Add-ItemAcl -PropagationType &lt;Unknown | Descendants | Entity | Any&gt; -SecurityPermission &lt;NotSet | AllowAccess | DenyAccess | AllowInheritance | DenyInheritance&gt; -AccessRight &lt;String&gt; -Identity &lt;AccountIdentity&gt; -Item &lt;Item&gt; [-PassThru] 
 
Add-ItemAcl -AccessRules &lt;AccessRuleCollection&gt; -Path &lt;String&gt; [-PassThru] 
 
Add-ItemAcl -AccessRules &lt;AccessRuleCollection&gt; -Id &lt;String&gt; [-PassThru] 
 
Add-ItemAcl -AccessRules &lt;AccessRuleCollection&gt; -Item &lt;Item&gt; [-PassThru] 
 
Add-ItemAcl -Item &lt;Item&gt; [-PassThru] 
 
Add-ItemAcl -Path &lt;String&gt; [-PassThru] 
 
Add-ItemAcl -Id &lt;String&gt; [-Database &lt;String&gt;] [-PassThru] 
 
 
## Detailed Description 
 
Adds new access rule to an item allowing for the item to have the access granted or denied for a provided role or user.
The new rules will be appended to the existing security descriptors on the item. 
 
© 2010-2017 Adam Najmanowicz, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
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
 
### -AccessRules&nbsp; &lt;AccessRuleCollection&gt; 
 
A single or multiple access rules created e.g. through the New-ItemAcl or obtained from other item using the Get-ItemAcl cmdlet.
This information will be appended to the existing security descriptors on the item. 
 
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
 
### -PassThru&nbsp; &lt;SwitchParameter&gt; 
 
 
 
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
 
### -AccessRight&nbsp; &lt;String&gt; 
 
The access right to grand or deny.
Well known rights are:
- field:read - "Field Read" - controls whether an account can read a specific field on an item..
- field:write - "Field Write" - controls whether an account can update a specific field on an item.

- item:read - "Read" - controls whether an account can see an item in the content tree and/or on the published Web site, including all of its properties and field values.
- item:write - "Write" - controls whether an account can update field values. The write access right requires the read access right and field read and field write access rights for individual fields (field read and field write are allowed by default).
- item:rename - "Rename" - controls whether an account can change the name of an item. The rename access right requires the read access right.
- item:create - "Create" - controls whether an account can create child items. The create access right requires the read access right.
- item:delete - "Delete" - Delete right for items. controls whether an account can delete an item. The delete access right requires the read access right
	Important!
The Delete command also deletes all child items, even if the account has been denied Delete
rights for one or more of the subitems. 
- item:admin - "Administer" - controls whether an account can configure access rights on an item. The administer access right requires the read and write access rights.
- language:read - "Language Read" - controls whether a user can read a specific language version of items.
- language:write - "Language Write" - controls whether a user can update a specific language version of items.
- site:enter - controls whether a user can access a specific site.
- insert:show - "Show in Insert" - Determines if the user can see the insert option
- workflowState:delete - "Workflow State Delete" - controls whether a user can delete items which are currently associated with a specific workflow state.
- workflowState:write - "Workflow State Write" - controls whether a user can update items which are currently associated with a specific workflow state.
- workflowCommand:execute - "Workflow Command Execute" - — controls whether a user is shown specific workflow commands.
- profile:customize - "Customize Profile Key Values" - The right to input out of range values of profile keys, that belong to this profile.
- bucket:makebucket - "Create Bucket" - convert item to bucket.
- bucket:unmake - "Revert Bucket" - convert item back from bucket.
- remote:fieldread - "Field Remote Read" - Field Read right for remoted clients. 
 
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
 
### -Item&nbsp; &lt;Item&gt; 
 
The item to be processed. 
 
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
            <td>true (ByValue)</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
### -Path&nbsp; &lt;String&gt; 
 
Path to the item to be processed. 
 
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
 
### -Id&nbsp; &lt;String&gt; 
 
Id of the item to be processed. Requires Database parameter to be specified 
 
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
 
### -Database&nbsp; &lt;String&gt; 
 
Database containing the item to be fetched using the Id Parameter. 
 
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
 
* can be piped from another cmdlet* Sitecore.Data.Items.Item 
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* Only if -PassThru is used* Sitecore.Data.Items.Item 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 1 
 
allows the "sitecore\adam" user to rename the Home item and all of its childre 
 
```powershell   
 
PS master:\> Add-ItemAcl -Path master:\content\Home -AccessRight "item:rename" -PropagationType Any -SecurityPermission AllowAccess -Identity "sitecore\admin" 
 
``` 
 
### EXAMPLE 2 
 
Allows the "sitecore\adam" user to delete the Home item and all of its children.
Denies the "sitecore\mikey" user reading the descendants of the Home item. ;P
The security info is created prior to adding it to the item.
The item is delivered to the Add-ItemAcl from the pipeline and returned to the pipeline after processing due to the -PassThru parameter. 
 
```powershell   
 
$acl1 = New-ItemAcl -AccessRight item:delete -PropagationType Any -SecurityPermission AllowAccess -Identity "sitecore\admin"
$acl2 = New-ItemAcl -AccessRight item:read -PropagationType Descendants -SecurityPermission DenyAccess -Identity "sitecore\mikey"
Get-Item -Path master:\content\home | Add-ItemAcl -AccessRules $acl1, $acl2 -PassThru

Name   Children Languages                Id                                     TemplateName
----   -------- ---------                --                                     ------------
Home   False    {en, ja-JP, de-DE, da}   {110D559F-DEA5-42EA-9C1C-8A5DF7E70EF9} Sample Item 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>* [Add-ItemAcl](/appendix/commands/Add-ItemAcl.md)* [Clear-ItemAcl](/appendix/commands/Clear-ItemAcl.md)* [Get-ItemAcl](/appendix/commands/Get-ItemAcl.md)* [New-ItemAcl](/appendix/commands/New-ItemAcl.md)* [Set-ItemAcl](/appendix/commands/Set-ItemAcl.md)* [Test-ItemAcl](/appendix/commands/Test-ItemAcl.md)* <a href='https://sdn.sitecore.net/upload/sitecore6/security_administrators_cookbook_a4.pdf' target='_blank'>https://sdn.sitecore.net/upload/sitecore6/security_administrators_cookbook_a4.pdf</a><br/>* <a href='https://sdn.sitecore.net/upload/sitecore6/61/security_reference-a4.pdf' target='_blank'>https://sdn.sitecore.net/upload/sitecore6/61/security_reference-a4.pdf</a><br/>* <a href='https://sdn.sitecore.net/upload/sitecore6/64/content_api_cookbook_sc64_and_later-a4.pdf' target='_blank'>https://sdn.sitecore.net/upload/sitecore6/64/content_api_cookbook_sc64_and_later-a4.pdf</a><br/>* <a href='http://www.sitecore.net/learn/blogs/technical-blogs/john-west-sitecore-blog/posts/2013/01/sitecore-security-access-rights.aspx' target='_blank'>http://www.sitecore.net/learn/blogs/technical-blogs/john-west-sitecore-blog/posts/2013/01/sitecore-security-access-rights.aspx</a><br/>* <a href='https://briancaos.wordpress.com/2009/10/02/assigning-security-to-items-in-sitecore-6-programatically/' target='_blank'>https://briancaos.wordpress.com/2009/10/02/assigning-security-to-items-in-sitecore-6-programatically/</a><br/>
