<#
    .SYNOPSIS
        Determines if the Sitecore role or user account exists.

    .DESCRIPTION
        The Test-Account command determines if a Sitecore role or user account exists.


    .PARAMETER Identity
        Role or User name including domain. If no domain is specified - 'sitecore' will be used as the default value

    .PARAMETER AccountType
        Specifies which account to check existence.

        - All
        - Role
        - User
    
    .INPUTS
        System.String
    
    .OUTPUTS
        True or False

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Test-Account -Identity Michael

        True
#>
