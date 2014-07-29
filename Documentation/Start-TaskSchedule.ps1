<#
    .SYNOPSIS
        Executes a task schedule.

    .DESCRIPTION
        Executes a task schedule either passed from Get-Schedule, based on Item or Schedule path.


    .PARAMETER Schedule
        ScheduleItem most conveniently obtained from Get-Schedule commandlet.

    .PARAMETER Item
        Schedule item - if Item is of wrong template - an appropriate error will be written to teh host.

    .PARAMETER Path
        Path to the schedule item - if item is of wrong template - an appropriate error will be written to teh host.
    
    .INPUTS
        Sitecore.Tasks.ScheduleItem
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Tasks.ScheduleItem

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Get-TaskSchedule

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Start-TaskSchedule -Path "master:/system/Tasks/Schedules/Email Campaign/Clean Message History"
        
        Name                             Database        Active   Auto Remove  Is Due   Expired  Completed    Last Run               Next Run
        ----                             --------        ------   -----------  ------   -------  ---------    --------               --------
        Clean Message History            master          True     False        False    False    False        2014-07-29 16:22:49    2014-07-30 04:52:49

    .EXAMPLE
        PS master:\> Get-TaskSchedule -Name "Check Bounced Messages" -Database "master" | Start-TaskSchedule
        
        Name                             Database        Active   Auto Remove  Is Due   Expired  Completed    Last Run               Next Run
        ----                             --------        ------   -----------  ------   -------  ---------    --------               --------
        Check Bounced Messages           master          True     False        False    False    False        2014-07-29 16:21:33    2014-07-30 04:51:33

#>
