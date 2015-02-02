<#
    .SYNOPSIS
        Shows a user a confirmation request message box.

    .DESCRIPTION
        Shows a user a confirmation request message box.
        Returns "yes" or "no" based on user's response.
	The buttons that are shown to the user are "OK" and "Cancel".

    .PARAMETER Title
        Text to show the user in the dialog.
    
    .INPUTS
    
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
        PS master:\> Show-Confirm -Title "Do you like Sitecore PowerShell Extensions?"

        yes
#>
