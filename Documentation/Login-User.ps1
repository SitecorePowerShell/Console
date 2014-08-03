<#
    .SYNOPSIS
        Logs a user in and performs further script instructions in the context of the user.

    .DESCRIPTION
        Logs a user in and performs further script instructions in the context of the user.


    .PARAMETER Identity
        User name including domain. If no domain is specified - 'sitecore' will be used as the default value

    .PARAMETER Password
        Password for the account provided using the -Identity parameter.
    
    .INPUTS
    
    .OUTPUTS
        

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Login-User -Identity "sitecore\admin" -Password "b"
#>
