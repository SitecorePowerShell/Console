<#
    .SYNOPSIS
        Unprotects the specified Sitecore item.

    .DESCRIPTION
        The Unprotect-Item command removes protection from the item provided to it.

    .PARAMETER Id
        Id of the item to be unprotected.

    .PARAMETER PassThru
        Passes the unprotected item back into the pipeline.   

    .PARAMETER Item
        The item to be unprotected.

    .PARAMETER Path
        Path to the item to be unprotected - can work with Language parameter to specify the language other than current session language.

    .PARAMETER Id
        Id of the item to be unprotected - can work with Language parameter to specify the language other than current session language.

    .PARAMETER Database
        Database containing the item to be fetched with Id parameter.

    .PARAMETER Confirm
	Prompts you for confirmation before running the cmdlet.

    .PARAMETER WhatIf
	Shows what would happen if the cmdlet runs. The cmdlet is not run.

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
        Protect-Item

    .LINK
        Get-Item

    .EXAMPLE
        #Unprotect the Home item providing its path
        PS master:\> Unprotect-Item -Path master:\content\home

    .EXAMPLE
        #Unprotect the Home item providing it from the pipeline and passing it back to the pipeline 
        PS master:\> Get-Item -Path master:\content\home | Unprotect-Item -PassThru

        Name   Children Languages                Id                                     TemplateName
        ----   -------- ---------                --                                     ------------
        Home   False    {en, ja-JP, de-DE, da}   {110D559F-DEA5-42EA-9C1C-8A5DF7E70EF9} Sample Item

#>