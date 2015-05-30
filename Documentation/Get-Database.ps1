<#
    .SYNOPSIS
        Retrieves a Sitecore Database.

    .DESCRIPTION
        The Get-Database command retrieves one or more Sitecore Database objects based on name or item passed to it.

    .PARAMETER Name
        Name of the database to be returned.

    .PARAMETER Item
        Database returned will be taken from the item passed to the command.
    
    .INPUTS
        Sitecore.Data.Items.Item
        System.String
    
    .OUTPUTS
        Sitecore.Data.Database

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Get-Database
        Name                 Languages                      Protected  Read Only
        ----                 ---------                      ---------  ---------
        core                 {da, pl-PL, ja-JP, en...}      False      False
        master               {en, de-DE, es-ES, pt-BR...}   False      False
        web                  {es-ES, de-DE, pt-BR, pl-PL... False      False
        filesystem           {en, en-US}                    False      True

    .EXAMPLE
        PS master:\> Get-Database -Name "master"

        Name                 Languages                      Protected  Read Only
        ----                 ---------                      ---------  ---------
        master               {en, de-DE, es-ES, pt-BR...}   False      False

    .EXAMPLE
        PS master:\> Get-Item . | Get-Database

        Name                 Languages                      Protected  Read Only
        ----                 ---------                      ---------  ---------
        master               {en, de-DE, es-ES, pt-BR...}   False      False

#>
