<#
    .SYNOPSIS
        Sets the current host property.

    .DESCRIPTION
        Sets the current host property and perssits them for the future if used with -Persist parameter.


    .PARAMETER ForegroundColor
        Color of the console text.

    .PARAMETER BackgroundColor
        Color of the console background.

    .PARAMETER HostWidth
        Width of the text buffer (texts longer than the number provided will wrap to the next line.

    .PARAMETER Persist
        Persist the console setting provided
    
    .INPUTS
    
    .OUTPUTS        

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        #Set width of the console buffer to 80 and persist it for the future instances 
        PS master:\> Set-HostProperty -HostWidth 80 -Persist

    .EXAMPLE
        #Set color of the console text to cyan. Next instance of the console will revert to default (white).
        PS master:\> Set-HostProperty -ForegroundColor Cyan

#>
