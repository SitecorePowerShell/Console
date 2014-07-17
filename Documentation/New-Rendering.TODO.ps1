<#
    .SYNOPSIS
        New-Rendering.

    .DESCRIPTION
        New-Rendering.


    .PARAMETER Parameter
        TODO: Provide description for this parameter

    .PARAMETER PlaceHolder
        TODO: Provide description for this parameter

    .PARAMETER DataSource
        TODO: Provide description for this parameter

    .PARAMETER Cacheable
        TODO: Provide description for this parameter

    .PARAMETER VaryByData
        TODO: Provide description for this parameter

    .PARAMETER VaryByDevice
        TODO: Provide description for this parameter

    .PARAMETER VaryByLogin
        TODO: Provide description for this parameter

    .PARAMETER VaryByParameters
        TODO: Provide description for this parameter

    .PARAMETER VaryByQueryString
        TODO: Provide description for this parameter

    .PARAMETER VaryByUser
        TODO: Provide description for this parameter

    .PARAMETER Item
        The item to be processed.

    .PARAMETER Path
        Path to the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Id
        Id of the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Database
        Database containing the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Language
        If you need the item in specific Language You can specify it with this parameter. Globbing/wildcard supported.    
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Layouts.RenderingDefinition

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> New-Rendering -Path master:\content\home
#>
