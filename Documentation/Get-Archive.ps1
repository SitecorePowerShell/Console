<#
    .SYNOPSIS
        Returns Sitecore database archives.

    .DESCRIPTION
        Returns Sitecore archives in context of a particular database.

    .PARAMETER Name
        Name of the archive to retrieve.

    .PARAMETER Database
        Database for which the archives should be retrieved.
    
    .INPUTS
        Sitecore.Data.Database

    .OUTPUTS
        Sitecore.Data.Archiving.Archive

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Get-Archive -Database "master"
        
        Name                                        Items
        ----                                        -----
        archive                                         0
        recyclebin                                   1950
#>
