<#
    .SYNOPSIS
        Pauses the script and shows an alert to the user.

    .DESCRIPTION
        Pauses the script and shows an alert specified in the -Title to the user. Once user clicks the OK button - script execution resumes.


    .PARAMETER Title
        Text to show the user in the alert dialog.
    
    .INPUTS
    
    .OUTPUTS       

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

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
        Show-Result
    .LINK
        Show-YesNoCancel
    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Show-Alert "Hello world."
#>
