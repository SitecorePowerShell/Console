<#
    .SYNOPSIS
        Returns sitecore Search indices.

    .DESCRIPTION
        Returns sitecore Search indices.

    .PARAMETER Name
        Name of the index to return.
    
    .INPUTS
    
    .OUTPUTS
        Sitecore.Search.Index

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\>Get-SearchIndex | ft -auto
         
        Name   Analyzer                                      Directory
        ----   --------                                      ---------
        system Lucene.Net.Analysis.Standard.StandardAnalyzer Lucene.Net.Store.SimpleFSDirectory@C:\Projects\ZenGarden\Data\indexes\__system lockFactory=Sitecore.Search.SitecoreLockFactory
        WeBlog Lucene.Net.Analysis.Standard.StandardAnalyzer Lucene.Net.Store.SimpleFSDirectory@C:\Projects\ZenGarden\Data\indexes\WeBlog lockFactory=Sitecore.Search.SitecoreLockFactory

#>
