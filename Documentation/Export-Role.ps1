<#
    .SYNOPSIS
        Exports (serializes) Sitecore roles to the filesystem on the server.

    .DESCRIPTION
        The Export-Role command exports (serializes) Sitecore roles to the filesystem on the server.

    .PARAMETER Filter
        Specifies a simple pattern to match Sitecore roles.

        Examples:
        The following examples show how to use the filter syntax.

        To get all the roles, use the asterisk wildcard:
        Export-Role -Filter *

        To get all the roles in a domain use the following command:
        Export-Role -Filter "sitecore\*"

    .PARAMETER Identity
        Specifies the Sitecore role by providing one of the following values.

            Local Name
                Example: developer
            Fully Qualified Name
                Example: sitecore\developer

    .PARAMETER Role
        Specifies the role to be exported

    .PARAMETER Path
        Path to the file the role should be saved to.

    .PARAMETER Root
        Specifies the serialization root directory. If this parameter is not specified - the default Sitecore serialization folder will be used (unless you're saving to an explicit location with the -Path parameter).
    
    .INPUTS
        System.String
        Sitecore.Security.Accounts.Role
    
    .OUTPUTS
        System.String

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Import-Role

    .LINK
        Export-User

    .LINK
        Import-User

    .LINK
        Export-Item

    .LINK
        Import-Item

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Export-Role -Identity sitecore\Author

    .EXAMPLE
        PS master:\> Export-Role -Filter sitecore\*

    .EXAMPLE
        PS master:\> Export-Role -Root C:\my\Serialization\Folder\ -Filter *\*

    .EXAMPLE
        PS master:\> Export-Role -Path C:\my\Serialization\Folder\Authors.role -Identity sitecore\Author
#>
