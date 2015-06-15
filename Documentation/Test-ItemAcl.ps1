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

    .PARAMETER Item
        The item to be tested against.

    .PARAMETER Path
        Path to the item to be tested against.

    .PARAMETER Id
        Id of the item to be tested against. Requires the Database parameter to be specified.

    .PARAMETER Database
        Database containing the item to be fetched with Id parameter.

    .PARAMETER AccessRight
        Access right / action to be tested for the user.
    
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

