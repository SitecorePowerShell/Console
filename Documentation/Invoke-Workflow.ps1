<#
    .SYNOPSIS
        Executes Workflow action for an item.
        This command used to be named Execute-Workflow - a matching alias added for compatibility with older scripts.

    .DESCRIPTION
        Executes Workflow action for an item. If the workflow action could not be performed for any reason - an appropriate error will be raised.


    .PARAMETER CommandName
        Namer of the workflow command.

    .PARAMETER Comments
        Comment to be saved in the history table for the action.

    .PARAMETER Item
        The item to have the workflow action executed.

    .PARAMETER Path
        Path to the item to have the workflow action executed - additionally specify Language parameter to fetch different item language than the current user language.

    .PARAMETER Id
        Id of the the item to have the workflow action executed - additionally specify Language parameter to fetch different item language than the current user language.

    .PARAMETER Database
        Database containing the item to have the workflow action executed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Language
        Language that will be used as source language. If not specified the current user language will be used. Globbing/wildcard supported.
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
	# Submit item to approval, item gotten from path
        PS master:\> Invoke-Workflow -Path master:/content/home -CommandName "Submit" -Comments "Automated"

    .EXAMPLE
	# Reject item, item gotten from pipeline
        PS master:\> Get-Item master:/content/home | Invoke-Workflow -CommandName "Reject" -Comments "Automated"
#>
