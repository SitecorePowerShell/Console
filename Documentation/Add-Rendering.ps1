<#
    .SYNOPSIS
        Adds Rendering to Item.

    .DESCRIPTION
        Adds rendering to a chosen device for the presentation of an item.

    .PARAMETER Instance
        Rendering definition to be added to the item

    .PARAMETER Parameter
        Rendering Parameters to be overriden on the Rendering that is being updated - if not specified the value provided in rendering definition specified in the Instance parameter will be used.

    .PARAMETER PlaceHolder
        Placeholder path the Rendering should be added to - if not specified the value provided in rendering definition specified in the Instance parameter will be used.

    .PARAMETER DataSource
        Data source of the Rendering - if not specified the value provided in rendering definition specified in the Instance parameter will be used.

    .PARAMETER Index
        Index at which the Rendering should be inserted. If not provided the rendering will be appended at the end of the list.

    .PARAMETER Device
        Device the rendering is assigned to. If not specified - default device will be used.

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
        New-Rendering

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
