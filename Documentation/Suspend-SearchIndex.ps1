<#
    .SYNOPSIS
        Suspends (pauses) the Sitecore index.

    .DESCRIPTION
        The Suspend-SearchIndex command suspends (pauses) the Sitecore index.

    .PARAMETER Name
        The name of the index to suspend (pause).
    
    .INPUTS
        Sitecore.ContentSearch.ISearchIndex or System.String
    
    .OUTPUTS
        None

    .NOTES
        Help Author: Adam Najmanowicz, Michael West
    
    .LINK
        Initialize-SearchIndex

    .LINK
        Stop-SearchIndex

    .LINK
        Resume-SearchIndex

    .LINK
        Get-SearchIndex

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        The following suspends (pauses) the indexing process from running.

        PS master:\> Suspend-SearchIndex -Name sitecore_master_index

    .EXAMPLE
        The following suspends (pauses) the indexing process from running.

        PS master:\> Get-SearchIndex -Name sitecore_master_index | Suspend-SearchIndex
#>
