<#
    .SYNOPSIS
        Export (serialize) a Sitecore user to the filesystem on the server.

    .DESCRIPTION
        The Export-User command serializes a Sitecore user to the filesystem on the server.

        The Identity parameter specifies the Sitecore user to get. You can specify a user by its local name or fully qualified name.
        You can also specify user object variable, such as $<user>.

        To search for and retrieve more than one user, use the Filter parameter.

	You can also pipe a user from the Get-user commandlet.


    .PARAMETER Identity
        Specifies the Sitecore user by providing one of the following values.

            Local Name
                Example: admin
            Fully Qualified Name
                Example: sitecore\admin

    .PARAMETER Filter
        Specifies a simple pattern to match Sitecore users.

        Examples:
        The following examples show how to use the filter syntax.

        To get all the users, use the asterisk wildcard:
        Export-User -Filter *

        To get all the users in a domain use the following command:
        Export-User -Filter "sitecore\*"

    .PARAMETER User
        User object retrieved from the Sitecore API or using the Get-User commandlet.

    .PARAMETER Current
        Specifies that the current user should be serialized.

    .PARAMETER Path
        Path to the file the user should be saved to.

    .PARAMETER Root
        Overrides Sitecore Serialization root directory
    
    .INPUTS
        Sitecore.Security.Accounts.User
    
    .OUTPUTS        

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Export-Role
    
    .LINK
        Import-User

    .LINK
        Export-Item
    
    .LINK
        Import-Role

    .LINK
        Import-Item

    .LINK
        Get-User

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Export-User -Identify sitecore\admin
#>
