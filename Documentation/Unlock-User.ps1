<#
    .SYNOPSIS
        Unlocks a Sitecore user using the specified criteria.

    .DESCRIPTION
        The Unlock-User command gets a user and unlocks the account in Sitecore.

        The Identity parameter specifies the Sitecore user to get. You can specify a user by its local name or fully qualified name.
        You can also specify user object variable, such as $<user>.

    .PARAMETER Instance
        Specifies the Sitecore user by providing an instance of a user.

    .INPUTS
        System.String
        Represents the identity of a user.

        Sitecore.Security.Accounts.User
        One or more user instances.
    
    .OUTPUTS
        None.

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .LINK
        Get-User

    .LINK
        New-User

    .LINK
        Remove-User

    .LINK
        Set-User

    .EXAMPLE
        PS master:\> Unlock-User -Identity michael

    .EXAMPLE
        PS master:\> Get-User -Filter * | Unlock-User

#>
