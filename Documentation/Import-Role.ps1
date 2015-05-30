<#
    .SYNOPSIS
        Imports (deserializes) Sitecore roles from the Sitecore server filesystem.

    .DESCRIPTION
        The Import-Role command imports (deserializes) Sitecore roles from the Sitecore server filesystem.


    .PARAMETER Identity
        Specifies the Sitecore role to be deserialized by providing one of the following values.

            Local Name
                Example: developer
            Fully Qualified Name
                Example: sitecore\developer

    .PARAMETER Filter
        Specifies a simple pattern to match Sitecore roles.

        Examples:
        The following examples show how to use the filter syntax.

        To get all the roles, use the asterisk wildcard:
        Import-Role -Filter *

        To get all the roles in a domain use the following command:
        Import-Role -Filter "sitecore\*"

    .PARAMETER Role
        An existing role object to be restored to the version from disk

    .PARAMETER Path
        Path to the file the role should be loaded from.

    .PARAMETER Root
        Specifies the serialization root directory. If this parameter is not specified - the default Sitecore serialization folder will be used (unless you're reading from an explicit location with the -Path parameter).
    
    .INPUTS
        System.String
        Sitecore.Security.Accounts.Role
    
    .OUTPUTS
        

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Import-Role -Identity sitecore\Author

    .EXAMPLE
        PS master:\> Import-Role -Filter sitecore\*

    .EXAMPLE
        PS master:\> Import-Role -Root C:\my\Serialization\Folder\ -Filter *\*

    .EXAMPLE
        PS master:\> Import-Role -Path C:\my\Serialization\Folder\Admins.role

#>
