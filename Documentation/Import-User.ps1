<#
    .SYNOPSIS
        Imports (deserializes) Sitecore users from the Sitecore server filesystem.

    .DESCRIPTION
        The Import-User command imports (deserializes) Sitecore users from the Sitecore server filesystem.

    .PARAMETER Identity
        Specifies the Sitecore user to be deserialized by providing one of the following values.

            Local Name
                Example: developer
            Fully Qualified Name
                Example: sitecore\developer

    .PARAMETER Filter
        Specifies a simple pattern to match Sitecore users.

        Examples:
        The following examples show how to use the filter syntax.

        To get all the roles, use the asterisk wildcard:
        Import-User -Filter *

        To get all the roles in a domain use the following command:
        Import-User -Filter "sitecore\*"

    .PARAMETER User
        User object retrieved from the Sitecore API or using the Get-User command identifying the user account to be deserialized.

    .PARAMETER Path
        Path to the file the user should be loaded from.

    .PARAMETER Root
        Specifies the serialization root directory. If this parameter is not specified - the default Sitecore serialization folder will be used (unless you're reading from an explicit location with the -Path parameter).
    
    .INPUTS
        System.String
        Sitecore.Security.Accounts.User
    
    .OUTPUTS        

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Import-User -Identity sitecore\Admin

    .EXAMPLE
        PS master:\> Import-User -Filter sitecore\*

    .EXAMPLE
        PS master:\> Import-User -Root C:\my\Serialization\Folder\ -Filter *\*

    .EXAMPLE
        PS master:\> Import-User -Path C:\my\Serialization\Folder\Admin.user
#>
