<#
    .SYNOPSIS
        Updates rendering with new values.

    .DESCRIPTION
        Updates rendering instance with new values. The instance should be earlier obtained using Get-Rendering.

    .PARAMETER Instance
        Instance of the Rendering to be updated.

    .PARAMETER Index
        If provided the rendering will be moved to the specified index.

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
        

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .LINK
        Add-Rendering

    .LINK
        New-Rendering

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
        #change all rendering's placeholder from main to footer
        PS master:\> $item = Get-Item -Path master:\content\home
        PS master:\> Get-Rendering -Item $item -PlaceHolder "main" | Foreach-Object { $_.Placeholder = "footer"; Set-Rendering -Item $item -Instance $_ }

#>
