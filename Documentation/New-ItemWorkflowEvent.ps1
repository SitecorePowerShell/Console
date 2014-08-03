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
        Path to the item to have the history event attached - can work with Language parameter to narrow the publication scope.

    .PARAMETER Id
        Id of the item to have the history event attached - can work with Language parameter to narrow the publication scope.

    .PARAMETER Database
        Database containing the item to have the history event attached - can work with Language parameter to narrow the publication scope.

    .PARAMETER Language
        If you need the item in specific Language You can specify it with this parameter. Globbing/wildcard supported.
    
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
