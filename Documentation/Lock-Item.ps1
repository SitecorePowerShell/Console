<#
    .SYNOPSIS
        Locks the Sitecore item by the current or specified user.

    .DESCRIPTION
        The Lock-Item command unlocks the item.

    .PARAMETER Identity
        User name including domain for which the item is to be locked. If no domain is specified - 'sitecore' will be used as the default domain.

        Specifies the Sitecore user by providing one of the following values.

            Local Name
                Example: adam
            Fully Qualified Name
                Example: sitecore\adam

    .PARAMETER Id
        Id of the item to be processed.

    .PARAMETER PassThru
        Passes the processed object back into the pipeline.   

    .PARAMETER Item
        The item to be processed.

    .PARAMETER Path
        Path to the item to be processed - can work with Language parameter to specify the language other than current session language.

    .PARAMETER Id
        Id of the item to be processed - can work with Language parameter to specify the language other than current session language.

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
        Unlock-Item

    .LINK
        Get-Item

    .EXAMPLE
        #Lock the Home item providing its path
        PS master:\> Lock-Item -Path master:\content\home

    .EXAMPLE
        #Lock the Home item by providing it from the pipeline and passing it back to the pipeline. The Item is locked by the "sitecore\adam" user.
        PS master:\> Get-Item -Path master:\content\home | Lock-Item -PassThru -Identity sitecore\adam

        Name   Children Languages                Id                                     TemplateName
        ----   -------- ---------                --                                     ------------
        Home   False    {en, ja-JP, de-DE, da}   {110D559F-DEA5-42EA-9C1C-8A5DF7E70EF9} Sample Item

#>