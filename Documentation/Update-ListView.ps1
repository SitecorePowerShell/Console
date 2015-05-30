<#
    .SYNOPSIS
        Updates List View (created by Show-ListView) data.

    .DESCRIPTION
        This command updates the data displayed by List View that called the script the command is part of.
        Calling this command makes sense only when it's being used in script exposed as Action on the Show-ListView window.
        For example the main script might be listing all logged in users. And than the "Kick" action might be closing sessions for selected rows and refreshing the List view to take into account that the sessions are no longer connected.
        Another example is Task Manager script you can find in Toolbox. The List View shown by it shows tasks and when they were last run. If you choose to execute a task the Update-ListView command will later be called to update the data to account for the fact that the task's "Last Run" date has been updated.

    .PARAMETER Data
        Data that you want to be sent to the list view for display.

    .PARAMETER Property
        If this parameter is specified - it allows for modifying the columns shown in the list view, otherwise the columns stay the same as in the original view.
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        System.String

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Show-ListView
    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        #This example consists of 2 scripts - a report that shows a list view and the second one that updates the list view from an action.

        # THE INITIAL SCRIPT showing the list of tasks in a list view
        # This script does not yet make use of Update-ListView but tests the state for actions to be performed

        Import-Function "Edit-TaskSchedule"
        
        # Get all the items recursively where the TemplateName equals "Schedule".
        Get-ChildItem -Path master:\system\Tasks\Schedules -Recurse | Where-Object { $_.TemplateName -eq "Schedule" } |
            Show-ListView -Property @{Label="Name"; Expression={ $_.DisplayName } },
                @{Label="Last Run"; Expression={ $_."Last Run" } },
                @{Label="Command"; Expression={ $_.Database.GetItem($_.Command).Name } },
                @{Label="From"; Expression={ Parse-TaskSchedule $_ "from"} },
                @{Label="To"; Expression={ Parse-TaskSchedule $_ "to"} },
                @{Label="On Week Days"; Expression={ Parse-TaskSchedule $_ "strWeekdays" } },
                @{Label="Run Every"; Expression={ Parse-TaskSchedule $_ "interval" } } `
                -Title "Task Manager"        
        Close-Window



        # NOW THE PROPER ACTION SCRIPT
        
        # The Execute task action that (at the very end) updates the list with the latest tasks data
        foreach($sheduleItem in $resultSet)
        {
            $shedule = New-Object  -TypeName "Sitecore.Tasks.ScheduleItem" -ArgumentList $sheduleItem 
            $shedule.Execute();
        }
        Import-Function "Edit-TaskSchedule"
        Get-ChildItem -Path master:\system\Tasks\Schedules -Recurse | Where-Object { $_.TemplateName -eq "Schedule" } | Update-ListView
#>
