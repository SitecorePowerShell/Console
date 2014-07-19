<#
    .SYNOPSIS
        Sets the Sitecore user password.

    .DESCRIPTION
        The Set-UserPassword cmdlet resets or changes a user password.

        The Identity parameter specifies the Sitecore user to remove. You can specify a user by its local name or fully qualified name.

    .PARAMETER Identity
        Specifies the Sitecore user by providing one of the following values.

            Local Name
                Example: admin
            Fully Qualified Name
                Example: sitecore\admin
    
    .INPUTS
        System.String
        Represents the identity of a user.

        Sitecore.Security.Accounts.User
        Represents the instance of a user.
    
    .OUTPUTS
        None.

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .LINK
        Get-User

    .LINK
        Set-User

    .EXAMPLE
        PS master:\> Set-UserPassword -Identity michael -NewPassword pass123 -OldPassword b

    .EXAMPLE
        PS master:\> "michael","adam","mike" | Set-UserPassword -NewPassword b -Reset

#>