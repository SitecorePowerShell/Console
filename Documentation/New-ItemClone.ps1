<#
    .SYNOPSIS
        Creates a new item clone based on the item provided.

    .DESCRIPTION
        Creates a new item clone based on the item provided.


    .PARAMETER Destination
        Parent item under which the clone should be created.

    .PARAMETER Name
        Name of the item clone.

    .PARAMETER Recurse
        Add the parameter to clone thw whole branch rather than a single item.

    .PARAMETER Item
        The item to be cloned.

    .PARAMETER Path
        Path to the item to be cloned.

    .PARAMETER Id
        Id of the item to be cloned

    .PARAMETER Database
        Database of the item to be cloned if item is specified through its ID.

    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Data.Items.Item

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Get-ItemClone

    .LINK
        ConvertFrom-ItemClone

    .LINK
        New-Item

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .LINK
        https://github.com/SitecorePowerShell/Console/issues/218

    .EXAMPLE
        # Clone /sitecore/content/home/ under /sitecore/content/new-target/ with the "New Home" name.
	PS master:\> $newTarget = Get-Item master:\content\new-target\
        PS master:\> New-ItemClone -Path master:\content\home -Destination $newTarget -Name "New Home"
#>
