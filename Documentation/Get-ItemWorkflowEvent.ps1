<#
    .SYNOPSIS
        Returns entries from the history store notifying of workflow state change for the specified item.

    .DESCRIPTION
        The Get-ItemWorkflowEvent command returns entries from the history store notifying of workflow state change for the specified item.

    .PARAMETER Identity
        User that has been associated with the enteries. Wildcards are supported.

    .PARAMETER Item
        The item to have its history items returned.

    .PARAMETER Path
        Path to the item to have its history items returned - additionally specify Language parameter to fetch different item language than the current user language.

    .PARAMETER Id
        Id of the the item to have its history items returned - additionally specify Language parameter to fetch different item language than the current user language.

    .PARAMETER Database
        Database containing the item to have its history items returned - can work with Language parameter to narrow the publication scope.

    .PARAMETER Language
        Language that will be used as source language. If not specified the current user language will be used. Globbing/wildcard supported.

    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Workflows.WorkflowEvent

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        New-ItemWorkflowEvent

    .LINK
        Execute-Workflow

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Get-ItemWorkflowEvent -Path master:\content\home
        Date     : 2014-07-27 14:23:33
        NewState : {190B1C84-F1BE-47ED-AA41-F42193D9C8FC}
        OldState : {46DA5376-10DC-4B66-B464-AFDAA29DE84F}
        Text     : Automated
        User     : sitecore\admin
        
        Date     : 2014-08-01 15:43:29
        NewState : {190B1C84-F1BE-47ED-AA41-F42193D9C8FC}
        OldState : {190B1C84-F1BE-47ED-AA41-F42193D9C8FC}
        Text     : Just leaving a note
        User     : sitecore\admin
#>
