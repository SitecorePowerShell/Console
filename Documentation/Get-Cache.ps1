<#
    .SYNOPSIS
        Retrieves a Sitecore cache.

    .DESCRIPTION
        Retrieves a Sitecore cache.

    .PARAMETER Name
        Name of the cache to retrieve. Supports wildcards.
    
    .INPUTS
    
    .OUTPUTS
        Sitecore.Caching.Cache

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Get-Cache -Name master*

        Name                                     Enabled       Count       Size   Max Size Default  Scavengable
                                                                                           Priority
        ----                                     -------       -----       ----   -------- -------- -----------
        master[blobIDs]                          True              0          0     512000   Normal       False
        master[blobIDs]                          True              0          0     512000   Normal       False
        master[blobIDs]                          True              0          0     512000   Normal       False
        master[itempaths]                        True            292     108228   10485760   Normal       False
        master[standardValues]                   True             57      38610     512000   Normal       False
        master[paths]                            True            108      13608     512000   Normal       False
        master[items]                            True           1010    5080300   10485760   Normal       False
        master[data]                             True           3427    7420654   20971520   Normal       False

#>
