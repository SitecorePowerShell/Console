<#
    .SYNOPSIS
        Stops the Sitecore index.

    .DESCRIPTION
        The Stop-SearchIndex command stops the Sitecore index.

    .PARAMETER Name
        The name of the index to stop.
    
    .INPUTS
        Sitecore.ContentSearch.ISearchIndex or System.String
    
    .OUTPUTS
        None

    .NOTES
        Help Author: Adam Najmanowicz, Michael West
    
    .LINK
        Initialize-SearchIndex

    .LINK
        Suspend-SearchIndex

    .LINK
        Resume-SearchIndex

    .LINK
        Get-SearchIndex

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        The following stops the indexing process from running.

        PS master:\> Stop-SearchIndex -Name sitecore_master_index

    .EXAMPLE
        The following stops the indexing process from running.

        PS master:\> Get-SearchIndex -Name sitecore_master_index | Stop-SearchIndex
#>
