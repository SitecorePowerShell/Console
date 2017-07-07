<#
    .SYNOPSIS
        Initialize-SearchIndexItem.

    .DESCRIPTION
        Initialize-SearchIndexItem.

    .PARAMETER Item
        The item which should be indexed.

    .PARAMETER SearchResultItem
        The item returned by the search index which should be indexed.

    .PARAMETER AsJob
        Indicates that the command should return a job handle. Useful when you need to poll for the completion of the job.

    .PARAMETER Name
        The name or path of the item to be indexed.
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        None or Sitecore.Jobs.Job

    .LINK
        Find-Item

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

#>
