<#
    .SYNOPSIS
        Deserialize-Item.

    .DESCRIPTION
        Deserialize-Item.


    .PARAMETER Database
        Database containing the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Item
        The item to be processed.

    .PARAMETER Preset
        TODO: Provide description for this parameter

    .PARAMETER Path
        Path to the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Recurse
        Process the item and all of its children.

    .PARAMETER Root
        TODO: Provide description for this parameter

    .PARAMETER UseNewId
        TODO: Provide description for this parameter

    .PARAMETER DisableEvents
        TODO: Provide description for this parameter

    .PARAMETER ForceUpdate
        TODO: Provide description for this parameter    
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        System.Void

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Deserialize-Item -Path master:\content\home
#>
