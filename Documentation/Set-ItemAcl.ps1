<#
    .SYNOPSIS
        Sets new security information on an item overwriting the previous settings.

    .DESCRIPTION
        Sets new security information on an item. The new rules will overwrite the existing security descriptors on the item.

    .PARAMETER AccessRules
        A single or multiple access rules created e.g. through the New-ItemAcl or obtained from other item using the Get-ItemAcl cmdlet.
        This information will overwrite the existing security descriptors on the item.

    .PARAMETER PassThru
        Passes the processed object back into the pipeline.

    .PARAMETER Item
        The item to be processed.

    .PARAMETER Path
        Path to the item to be processed.

    .PARAMETER Id
        Id of the item to be processed. Requires the Database parameter to be specified.

    .PARAMETER Database
        Database containing the item to be fetched with Id parameter.

    .PARAMETER WhatIf
        Shows what would happen if the cmdlet runs. The cmdlet is not run.

    .PARAMETER Confirm
        Prompts you for confirmation before running the cmdlet.    
    
    .INPUTS
        Sitecore.Data.Items.Item
        # can be piped from another cmdlet
    
    .OUTPUTS
        Sitecore.Data.Items.Item
        # Only if -PassThru is used

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
