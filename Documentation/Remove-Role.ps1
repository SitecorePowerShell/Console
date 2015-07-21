<#
    .SYNOPSIS
        Removes a Sitecore role.

    .DESCRIPTION
        The Remove-Role command removes a Sitecore role.

    .PARAMETER Identity
        Role name including domain. If no domain is specified - 'sitecore' will be used as the default value

    .PARAMETER Instance
        Role instance like that returned by the Get-Role command.

    .PARAMETER WhatIf
        Shows what would happen if the cmdlet runs. The cmdlet is not run.

    .PARAMETER Confirm
        Prompts you for confirmation before running the cmdlet.    
    
    .INPUTS
        Sitecore.Security.Accounts.Role
    
    .OUTPUTS
        None

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Remove-Role -Identity Michael
#>
