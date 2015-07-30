<#
    .SYNOPSIS
        Rebuilds the Sitecore index.

    .DESCRIPTION
        The Rebuild-SearchIndex command rebuilds Sitecore index. This command is an alias for Initialize-SearchIndex.

    .PARAMETER Name
        The name of the index to resume.

    .PARAMETER Index
        The index instance.

    .PARAMETER IncludeRemoteIndex
        The remote indexing should be triggered.

    .PARAMETER AsJob
        The job created for rebuilding the index should be returned as output.
    
    .INPUTS
        None or Sitecore.Jobs.Job
    
    .OUTPUTS
        None

    .NOTES
        Help Author: Adam Najmanowicz, Michael West
    
    .LINK
        Resume-SearchIndex

    .LINK
        Suspend-SearchIndex

    .LINK
        Stop-SearchIndex

    .LINK
        Get-SearchIndex

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        The following rebuilds the index.

        PS master:\> Rebuild-SearchIndex -Name sitecore_master_index

    .EXAMPLE
        The following rebuilds the index.

        PS master:\> Get-SearchIndex -Name sitecore_master_index | Rebuild-SearchIndex
#>
