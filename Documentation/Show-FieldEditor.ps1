<#
    .SYNOPSIS
        Shows Field editor for a provided item.

    .DESCRIPTION
        Shows Field editor for a provided item allows for editing all or selected list of fields.
        If user closes the dialog by pressing the "OK" button "ok" string will be returned. 
        Otherwise "cancel" will be returned.

    .PARAMETER Item
        The item to be edited.

    .PARAMETER Path
        Path to the item to be edited - additionally specify Language parameter to fetch different item language than the current user language.

    .PARAMETER Id
        Id of the the item to be edited - additionally specify Language parameter to fetch different item language than the current user language.

    .PARAMETER Database
        Database containing the item to be edited - can work with Language parameter to narrow the publication scope.

    .PARAMETER Language
        Language that will be edited. If not specified the current user language will be used. Globbing/wildcard supported.

    .PARAMETER PreserveSections
        If added this parameter tells editor to preserve the original item field sections, otherwise all fields are placed in a single section Named by SectionTitle parameter and having the SectionIcon icon.

    .PARAMETER SectionTitle
        If PreserveSections is not added to parameters - this parameter provides a title for the global section all fields are placed under.

    .PARAMETER SectionIcon
        If PreserveSections is not added to parameters - this parameter provides a iconfor the global section all fields are placed under.

    .PARAMETER IncludeStandardFields
        Add this parameter to add standard fields to the list that is being considered to be displayed

    .PARAMETER Name
        Array of names of the fields to be edited. 

        This parameter supports globbing so you can simply use "*" to allow editing of all fields. 
        If a field is prefixed with a dash - this field will be excluded from the list of fields.
        e.g. the following will display all fields except title from 
        Show-FieldEditor -Path "master:\content\home" -Name "*", "-Title"

    .PARAMETER Title
        Title of the dialog containing the field editor.

    .PARAMETER Width
        Width of the dialog containing the field editor.

    .PARAMETER Height
        Height of the dialog containing the field editor.
    
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
        # Show field editor that shows all non-standard fields on sitecore/content/home item except for field "title"
        # The dialog will be titled "My Home Item" all fields inside will be in single section.
        PS master:\> Show-FieldEditor -Path master:\content\home -Name "*" , "-Title" -Title "My Home Item" 

    .EXAMPLE
        # Show field editor that shows all fields including standard fields on sitecore/content/home
        # The dialog will preserve the item sections.
        PS master:\> Get-Item "master:\content\home" | Show-FieldEditor -Name "*" -IncludeStandardFields -PreserveSections
#>
