<#
    .SYNOPSIS
        Creates new rendering definition that can later be added to an item.

    .DESCRIPTION
        Creates new rendering definition that can later be added to an item. Most parameters can later be overriden when calling Add-Rendering.


    .PARAMETER Parameter
        Rendering parameters as hashtable

    .PARAMETER PlaceHolder
        Placeholder for the rendering to be placed into.

    .PARAMETER DataSource
        Datasource for the rendering.

    .PARAMETER Cacheable
        Defined whether the rendering is cacheable.

    .PARAMETER VaryByData
        Defines whether a data-specific cache version of the rendering should be kept.

    .PARAMETER VaryByDevice
        Defines whether a device-specific cache version of the rendering should be kept.

    .PARAMETER VaryByLogin
        Defines whether a login - specific cache version of the rendering should be kept.

    .PARAMETER VaryByParameters
        Defines whether paremeter - specific cache version of the rendering should be kept.

    .PARAMETER VaryByQueryString
        Defines whether query string - specific cache version of the rendering should be kept.

    .PARAMETER VaryByUser
        Defines whether a user - specific cache version of the rendering should be kept.

    .PARAMETER Item
        The item to be processed.

    .PARAMETER Path
        Path to the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Id
        Id of the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Database
        Database containing the item to be processed - can work with Language parameter to narrow the publication scope.

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
        Set-Rendering

    .LINK
        Get-Rendering

    .LINK
        Get-LayoutDevice

    .LINK
        Remove-Rendering

    .LINK
        Get-Layout

    .LINK
        Set-Layout

    .EXAMPLE
        # find item defining rendering and create rendering definition
        PS master:\> $renderingItem = gi master:\layout\Sublayouts\ZenGarden\Basic\Content | New-Rendering -Placeholder "main"
        # find item you want the rendering added to
        PS master:\> $item = gi master:\content\Demo\Int\Home
        # Add the rendering to the item
        PS master:\> Add-Rendering -Item $item -PlaceHolder "main" -Rendering $renderingItem -Parameter @{ FieldName = "Content" }
#>
