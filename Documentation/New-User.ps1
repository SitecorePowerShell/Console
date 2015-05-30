<#
    .SYNOPSIS
        Creates a new Sitecore user.

    .DESCRIPTION
        The New-User command creates a new user in Sitecore.

        The Identity parameter specifies the Sitecore user to create. You can specify a user by its local name or fully qualified name.

    .PARAMETER Identity
        Specifies the Sitecore user by providing one of the following values.

            Local Name
                Example: developer
            Fully Qualified Name
                Example: sitecore\developer

    .PARAMETER Enabled
        Specifies that the account should be enabled. When enabled, the Password parameter is required.

    .INPUTS
        System.String
        Represents the identity of a role.
    
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

    .LINK
        Remove-User

    .LINK
        Unlock-User

    .EXAMPLE
        PS master:\> New-User -Identity michael

    .EXAMPLE
        PS master:\> New-User -Identity michael -Enabled -Password b -Email michaellwest@gmail.com -FullName "Michael West"

    .EXAMPLE
        PS master:\> New-User -Identity michael -PassThru
 
        Name                     Domain       IsAdministrator IsAuthenticated
        ----                     ------       --------------- ---------------
        sitecore\michael2        sitecore     False           False
#>