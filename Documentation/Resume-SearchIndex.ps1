<#
    .SYNOPSIS
        Resumes the suspended (paused) Sitecore index.

    .DESCRIPTION
        The Resume-SearchIndex command resumes the suspended (paused) Sitecore index.

    .PARAMETER Name
        The name of the index to resume.
    
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
        Stop-SearchIndex

    .LINK
        Get-SearchIndex

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        The following stops the indexing process from running.

        PS master:\> Resume-SearchIndex -Name sitecore_master_index

    .EXAMPLE
        The following stops the indexing process from running.

        PS master:\> Get-SearchIndex -Name sitecore_master_index | Resume-SearchIndex
#>