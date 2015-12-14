<#
    .SYNOPSIS
        Publishes a Sitecore item.

    .DESCRIPTION
        The Publish-Item command publishes the Sitecore item and optionally subitems. Allowing for granular control over languages and modes of publishing.

    .PARAMETER Target
        Specifies the publishing targets. The default target database is "web".

    .PARAMETER Recurse
        Specifies that subitems should also get published with the root item.
	
    .PARAMETER PublishMode
        Specified the Publish mode. Valid values are: 
        - Full
        - Incremental
        - SingleItem
        - Smart

    .PARAMETER Path
        Path to the item that should be published - can work with Language parameter to narrow the publication scope.

    .PARAMETER Id
        Id of the item that should be published - can work with Language parameter to narrow the publication scope.

    .PARAMETER Language
        Language of the item that should be published. Supports globbing/wildcards.
        Allows for more than one language to be provided at once. e.g. "en*", "pl-pl"

    .PARAMETER PublishRelatedItems
        Turns publishing of related items on. Works only on Sitecore 7.2 or newer

    .PARAMETER RepublishAll
        Republishes all items provided to the publishing job.

    .PARAMETER CompareRevisions
        Turns revision comparison on.

    .PARAMETER FromDate
        Publishes items newer than the date provided only.

    .PARAMETER Synchronous
        Performs the publishing action synchronously making the script wait for the publishing opration to end.

    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        None.

    .NOTES
        Help Author: Michael West, Adam Najmanowicz

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Publish-Item -Path master:\content\home -Target Internet		

    .EXAMPLE
        PS master:\> Get-Item -Path master:\content\home | Publish-Item -Recurse -PublishMode Incremental

    .EXAMPLE
        PS master:\> Get-Item -Path master:\content\home | Publish-Item -Recurse -Language "en*"
#>