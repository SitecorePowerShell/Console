<#
    .SYNOPSIS
        Creates a new Sitecore role.

    .DESCRIPTION
        The New-Role command creates a new Sitecore role.

    .PARAMETER Identity
        Role name including domain. If no domain is specified - 'sitecore' will be used as the default value

    .PARAMETER PassThru
        Passes the processed object back into the pipeline.    
    
    .INPUTS
        System.String
    
    .OUTPUTS
        Sitecore.Security.Accounts.Role

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> New-Role -Identity Michael
#>
