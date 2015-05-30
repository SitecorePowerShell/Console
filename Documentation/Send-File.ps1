<#
    .SYNOPSIS
        Allows users to download files from server and file items from media library.

    .DESCRIPTION
        Executing this command with file path on the server provides script users with means to download a file to their computer.
	Executing it for an Item located in Sitecore Media library allows the user to download the blob stored in that item.
	If the file has been downloaded the dialog returns "downloaded" string, otherwise "cancelled" is returned.

    .PARAMETER Path
        Path to the file to be downloaded. The file has to exist in the Data folder. Files from outside the Data folder are not downloadable.

    .PARAMETER Message
        Message to show the user in the download dialog.

    .PARAMETER Item
        The item to be downloaded.

    .PARAMETER NoDialog
        If this parameter is used the Dialog will not be shown but instead the file download will begin immediately.

    .PARAMETER Title
        Download dialog title.

    .PARAMETER Width
        Download dialog width.        

    .PARAMETER Height
        Download dialog height.
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        System.String

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        #Download File from server disk drive
        PS master:\> Send-File -Path "C:\Projects\ZenGarden\Data\packages\Sitecore PowerShell Extensions-2.6.zip"

    .EXAMPLE
        #Download item from media library
        PS master:\> Get-Item "master:/media library/Showcase/cognifide_logo" | Send-File -Message "Cognifide Logo"

#>
