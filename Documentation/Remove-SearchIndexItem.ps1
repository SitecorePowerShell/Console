<#
    .SYNOPSIS
        Removes an item from the search index.

    .DESCRIPTION
        Removes the specified item from the search index. Useful when you want to avoid a full index rebuild.

    .PARAMETER Item
        The item to be processed.

    .PARAMETER SearchResultItem
        The search item instance returned from the search results.

    .PARAMETER AsJob
        When used, the command returns a job handle. Useful when you want to poll for the command completion.

    .PARAMETER Name
        The name or path to the item.
   
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        None or Sitecore.Jobs.Job    

    .NOTES
        Help Author: Adam Najmanowicz, Michael West
#>
