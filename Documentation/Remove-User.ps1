<#
    .SYNOPSIS
        Removes the Sitecore user.

    .DESCRIPTION
        The Remove-User command removes a user from Sitecore.

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
        New-User

    .LINK
        Set-User

    .LINK
        Unlock-User

    .EXAMPLE
        PS master:\> Remove-User -Identity michael

    .EXAMPLE
        PS master:\> "michael","adam","mike" | Remove-User

    .EXAMPLE
        PS master:\> Get-User -Filter sitecore\m* | Remove-User

#>