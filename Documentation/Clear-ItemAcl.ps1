<#
    .SYNOPSIS
        Removes all security information from the item specified.

    .DESCRIPTION
        Removes all security information from the item specified.

    .PARAMETER PassThru
        Passes the processed item back into the pipeline.

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
	Add-ItemAcl

    .LINK
	Get-ItemAcl

    .LINK
	New-ItemAcl

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
        # Clears the security information from the Home item providing its path
        PS master:\> Clear-ItemAcl -Path master:\content\home

    .EXAMPLE
        # Clears the security information from the Home item by providing it from the pipeline and passing it back to the pipeline.
        PS master:\> Get-Item -Path master:\content\home | Clear-ItemAcl -PassThru

        Name   Children Languages                Id                                     TemplateName
        ----   -------- ---------                --                                     ------------
        Home   False    {en, ja-JP, de-DE, da}   {110D559F-DEA5-42EA-9C1C-8A5DF7E70EF9} Sample Item

#>
