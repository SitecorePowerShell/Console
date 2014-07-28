<#
    .SYNOPSIS
        Shows Sitecore Sheer control as a modal dialog.

    .DESCRIPTION
        Shows Sitecore Sheer control as a modal dialog. If control returns a value - the value will be passed back as the result of the commandlet execution.

    .PARAMETER Control
        Name of the Sitecore Sheer control to show

    .PARAMETER Url
        A fully formed URL that constitutes a control execution request.

    .PARAMETER Parameters
        Hashtable of parameters to pass to the control in the url.

    .PARAMETER Title
        Title of the control dialog.

    .PARAMETER Width
        Width of the control dialog.

    .PARAMETER Height
        Height of the control dialog.
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        System.String

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

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
        Show-ListView
    .LINK
        Show-Result
    .LINK
        Show-YesNoCancel
    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Show-ModalDialog -Control "ConfirmChoice" -Parameters @{btn_0="Yes (returns btn_0)"; btn_1="No (returns btn_1)"; btn_2="return btn_2"; te="Message Text"; cp="My Caption"} -Height 120 -Width 400
#>
