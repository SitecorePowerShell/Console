# Update-ListView 
 
Updates List View (created by Show-ListView) data. 
 
## Syntax 
 
Update-ListView [-InfoTitle &lt;String&gt;] [-InfoDescription &lt;String&gt;] [-MissingDataMessage &lt;String&gt;] [-Icon &lt;String&gt;] -Data &lt;Object&gt; [-Property &lt;Object[]&gt;] 
 
 
## Detailed Description 
 
This command updates the data displayed by List View that called the script the command is part of.
Calling this command makes sense only when it's being used in script exposed as Action on the Show-ListView window.
For example the main script might be listing all logged in users. And than the "Kick" action might be closing sessions for selected rows and refreshing the List view to take into account that the sessions are no longer connected.
Another example is Task Manager script you can find in Toolbox. The List View shown by it shows tasks and when they were last run. If you choose to execute a task the Update-ListView command will later be called to update the data to account for the fact that the task's "Last Run" date has been updated. 
 
© 2010-2017 Adam Najmanowicz, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -InfoTitle&nbsp; &lt;String&gt; 
 
 
 
<table>
    <thead></thead>
    <tbody>
        <tr>
            <td>Aliases</td>
            <td></td>
        </tr>
        <tr>
            <td>Required?</td>
            <td>false</td>
        </tr>
        <tr>
            <td>Position?</td>
            <td>named</td>
        </tr>
        <tr>
            <td>Default Value</td>
            <td></td>
        </tr>
        <tr>
            <td>Accept Pipeline Input?</td>
            <td>false</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
### -InfoDescription&nbsp; &lt;String&gt; 
 
 
 
<table>
    <thead></thead>
    <tbody>
        <tr>
            <td>Aliases</td>
            <td></td>
        </tr>
        <tr>
            <td>Required?</td>
            <td>false</td>
        </tr>
        <tr>
            <td>Position?</td>
            <td>named</td>
        </tr>
        <tr>
            <td>Default Value</td>
            <td></td>
        </tr>
        <tr>
            <td>Accept Pipeline Input?</td>
            <td>false</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
### -MissingDataMessage&nbsp; &lt;String&gt; 
 
 
 
<table>
    <thead></thead>
    <tbody>
        <tr>
            <td>Aliases</td>
            <td></td>
        </tr>
        <tr>
            <td>Required?</td>
            <td>false</td>
        </tr>
        <tr>
            <td>Position?</td>
            <td>named</td>
        </tr>
        <tr>
            <td>Default Value</td>
            <td></td>
        </tr>
        <tr>
            <td>Accept Pipeline Input?</td>
            <td>false</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
### -Icon&nbsp; &lt;String&gt; 
 
 
 
<table>
    <thead></thead>
    <tbody>
        <tr>
            <td>Aliases</td>
            <td></td>
        </tr>
        <tr>
            <td>Required?</td>
            <td>false</td>
        </tr>
        <tr>
            <td>Position?</td>
            <td>named</td>
        </tr>
        <tr>
            <td>Default Value</td>
            <td></td>
        </tr>
        <tr>
            <td>Accept Pipeline Input?</td>
            <td>false</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
### -Data&nbsp; &lt;Object&gt; 
 
Data that you want to be sent to the list view for display. 
 
<table>
    <thead></thead>
    <tbody>
        <tr>
            <td>Aliases</td>
            <td></td>
        </tr>
        <tr>
            <td>Required?</td>
            <td>true</td>
        </tr>
        <tr>
            <td>Position?</td>
            <td>named</td>
        </tr>
        <tr>
            <td>Default Value</td>
            <td></td>
        </tr>
        <tr>
            <td>Accept Pipeline Input?</td>
            <td>true (ByValue)</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
### -Property&nbsp; &lt;Object[]&gt; 
 
If this parameter is specified - it allows for modifying the columns shown in the list view, otherwise the columns stay the same as in the original view. 
 
<table>
    <thead></thead>
    <tbody>
        <tr>
            <td>Aliases</td>
            <td></td>
        </tr>
        <tr>
            <td>Required?</td>
            <td>false</td>
        </tr>
        <tr>
            <td>Position?</td>
            <td>named</td>
        </tr>
        <tr>
            <td>Default Value</td>
            <td></td>
        </tr>
        <tr>
            <td>Accept Pipeline Input?</td>
            <td>false</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
## Inputs 
 
The input type is the type of the objects that you can pipe to the cmdlet. 
 
* Sitecore.Data.Items.Item 
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* System.String 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 
 
This example consists of 2 scripts - a report that shows a list view and the second one that updates the list view from an action. 
 
```powershell   
 
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
 
``` 
 
## Related Topics 
 
* [Show-ListView](/appendix/commands/Show-ListView.md)* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>
