<#
    .SYNOPSIS
        Returns current user's browser user agent.

    .DESCRIPTION
        Returns current user's browser user agent. Works only if Console is running outside of job. (e.g. in ISE - script needs to be run from the dropdown under the "Execute" button)    
    
    .INPUTS
    
    .OUTPUTS
        System.String

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> Get-UserAgent

        Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/36.0.1985.125 Safari/537.36
#>
