<#
    .SYNOPSIS
        Executes Sitecore Sheer application.

    .DESCRIPTION
        Executes Sitecore Sheer application, allows for passing additional parameters, launching it on desktop in cooperative or in Modal mode.


    .PARAMETER Application
        Name of the Application to be executed. Application must be defined in the Core databse.

    .PARAMETER Parameter
        Additional parameters passed to the application.

    .PARAMETER Icon
        Icon of the executed application (used for titlebar and in the Sitecore taskbar on the desktop)

    .PARAMETER Modal
        Causes the application to show in new browser modal window or modal overlay if used in Sitecore 7.2 or later.

    .PARAMETER Title
        Title of the window the app opens in.

    .PARAMETER Width
        Width of the modal window.

    .PARAMETER Height
        Height of the modal window.
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Read-Variable
    .LINK
        Show-Alert
    .LINK
        Show-Confirm
    .LINK
        Show-FieldEditor
    .LINK
        Show-Input
    .LINK
        Show-ListView
    .LINK
        Show-ModalDialog
    .LINK
        Show-Result
    .LINK
        Show-YesNoCancel
    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        #Show Content Editor in new window (or as an overlay in modal mode in Sitecore 7.2+) with "/sitecore/templates" item selected.
        $item = gi master:\templates
        
        Show-Application `
            -Application "Content Editor" `
            -Parameter @{id ="$($item.ID)"; fo="$($item.ID)";la="$($item.Language.Name)"; vs="$($item.Version.Number)";sc_content="$($item.Database.Name)"} `
            -Modal -Width 1600 -Height 800

    .EXAMPLE
        #Show Content Editor as a new application on desktop with "/sitecore/content/home" item selected.
        $item = gi master:\content\home
        
        Show-Application `
            -Application "Content Editor" `
            -Parameter @{id ="$($item.ID)"; fo="$($item.ID)";la="$($item.Language.Name)"; vs="$($item.Version.Number)";sc_content="$($item.Database.Name)"} `
#>
