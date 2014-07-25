<#
    .SYNOPSIS
        Returns Sitecore database indices.

    .DESCRIPTION
        Returns Sitecore indices in context of a particular database.

    .PARAMETER Name
        Name of the index to retrieve.

    .PARAMETER Database
        Database for which the indices should be retrieved.
    
    .INPUTS
        Sitecore.Data.Database

    .OUTPUTS
        Sitecore.Data.Indexing.Index

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Get-Index -Database "master"
#>
