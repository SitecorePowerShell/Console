<#
    .SYNOPSIS
        Writes text to the Sitecore event log.

    .DESCRIPTION
        The Write-Log cmdlet writes text to the Sitecore event log with the specified logging level.

    .PARAMETER Log
        Specifies the Sitecore logging level.

    .PARAMETER Object
        Specifies the object to write to the log.

    .PARAMETER Separator
        Strings the output together with the specified text.

    .INPUTS
        System.String
        Represents the identity of a role.
    
    .OUTPUTS
        None.

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Write-Log "Information."
#>