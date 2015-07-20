<#
    .SYNOPSIS
        New-Role.

    .DESCRIPTION
        New-Role.


    .PARAMETER Identity
        User name including domain. If no domain is specified - 'sitecore' will be used as the default value

    .PARAMETER PassThru
        Passes the processed object back into the pipeline.    
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Security.Accounts.Role

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> New-Role -Path master:\content\home
#>
