<#
    .SYNOPSIS
        Executes Sitecore Shell command for an item.
        This command used to be named Execute-ShellCommand - a matching alias added for compatibility with older scripts.

    .DESCRIPTION
        Executes Sitecore Shell command for an item. e.g. opening dialogs or performing commands that you can find in the Content Editor ribbon or context menu.

    .PARAMETER Name
        Name of the sitecore command e.g. "item:publishingviewer"

    .PARAMETER Item
        The item to be sent to the command.

    .PARAMETER Path
        Path to the item to be sent to the command - additionally specify Language parameter to fetch different item language than the current user language.

    .PARAMETER Id
        Id of the the item to be sent to the command - additionally specify Language parameter to fetch different item language than the current user language.

    .PARAMETER Database
        Database containing the item to be sent to the command - can work with Language parameter to narrow the publication scope.

    .PARAMETER Language
        Language that will be used as source language. If not specified the current user language will be used. Globbing/wildcard supported.
        
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Data.Items.Item

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
	# Launch Publishing Viewer for /sitecore/content/home item.
        PS master:\> Get-Item master:\content\home\ | Invoke-ShellCommand "item:publishingviewer"

    .EXAMPLE
        # Initiate /sitecore/content/home item duplication.
        PS master:\> Get-Item master:/content/home | Invoke-ShellCommand "item:duplicate"

    .EXAMPLE
        # Show properties of the /sitecore/content/home item.
        PS master:\> Get-Item master:/content/home | Invoke-ShellCommand "contenteditor:properties"
#>
