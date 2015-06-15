<#
    .SYNOPSIS
        Retrieves security access rules from an item.

    .DESCRIPTION
        Retrieves security access rules from an item.

    .PARAMETER Identity
        User name including domain for which the access rule is being created. If no domain is specified - 'sitecore' will be used as the default domain.

        Specifies the Sitecore user by providing one of the following values.

            Local Name
                Example: adam
            Fully Qualified Name
                Example: sitecore\adam

    .PARAMETER Filter
        Specifies a simple pattern to match Sitecore roles & users.

        Examples:
        The following examples show how to use the filter syntax.

        To get all the roles, use the asterisk wildcard:
        Export-Role -Filter *

        To get all the roles in a domain use the following command:
        Export-Role -Filter "sitecore\*"

    .PARAMETER Item
        The item from which the security rules should be taken.

    .PARAMETER Path
        Path to the item from which the security rules should be taken.

    .PARAMETER Id
        Id of the item from which the security rules should be taken.

    .PARAMETER Database
        Database containing the item to be fetched with Id parameter containing the security rules that should be returned.
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Security.AccessControl.AccessRule

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
	Add-ItemAcl

    .LINK
	Clear-ItemAcl

    .LINK
	Set-ItemAcl

    .LINK
	New-ItemAcl

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
        # Take the security information from the Home item and apply it to the Settings item
	$acl = Get-ItemAcl -Path master:\content\home
	Set-ItemAcl -Path master:\content\Settings -AccessRules $acl -PassThru

    .EXAMPLE
        # Take the security information from the Home item and add it to the access rules on the Settings item
	$acl = Get-ItemAcl -Path master:\content\home
	Add-ItemAcl -Path master:\content\Settings -AccessRules $acl -PassThru
#>
