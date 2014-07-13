<#
    .SYNOPSIS
        Publishes the Sitecore item.

    .DESCRIPTION
        The Publish-Item cmdlet publishes the Sitecore item and optionally subitems.

    .PARAMETER Target
        Specifies the publishing targets. The default target database is "master".

    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        None.

    .NOTES
        Michael West

    .LINK
        http://michaellwest.blogspot.com

    .EXAMPLE
        PS master:\> Publish-Item -Path master:\content\home -Target Internet

    .EXAMPLE
        PS master:\> Get-Item -Path master:\content\home | Publish-Item -Recurse -PublishMode Incremental
#>