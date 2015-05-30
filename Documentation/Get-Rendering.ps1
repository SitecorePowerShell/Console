<#
    .SYNOPSIS
        Returns a RenderingDefinition for an item using the filtering parameters.

    .DESCRIPTION
        The Get-Rendering command returns a RenderingDefinition for an item using the filtering parameters.

    .PARAMETER Item
        The item to be processed.

    .PARAMETER Path
        Path to the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Id
        Id of the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Database
        Database containing the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER DataSource
        Data source filter - supports wildcards.
    .PARAMETER Rendering
        Item representing the sublayout/rendering. If matching the rendering will be returned.

    .PARAMETER Index
        Index at which the rendering exists in the layout. The rendering at that index will be returned.

    .PARAMETER PlaceHolder
        Place holder at which the rendering exists in the layout. Renderings at that place holder will be returned.

    .PARAMETER Parameter
        Additional rendering parameter values. If both name and value match - the rendering will be returned. Values support wildcards.

    .PARAMETER Instance
        Specific instance of rendering that should be returned. The instance could earlier be obtained through e.g. use of Get-Rendering.

    .PARAMETER UniqueId
        UniqueID of the rendering to be retrieved.

    .PARAMETER Device
        Device for which the renderings will be retrieved.
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Layouts.RenderingDefinition

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .LINK
        Add-Rendering

    .LINK
        New-Rendering

    .LINK
        Set-Rendering

    .LINK
        Get-LayoutDevice

    .LINK
        Remove-Rendering

    .LINK
        Get-Layout

    .LINK
        Set-Layout

    .EXAMPLE
        # get all renderings for "Default" device, located in the any placeholder that has name in it or any of its sub-placeholders
        PS master:\> Get-Item master:\content\home | Get-Rendering -Placeholder "*main*" -Device (Get-LayoutDevice "Default")
#>
