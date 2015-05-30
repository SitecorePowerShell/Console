<#
    .SYNOPSIS
        Shows prompt message box asking user to provide a text string.

    .DESCRIPTION
        Shows prompt message box asking user to provide a text string.


    .PARAMETER Prompt
        Prompt message to show in the message box shown to a user.

    .PARAMETER DefaultValue
        Default value to be provided for the text box.

    .PARAMETER Validation
        Regex for value validation. If user enters a value that does not validate - en error message defined with the "ErrorMessage" parameter will be shown and user will be asked to enter the value again.

    .PARAMETER ErrorMessage
        Error message to show when regex validation fails.

    .PARAMETER MaxLength
        Maximum length of the string returned. If user enters a longer value - en error message will be shown and user will be asked to enter the value again.
    
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
        Show-Confirm
    .LINK
        Show-FieldEditor
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
        # Requests that the user provides an email, validates it against a regular expression snd whows an allert if the format is not valid
        PS master:\> Show-Input "Please provide your email" -DefaultValue "my@email.com"  -Validation "^[a-zA-Z0-9_-]+(?:\.[a-zA-Z0-9_-]+)*@(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?\.)+[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?$" -ErrorMessage "Not a proper email!"

    .EXAMPLE
        # Uses Show-Input command to request user a new name for the content item validating the proper characters are used and assigns the result to $newName variable (nothing gets changed)
        PS master:\> $contentItem = get-item master:\content
        PS master:\> $newName = Show-Input "Please provide the new name for the '$($contentItem.Name)' Item" -DefaultValue $contentItem.Name  -Validation "^[\w\*\$][\w\s\-\$]*(\(\d{1,}\)){0,1}$" -ErrorMessage "Invalid characters in the name"

        #print new name
        PS master:\> Write-Host "The new name you've chosen is '$($newName)'"

    .EXAMPLE
        # Requests that the user provides a string of at most  5 characters
        Show-Input "Please provide 5 characters at most" -MaxLength 5

#>
