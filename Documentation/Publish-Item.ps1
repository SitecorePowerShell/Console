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