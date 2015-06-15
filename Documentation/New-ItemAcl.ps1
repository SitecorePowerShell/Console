<#
    .SYNOPSIS
        Creates a new access rule for use with Set-ItemAcl and Add-ItemAcl cmdlets.

    .DESCRIPTION
        Creates a new access rule for use with Set-ItemAcl and Add-ItemAcl cmdlets.

    .PARAMETER PropagationType
       The PropagationType enumeration determines which items will be granted the access right.
        - Any - the item and all items inheriting
        - Descendants - applies rights to inheriting children only
        - Entity - applies right to the item only

    .PARAMETER SecurityPermission
	The SecurityPermission determines whether to grant (allow) or deny the access right, and deny or allow inheritance of the right.
    	- AllowAccess - 
    	- DenyAccess - 
    	- AllowInheritance - 
    	- DenyInheritance - 

    .PARAMETER AccessRight
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

    .PARAMETER Identity
        User name including domain for which the access rule is being created. If no domain is specified - 'sitecore' will be used as the default domain.

        Specifies the Sitecore user by providing one of the following values.

            Local Name
                Example: adam
            Fully Qualified Name
                Example: sitecore\adam
    
    .INPUTS
    
    .OUTPUTS
        Sitecore.Security.AccessControl.AccessRule

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .LINK
	Add-ItemAcl

    .LINK
	Clear-ItemAcl

    .LINK
	Get-ItemAcl

    .LINK
	Set-ItemAcl

    .LINK
	Test-ItemAcl

    .LINK
        https://sdn.sitecore.net/upload/sitecore6/security_administrators_cookbook_a4.pdf

    .LINK
	https://sdn.sitecore.net/upload/sitecore6/61/security_reference-a4.pdf

    .LINK
	https://sdn.sitecore.net/upload/sitecore6/64/content_api_cookbook_sc64_and_later-a4.pdf

    .LINK
	http://www.sitecore.net/learn/blogs/technical-blogs/john-west-sitecore-blog/posts/2013/01/sitecore-security-access-rights.aspx

    .LINK
	https://briancaos.wordpress.com/2009/10/02/assigning-security-to-items-in-sitecore-6-programatically/

    .EXAMPLE
        # Creates an access rule that allows the "sitecore\adam" user to delete the item to which it will be applied and all of its children
        PS master:\> New-ItemAcl -AccessRight item:delete -PropagationType Any -SecurityPermission AllowAccess -Identity "sitecore\adam"

        Account           AccessRight    PermissionType   PropagationType  SecurityPermission
	-------           -----------    --------------   ---------------  ------------------
	sitecore\admin    item:delete    Access           Any              AllowAccess

    .EXAMPLE
        # Allows the "sitecore\adam" user to delete the Home item and all of its children.
        # Denies the "sitecore\mikey" user reading the descendants of the Home item. ;P
        # The security info is created prior to adding it to the item.
        # The item is delivered to the Add-ItemAcl from the pipeline and returned to the pipeline after processing due to the -PassThru parameter.
        #The security information is added to the previously existing security qualifiers.
	$acl1 = New-ItemAcl -AccessRight item:delete -PropagationType Any -SecurityPermission AllowAccess -Identity "sitecore\adam"
	$acl2 = New-ItemAcl -AccessRight item:read -PropagationType Descendants -SecurityPermission DenyAccess -Identity "sitecore\mikey"
	Get-Item -Path master:\content\home | Add-ItemAcl -AccessRules $acl1, $acl2 -PassThru

        Name   Children Languages                Id                                     TemplateName
        ----   -------- ---------                --                                     ------------
        Home   False    {en, ja-JP, de-DE, da}   {110D559F-DEA5-42EA-9C1C-8A5DF7E70EF9} Sample Item

    .EXAMPLE
        # Allows the "sitecore\adam" user to delete the Home item and all of its children.
        # Denies the "sitecore\mikey" user reading the descendants of the Home item. ;P
        # The security info is created prior to setting it to the item.
        # The item is delivered to the Set-ItemAcl from the pipeline and returned to the pipeline after processing due to the -PassThru parameter.
        # Any previuous security information on the item is removed.
	$acl1 = New-ItemAcl -AccessRight item:delete -PropagationType Any -SecurityPermission AllowAccess -Identity "sitecore\adam"
	$acl2 = New-ItemAcl -AccessRight item:read -PropagationType Descendants -SecurityPermission DenyAccess -Identity "sitecore\mikey"
	Get-Item -Path master:\content\home | Set-ItemAcl -AccessRules $acl1, $acl2 -PassThru

        Name   Children Languages                Id                                     TemplateName
        ----   -------- ---------                --                                     ------------
        Home   False    {en, ja-JP, de-DE, da}   {110D559F-DEA5-42EA-9C1C-8A5DF7E70EF9} Sample Item

#>
