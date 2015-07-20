<#
    .SYNOPSIS
        Test-Account.

    .DESCRIPTION
        Test-Account.


    .PARAMETER Identity
        User name including domain. If no domain is specified - 'sitecore' will be used as the default value

    .PARAMETER AccountType
        TODO: Provide description for this parameter    
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Security.Accounts.Account

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Test-Account -Path master:\content\home
#>
