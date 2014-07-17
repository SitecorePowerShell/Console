<#
    .SYNOPSIS
        Start-TaskSchedule.

    .DESCRIPTION
        Start-TaskSchedule.


    .PARAMETER Schedule
        TODO: Provide description for this parameter

    .PARAMETER Item
        The item to be processed.

    .PARAMETER Path
        Path to the item to be processed - can work with Language parameter to narrow the publication scope.    
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Tasks.ScheduleItem

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Start-TaskSchedule -Path master:\content\home
#>
