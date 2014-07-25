<#
    .SYNOPSIS
        Shows Yes/No/Cancel dialog to the user and returns user choice.

    .DESCRIPTION
        Shows Yes/No/Cancel dialog to the user and returns user choice as a string value.

	Depending on the user response one of the 2 strings is returned:
        - yes
        - no
        - cancel

    .PARAMETER Title
        Question to ask the user in the dialog

    .PARAMETER Width
        Width of the dialog.

    .PARAMETER Height
        Height of the dialog.
    
    .INPUTS
    
    .OUTPUTS
        System.String

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Show-YesNoCancel "Should we delete those 2 items?"
        
        cancel
#>
