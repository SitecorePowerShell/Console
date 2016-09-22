# Set-ItemAcl 
 
Sets new security information on an item overwriting the previous settings. 
 
## Syntax 
 
Set-ItemAcl -AccessRules &lt;AccessRuleCollection&gt; [-Path] &lt;String&gt; [-PassThru] 
 
Set-ItemAcl -AccessRules &lt;AccessRuleCollection&gt; -Id &lt;String&gt; [-Database &lt;String&gt;] [-PassThru] 
 
Set-ItemAcl -AccessRules &lt;AccessRuleCollection&gt; [-Item] &lt;Item&gt; [-PassThru] 
 
 
## Detailed Description 
 
Sets new security information on an item. The new rules will overwrite the existing security descriptors on the item. 
 
© 2010-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -AccessRules&nbsp; &lt;AccessRuleCollection&gt; 
 
A single or multiple access rules created e.g. through the New-ItemAcl or obtained from other item using the Get-ItemAcl cmdlet.
This information will overwrite the existing security descriptors on the item. 
 
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
 
Passes the processed object back into the pipeline. 
 
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
            <td>1</td>
        </tr>
        <tr>
            <td>Default Value</td>
            <td></td>
        </tr>
        <tr>
            <td>Accept Pipeline Input?</td>
            <td>true (ByValue, ByPropertyName)</td>
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
 
### -Id&nbsp; &lt;String&gt; 
 
Id of the item to be processed. Requires the Database parameter to be specified. 
 
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
 
Database containing the item to be fetched with Id parameter. 
 
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
 
Take the security information from the Home item and apply it to the Settings item 
 
```powershell   
 
$acl = Get-ItemAcl -Path master:\content\home
Set-ItemAcl -Path master:\content\Settings -AccessRules $acl -PassThru 
 
``` 
 
### EXAMPLE 2 
 
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
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>* Add-ItemAcl* Clear-ItemAcl* Get-ItemAcl* New-ItemAcl* Test-ItemAcl* <a href='https://sdn.sitecore.net/upload/sitecore6/security_administrators_cookbook_a4.pdf' target='_blank'>https://sdn.sitecore.net/upload/sitecore6/security_administrators_cookbook_a4.pdf</a><br/>* <a href='https://sdn.sitecore.net/upload/sitecore6/61/security_reference-a4.pdf' target='_blank'>https://sdn.sitecore.net/upload/sitecore6/61/security_reference-a4.pdf</a><br/>* <a href='https://sdn.sitecore.net/upload/sitecore6/64/content_api_cookbook_sc64_and_later-a4.pdf' target='_blank'>https://sdn.sitecore.net/upload/sitecore6/64/content_api_cookbook_sc64_and_later-a4.pdf</a><br/>* <a href='http://www.sitecore.net/learn/blogs/technical-blogs/john-west-sitecore-blog/posts/2013/01/sitecore-security-access-rights.aspx' target='_blank'>http://www.sitecore.net/learn/blogs/technical-blogs/john-west-sitecore-blog/posts/2013/01/sitecore-security-access-rights.aspx</a><br/>* <a href='https://briancaos.wordpress.com/2009/10/02/assigning-security-to-items-in-sitecore-6-programatically/' target='_blank'>https://briancaos.wordpress.com/2009/10/02/assigning-security-to-items-in-sitecore-6-programatically/</a><br/>
