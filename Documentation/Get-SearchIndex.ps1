<#
    .SYNOPSIS
        Returns the available Sitecore indexes.

    .DESCRIPTION
        The Get-SearchIndex command returns the available Sitecore indexes. These are the same as those found in the Control Panel.

    .PARAMETER Name
        Name of the index to return.
    
    .INPUTS
        None or System.String
    
    .OUTPUTS
        Sitecore.ContentSearch.ISearchIndex

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Initialize-SearchIndex

    .LINK
        Stop-SearchIndex

    .LINK
        Resume-SearchIndex

    .LINK
        Suspend-SearchIndex

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        The following lists all available indexes.

        PS master:\>Get-SearchIndex
 
        Name                             IndexingState   IsRebuilding    IsSharded
        ----                             -------------   ------------    ---------
        sitecore_analytics_index         Started         False           False
        sitecore_core_index              Started         False           False
        sitecore_master_index            Started         True            False
        sitecore_web_index               Started         False           False
        sitecore_marketing_asset_inde... Started         False           False
        sitecore_marketing_asset_inde... Started         False           False
        sitecore_testing_index           Started         False           False
        sitecore_suggested_test_index    Started         False           False
        sitecore_fxm_master_index        Started         False           False
        sitecore_fxm_web_index           Started         False           False
        sitecore_list_index              Started         False           False
        social_messages_master           Started         False           False
        social_messages_web              Started         False           False

    .EXAMPLE
        The following lists only the specified index.

        PS master:\>Get-SearchIndex -Name sitecore_master_index
 
        Name                             IndexingState   IsRebuilding    IsSharded
        ----                             -------------   ------------    ---------
        sitecore_master_index            Started         True            False
#>
