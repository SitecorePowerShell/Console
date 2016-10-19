# Get-TaskSchedule 
 
Returns one or more task schedule items using the specified criteria. 
 
## Syntax 
 
Get-TaskSchedule -Item &lt;Item&gt; 
 
Get-TaskSchedule -Path &lt;String&gt; 
 
Get-TaskSchedule [[-Database] &lt;Database&gt;] [[-Name] &lt;String&gt;] 
 
 
## Detailed Description 
 
The Get-TaskSchedule command returns one or more task schedule items, based on name/database filter, path or simply converting a Sitecore item. 
 
© 2010-2016 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Item&nbsp; &lt;Item&gt; 
 
Task item to be converted. 
 
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
            <td>true (ByValue, ByPropertyName)</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
### -Path&nbsp; &lt;String&gt; 
 
Path to the item to be returned as Task Schedule. 
 
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
            <td>true (ByValue, ByPropertyName)</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
### -Database&nbsp; &lt;Database&gt; 
 
Database containing the task items to be returned. If not provided all databases will be considered for filtering using the "Name" parameter. 
 
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
            <td>2</td>
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
 
### -Name&nbsp; &lt;String&gt; 
 
Task filter - supports wildcards. Works with "Database" parameter to narrow tassk to only single database. 
 
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
            <td>1</td>
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
 
* Sitecore.Tasks.ScheduleItem 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 1 
 
 
 
```powershell   
 
PS master:\> Get-TaskSchedule
Name                             Database        Active   Auto Remove  Is Due   Expired  Completed    Last Run               Next Run
----                             --------        ------   -----------  ------   -------  ---------    --------               --------
__Task Schedule                  master          True     False        True     False    False        0001-01-01 00:00:00    0001-01-01 00:00:00
Check Bounced Messages           master          True     False        False    False    False        2014-07-29 10:18:43    2014-07-29 22:48:43
Check DSN Messages               master          True     False        False    False    False        2014-07-29 10:19:18    2014-07-29 22:49:18
Clean Confirmation IDs           master          True     False        False    False    False        2014-07-28 22:14:30    2014-07-31 02:14:30
Clean Message History            master          True     False        False    False    False        2014-07-29 10:19:18    2014-07-29 22:49:18
Close Outdated Connections       master          True     False        False    False    False        2014-07-29 12:30:22    2014-07-29 13:30:22
Test-PowerShell                  master          True     False        False    False    False        2014-07-28 14:30:06    2014-08-01 17:32:07
__Task Schedule                  web             True     False        True     False    False        0001-01-01 00:00:00    0001-01-01 00:00:00
Check Bounced Messages           web             True     False        True     False    False        2013-11-04 08:36:22    2013-11-04 21:06:22
Check DSN Messages               web             True     False        True     False    False        2013-11-04 08:36:22    2013-11-04 21:06:22
Clean Confirmation IDs           web             True     False        False    False    False        2013-11-04 08:36:22    2013-11-04 21:36:22
Clean Message History            web             True     False        True     False    False        2013-11-04 08:36:22    2013-11-04 21:06:22
Close Outdated Connections       web             True     False        True     False    False        2013-11-04 09:36:23    2013-11-04 10:36:23
Test-PowerShell                  web             True     False        True     False    False        2013-11-04 09:46:29    2013-11-04 09:46:30 
 
``` 
 
### EXAMPLE 2 
 
 
 
```powershell   
 
PS master:\> Get-TaskSchedule -Name "*Check*"
Name                             Database        Active   Auto Remove  Is Due   Expired  Completed    Last Run               Next Run
----                             --------        ------   -----------  ------   -------  ---------    --------               --------
Check Bounced Messages           master          True     False        False    False    False        2014-07-29 10:18:43    2014-07-29 22:48:43
Check DSN Messages               master          True     False        False    False    False        2014-07-29 10:19:18    2014-07-29 22:49:18
Check Bounced Messages           web             True     False        True     False    False        2013-11-04 08:36:22    2013-11-04 21:06:22
Check DSN Messages               web             True     False        True     False    False        2013-11-04 08:36:22    2013-11-04 21:06:22 
 
``` 
 
### EXAMPLE 3 
 
 
 
```powershell   
 
PS master:\> Get-TaskSchedule -Name "*Check*" -Database "master"
Name                             Database        Active   Auto Remove  Is Due   Expired  Completed    Last Run               Next Run
----                             --------        ------   -----------  ------   -------  ---------    --------               --------
Check Bounced Messages           master          True     False        False    False    False        2014-07-29 10:18:43    2014-07-29 22:48:43
Check DSN Messages               master          True     False        False    False    False        2014-07-29 10:19:18    2014-07-29 22:49:18 
 
``` 
 
## Related Topics 
 
* [Start-TaskSchedule](/appendix/commands/Start-TaskSchedule.md)* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>* <a href='https://www.youtube.com/watch?v=N3xgZcU9FqQ&list=PLph7ZchYd_nCypVZSNkudGwPFRqf1na0b&index=9' target='_blank'>https://www.youtube.com/watch?v=N3xgZcU9FqQ&list=PLph7ZchYd_nCypVZSNkudGwPFRqf1na0b&index=9</a><br/>
