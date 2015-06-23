<#
    .SYNOPSIS
        Creates a new domain with the specified name.

    .DESCRIPTION
        The New-Domain command creates a domain if it does not exist.


    .PARAMETER Name
        The name of the domain.

    .PARAMETER LocallyManaged
        TODO: Provide description for this parameter

    .PARAMETER PassThru
        Specifies the new domain should be passed into the pipeline.   
    
    .INPUTS
        System.String
        Represents the name of a domain.
    
    .OUTPUTS
        None.

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .LINK
        Get-Domain

    .LINK
        Remove-Domain

    .EXAMPLE
        PS master:\> New-Domain -Name "domainName"
#>
