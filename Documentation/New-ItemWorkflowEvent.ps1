<#
    .SYNOPSIS
        Creates new entry in the history store notifying of workflow state change.

    .DESCRIPTION
        Creates new entry in the history store notifying of workflow state change.


    .PARAMETER OldState
        Id of the old state. If not provided - current item workflow state will be used.

    .PARAMETER NewState
        Id of the old state. If not provided - current item workflow state will be used.

    .PARAMETER Text
        Action comment.

    .PARAMETER Item
        The item to have the history event attached.

    .PARAMETER Path
        Path to the item to have the history event attached - additionally specify Language parameter to fetch different item language than the current user language.

    .PARAMETER Id
        Id of the the item to have the history event attached - additionally specify Language parameter to fetch different item language than the current user language.

    .PARAMETER Database
        Database containing the item to have the history event attached - can work with Language parameter to narrow the publication scope.

    .PARAMETER Language
        Language that will be used as source language. If not specified the current user language will be used. Globbing/wildcard supported.

    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Get-ItemWorkflowEvent

    .LINK
        Execute-Workflow

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> New-ItemWorkflowEvent -Path master:\content\home -lanuage "en" -Text "Just leaving a note"
#>
