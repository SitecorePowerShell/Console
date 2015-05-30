<#
    .SYNOPSIS
        Unprotects the Sitecore item by the current or specified user.

    .DESCRIPTION
        The Unprotect-Item command protects the item.

    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        None.

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .LINK
        Protect-Item

    .LINK
        Get-Item

    .EXAMPLE
        PS master:\> Protect-Item -Path master:\content\home

    .EXAMPLE
        PS master:\> Get-Item -Path master:\content\home | Unprotect-Item -PassThru

        Name                             Children Languages                Id                                     TemplateName
        ----                             -------- ---------                --                                     ------------
        Home                             False    {en, ja-JP, de-DE, da}   {110D559F-DEA5-42EA-9C1C-8A5DF7E70EF9} Sample Item

#>