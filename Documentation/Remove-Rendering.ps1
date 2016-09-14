<#
    .SYNOPSIS
        Removes renderings from an item.

    .DESCRIPTION
        Removes renderings from an item based on a number of qualifying criteria. The search criteria are cumulative and narrowing the search in an "AND" manner.

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
        Item representing the sublayout/rendering. If matching the rendering will be removed.

    .PARAMETER Index
        Index at which the rendering exists in the layout. The rendering at that index will be removed.

    .PARAMETER PlaceHolder
        Place holder at which the rendering exists in the layout. Rendering at that placeholder will be removed.

    .PARAMETER Parameter
        Additional rendering parameter values. If both name and value match - the rendering will be removed. Values support wildcards.

    .PARAMETER Instance
        Specific instance of rendering that should be removed. The instance coule earlier be obtained through e.g. use of Get-Rendering.

    .PARAMETER UniqueId
        UniqueID of the rendering to be removed. The instance coule earlier be obtained through e.g. use of OD of rendering retrieved with Get-Rendering.

    .PARAMETER Device
        Device for which the rendering should be removed.

    .PARAMETER FinalLayout
        Targets the Final Layout. If not provided, the Shared Layout will be targeted. Applies to Sitecore 8.0 and higher only.

    .INPUTS
        Sitecore.Data.Items.Item

    .OUTPUTS
        System.Void

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
        Get-Rendering

    .LINK
        Get-LayoutDevice

    .LINK
        Get-Layout

    .LINK
        Set-Layout

    .EXAMPLE
        #remove all renderings for "Default" device
        PS master:\> Remove-Rendering -Path master:\content\home -Device (Get-LayoutDevice "Default")

    .EXAMPLE
        #remove all renderings from the "main" placeholder and all of its embedded placeholders.
        PS master:\> Remove-Rendering -Path master:\content\home -PlaceHolder "main*"

    .EXAMPLE
        #remove all renderings from the "main" placeholder and all of its embedded placeholders, but only in the "Default" device
        PS master:\> Remove-Rendering -Path master:\content\home -PlaceHolder "main*" -Device (Get-LayoutDevice "Default")
#>
