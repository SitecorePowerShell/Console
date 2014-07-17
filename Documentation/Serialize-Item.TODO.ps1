<#
    .SYNOPSIS
        Serialize-Item.

    .DESCRIPTION
        Serialize-Item.


    .PARAMETER Item
        The item to be processed.

    .PARAMETER Entry
        TODO: Provide description for this parameter

    .PARAMETER Path
        Path to the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Id
        Id of the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Recurse
        Process the item and all of its children.

    .PARAMETER ItemPathsAbsolute
        TODO: Provide description for this parameter

    .PARAMETER Target
        TODO: Provide description for this parameter    
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Serialize-Item -Path master:\content\home
# >
