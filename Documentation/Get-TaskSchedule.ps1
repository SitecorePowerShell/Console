<#
    .SYNOPSIS
        Returns task schedule item.

    .DESCRIPTION
        Returns task schedule item, based on name/database filter, path or simply converting a Sitecore item.


    .PARAMETER Item
        Task item to be converted.

    .PARAMETER Path
        Path to the item to be returned as Task Schedule.

    .PARAMETER Database
        Database containing the task items to be returned. If not provided all databases will be considered for filtering using the "Name" parameter.

    .PARAMETER Name
        Task filter - supports wildcards. Works with "Database" parameter to narrow tassk to only single database.
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Tasks.ScheduleItem

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
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

    .EXAMPLE
        PS master:\> Get-TaskSchedule -Name "*Check*"
        Name                             Database        Active   Auto Remove  Is Due   Expired  Completed    Last Run               Next Run
        ----                             --------        ------   -----------  ------   -------  ---------    --------               --------
        Check Bounced Messages           master          True     False        False    False    False        2014-07-29 10:18:43    2014-07-29 22:48:43
        Check DSN Messages               master          True     False        False    False    False        2014-07-29 10:19:18    2014-07-29 22:49:18
        Check Bounced Messages           web             True     False        True     False    False        2013-11-04 08:36:22    2013-11-04 21:06:22
        Check DSN Messages               web             True     False        True     False    False        2013-11-04 08:36:22    2013-11-04 21:06:22

    .EXAMPLE
        PS master:\> Get-TaskSchedule -Name "*Check*" -Database "master"
        Name                             Database        Active   Auto Remove  Is Due   Expired  Completed    Last Run               Next Run
        ----                             --------        ------   -----------  ------   -------  ---------    --------               --------
        Check Bounced Messages           master          True     False        False    False    False        2014-07-29 10:18:43    2014-07-29 22:48:43
        Check DSN Messages               master          True     False        False    False    False        2014-07-29 10:19:18    2014-07-29 22:49:18

#>
