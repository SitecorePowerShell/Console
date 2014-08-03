<#
    .SYNOPSIS
        Sends output to an interactive table in a separate window.

    .DESCRIPTION
        The Show-ListView cmdlet sends the output from a command to a grid view window where the output is displayed in an interactive table.
        Because this cmdlet requires a user interface, it does not work in a non-interactive scenarios like within web service calls.
        You can use the following features of the table to examine your data:
        -- Sort. To sort the data, click a column header. Click again to toggle from ascending to descending order.
        -- Quick Filter. Use the "Filter" box at the top of the window to search the text in the table. You can search for text in a particular column, search for literals, and search for multiple words.
        -- Execute actions on selected items. To execute action on the data from Show-ListView, Ctrl+click the items you want to be included in the action and press the desired action in the "Actions" chunk in the ribbon.
        -- Export contents of the view in XML, CSV, Json, HTML or Excel file and download that onto the user's computer. The downloaded results will take into account current filter and order of the items.

    .PARAMETER PageSize
        Number of results shown per page.

    .PARAMETER Icon
        Icon of the result window. (Shows in the top/left corner and on the Sitecore taskbar).

    .PARAMETER InfoTitle
        Title on the panel that appears below the ribbon in the results window.

    .PARAMETER InfoDescription
        Description that appears on the panel below the ribbon in the results window.

    .PARAMETER Modal
        If this parameter is provided Results will show in a new window (in Sitecore 6.x up till Sitecore 7.1) or in a modal overlay (Sitecore 7.2+)

    .PARAMETER ActionData
        Additional data what will be passed to the view. All actions that are executed from that view window will have that data accessible to them as $actionData variable.

    .PARAMETER ViewName
        View signature name - this can be used by action commandlets to determine whether to show an action or not using the Show/Enable rules.

    .PARAMETER ActionsInSession
        If this parameter is specified actions will be executed in the same session as the one in which the commandlet is executed. 

    .PARAMETER Data
        Data to be displayed in the view.

    .PARAMETER Property
        Specifies the object properties that appear in the display and the order in which they appear. Type one or more property names (separated by commas), or use a hash table to display a calculated property.        

	The value of the Property parameter can be a new calculated property. To create a calculated, property, use a hash table. Valid keys are:
	-- Name (or Label) <string>
	-- Expression <string> or <script block>

    .PARAMETER Title
        Title of the results window.

    .PARAMETER Width
        Width of the results window.

    .PARAMETER Height
        Height of the results window.
    
    .INPUTS
        System.Management.Automation.PSObject
    
    .OUTPUTS
        System.String

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Update-ListView
    .LINK
        Out-GridView
    .LINK
        Format-Table
    .LINK
        Show-Alert
    .LINK
        Show-Application
    .LINK
        Show-Confirm
    .LINK
        Show-FieldEditor
    .LINK
        Show-Input
    .LINK
        Show-ModalDialog
    .LINK
        Show-Result
    .LINK
        Show-YesNoCancel
    .LINK
        http://michaellwest.blogspot.com/2014/04/sitecore-code-editor-14-preview.html
    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        # This command formats information about Sitecore items in a table. The Get-ChildItem cmdlet gets objects representing the items. 
        # The pipeline operator (|) passes the object to the Show-ListView command. Show-ListView displays the objects in a table.
        PS master:\> Get-Item -path master:\* | Show-ListView -Property Name, DisplayName, ProviderPath, TemplateName, Language

    .EXAMPLE
        # This command formats information about Sitecore items in a table. The Get-ItemReferrer cmdlet gets all references of the "Sample Item" template. 
        # The pipeline operator (|) passes the object to the Show-ListView command. Show-ListView displays the objects in a table.
        # The Properties are not displaying straight properties but use the Name/Expression scheme to provide a nicely named values that 
        # like in the case of languages which are aggregarde form the "Languages" property.
        PS master:\> Get-ItemReferrer -path 'master:\templates\Sample\Sample Item' | 
                         Show-ListView -Property `
                             @{Label="Name"; Expression={$_.DisplayName} }, 
                             @{Label="Path"; Expression={$_.Paths.Path} }, 
                             @{Label="Languages"; Expression={$_.Languages | % { $_.Name + ", "} } }
#>
