<#
    .SYNOPSIS
        Executes Sitecore Shell command for an item.

    .DESCRIPTION
        Executes Sitecore Shell command for an item. e.g. opening dialogs or performing commands that you can find in the Content Editor ribbon or context menu.

    .PARAMETER Name
        Name of the sitecore command e.g. "item:publishingviewer"

    .PARAMETER Item
        The item to be processed.

    .PARAMETER Path
        Path to the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Id
        Id of the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Database
        Database containing the item to be processed - can work with Language parameter to narrow the publication scope.

    .PARAMETER Language
        If specified - language that will be used as source language.
        
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
        PS master:\> Get-Item master:\content\home\ | Execute-ShellCommand "item:publishingviewer"

    .EXAMPLE
        # Initiate /sitecore/content/home item duplication.
        PS master:\> Get-Item master:/content/home | Execute-ShellCommand "item:duplicate"

    .EXAMPLE
        # Show properties of the /sitecore/content/home item.
        PS master:\> Get-Item master:/content/home | Execute-ShellCommand "contenteditor:properties"
#>
