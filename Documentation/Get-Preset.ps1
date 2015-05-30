<#
    .SYNOPSIS
        Returns a serialization preset for use with Export-Item.

    .DESCRIPTION
        The Get-Preset command returns a serialization preset for use with Export-Item.

    .PARAMETER Name
        Name of the serialization preset.
    
    .INPUTS
    
    .OUTPUTS
        Sitecore.Data.Serialization.Presets.IncludeEntry

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Serialize-Item

    .LINK
        Deserialize-Item

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Get-Preset -Name "PowerShell", "AssetsOptimiser" | ft PresetName, Database, Path -AutoSize
        
        PresetName      Database Path
        ----------      -------- ----
        PowerShell      core     /sitecore/templates/Modules/PowerShell Console
        PowerShell      core     /sitecore/system/Modules/PowerShell/Console Colors
        PowerShell      core     /sitecore/system/Modules/PowerShell/Script Library
        PowerShell      core     /sitecore/layout/Layouts/Applications/PowerShell Console
        PowerShell      core     /sitecore/layout/Layouts/Applications/PowerShell ISE Sheer
        PowerShell      core     /sitecore/layout/Layouts/Applications/PowerShell ISE
        PowerShell      core     /sitecore/layout/Layouts/Applications/PowerShell ListView
        PowerShell      core     /sitecore/content/Documents and Settings/All users/Start menu/Right/PowerShell Toolbox
        PowerShell      core     /sitecore/content/Applications/PowerShell
        PowerShell      core     /sitecore/content/Applications/Content Editor/Context Menues/Default/Context PowerShell Scripts
        PowerShell      master   /sitecore/templates/Modules/PowerShell Console
        PowerShell      master   /sitecore/system/Modules/PowerShell/Console Colors
        PowerShell      master   /sitecore/system/Modules/PowerShell/Rules
        PowerShell      master   /sitecore/system/Modules/PowerShell/Script Library
        AssetsOptimiser master   /sitecore/templates/Cognifide/Optimiser
        AssetsOptimiser master   /sitecore/system/Modules/Optimiser
#>
