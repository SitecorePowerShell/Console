<#
    .SYNOPSIS
        Shows a Sheer dialog with text results showing the output of the script or another control selected by the user based on either control name or Url to the control.

    .DESCRIPTION
        Shows a Sheer dialog with text results showing the output of the script or another control selected by the user based on either control name or Url to the control.


    .PARAMETER Control
	Name of the Sheer control to execute.        

    .PARAMETER Url
        Url to the Sheer control to execute.

    .PARAMETER Parameters
        Parameters to be passed to the executed control when executing with the -Control parameter specified.

    .PARAMETER Text
        Shows the default text dialog.

    .PARAMETER Title
        Title of the window containing the control.

    .PARAMETER Width
        Width of the window containing the control.

    .PARAMETER Height
        Height of the window containing the control.
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        System.String

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Read-Variable
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
        Show-ModalDialog
    .LINK
        Show-YesNoCancel
    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        #Show results of script execution
        PS master:\> Show-Result -Text

    .EXAMPLE
        #Show the Control Panel control in a Window of specified size.
        PS master:\> Show-Result -Control "ControlPanel" -Width 1024 -Height 640

    .EXAMPLE
        Shows a new instance of ISE
        Show-Result -Url "/sitecore/shell/Applications/PowerShell/PowerShellIse"

    .EXAMPLE
        
#>
