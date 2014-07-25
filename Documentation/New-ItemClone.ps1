<#
    .SYNOPSIS
        Creates a new item clone based on the item provided.

    .DESCRIPTION
        Creates a new item clone based on the item provided.


    .PARAMETER Destination
        Parent item under which the clone should be created.

    .PARAMETER Name
        Name of the item clone.

    .PARAMETER Recursive
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
        https://github.com/SitecorePowerShell/Console/

    .LINK
        https://github.com/SitecorePowerShell/Console/issues/218

    .EXAMPLE
        PS master:\> New-ItemClone -Path master:\content\home
#>
