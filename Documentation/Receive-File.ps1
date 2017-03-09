<#
    .SYNOPSIS
        Shows a dialog to users allowing to upload files to either server file system or items in media library.

    .DESCRIPTION
        Executing this command with file path on the server (provided as -Path parameter) provides script users with means to upload a file from their computer.
	Executing it for an Item located in Sitecore Media library (provided as -ParentItem) allows the user to upload the file as a child to that item.
	If the file has been uploaded the dialog returns path to the file (in case of file system storage) or Item that has been created if the file was uplaoded to media library.

    .PARAMETER Path
        Path to the folder where uploaded file should be stored.

    .PARAMETER ParentItem
        The item under which the uploaded media items should be stored.

    .PARAMETER Description
        Dialog description displayed below the dialog title.

    .PARAMETER CancelButtonName
        Text shown on the cancel button.

    .PARAMETER OkButtonName
        Text shown on the OK button.

    .PARAMETER Title
        Dialog title - shown at the top of the dialog.

    .PARAMETER Width
        Dialog width.

    .PARAMETER Height
        Dialog width.
    
    .PARAMETER Versioned
        Indicates that the Media item should be created as a Versioned media item.

    .PARAMETER Language
        Specifies the language in which the media item should be created. if not specified - context language is selected.

    .PARAMETER Overwrite
        indicates that the upload should overwrite a file or a media item if that one exists. Otherwise a file with a non-confilicting name or a sibling media item is created.

    .PARAMETER Unpack
        Indicates that the uplaod is expected to be a ZIP file which should be unpacked when it's received.

    .PARAMETER AdvancedDialog
        Shows advanced dialog where user can upload multiple media items and select if the uploaded items are versioned, overwritten and unpacked.

    .INPUTS
        Sitecore.Data.Items.Item
        System.String
    
    .OUTPUTS
        Sitecore.Data.Items.Item
        System.String

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Send-File

    .LINK
        Out-Download

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        # Upload text.txt file to server disk drive.
        # A new file is created with a non-conflicting name and the path to it is returned
        PS master:\> Receive-File -Path "C:\temp\upload"
	C:\temp\upload\text_029.txt
    .EXAMPLE
        # Upload text.txt file to media library under the 'master:\media library\Files' item
        # A new media item is created and returned
        PS master:\> Receive-File -ParentItem (get-item "master:\media library\Files") 
        Name Children Languages Id                                     TemplateName
        ---- -------- --------- --                                     ------------
        text False    {en}      {7B11CE12-C0FC-4650-916C-2FC76F3DCAAF} File

    .EXAMPLE
        # Upload text.txt file to media library under the 'master:\media library\Files' item using advanced dialog.
        # A new media item is created but "undetermined" is returned as the dialog does not return the results.
        PS master:\> Receive-File (get-item "master:\media library\Files") -AdvancedDialog
        undetermined

    .EXAMPLE
        # Upload text.txt file to media library under the 'master:\media library\Files' item.
        # A new versioned media item in Danish language is created and returned. If the media item existed - it will be overwritten.
        PS master:\> Receive-File -ParentItem (get-item "master:\media library\Files") -Overwrite -Language "da" -Versioned
        Name Children Languages Id                                     TemplateName
        ---- -------- --------- --                                     ------------
        text False    {en, da}  {307BCF7D-27FD-46FC-BE83-D9ED640CB09F} File


#>
