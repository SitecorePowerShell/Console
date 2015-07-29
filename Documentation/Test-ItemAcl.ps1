<#
    .SYNOPSIS
        Tests a specific access right for a specified user against the provided item

    .DESCRIPTION
        Checks if a user can perform an operation on an item.


    .PARAMETER Identity
        User name including domain for which the access rule is being created. If no domain is specified - 'sitecore' will be used as the default domain.

        Specifies the Sitecore user by providing one of the following values.

            Local Name
                Example: adam
            Fully Qualified Name
                Example: sitecore\adam

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

    .PARAMETER Item
        The item to be tested against.

    .PARAMETER Path
        Path to the item to be tested against.

    .PARAMETER Id
        Id of the item to be tested against. Requires the Database parameter to be specified.

    .PARAMETER Database
        Database containing the item to be fetched with Id parameter.
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        System.Boolean

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
	New-ItemAcl

    .LINK
	Set-ItemAcl

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
        # Denies the "sitecore\author2" user renaming the descendants of the Home item.
        # The security info is created prior to adding it to the item.
        # The item is delivered to the Add-ItemAcl from the pipeline and returned to the pipeline after processing due to the -PassThru parameter.
	PS master:\> $acl = New-ItemAcl -AccessRight item:rename -PropagationType Descendants -SecurityPermission AllowAccess -Identity "sitecore\author2"
	PS master:\> Get-Item -Path master:\content\home | Set-ItemAcl -AccessRules $acl

        # Assuming the Home item has one child and author2 does not have rename rights granted above in the tree and is not an administrator
	PS master:\> Get-Item master:\content\home | Test-ItemAcl -Identity "sitecore\author2" -AccessRight item:rename
	False

	PS master:\> Get-ChildItem master:\content\home | Test-ItemAcl -Identity "sitecore\author2" -AccessRight item:rename
	True	
#>

