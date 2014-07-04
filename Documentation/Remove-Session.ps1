<#
    .SYNOPSIS
        Removes one or more Sitecore user sessions.

    .DESCRIPTION
        The Remove-Session cmdlet removes user sessions in Sitecore.

    .PARAMETER InstanceId
        Specifies the Sitecore SessionID.

    .PARAMETER Instance
        Specifies the Sitecore user sessions.

    .INPUTS
        Sitecore.Web.Authentication.DomainAccessGuard.Session
        Accepts a user session.
    
    .OUTPUTS
        None.

    .NOTES
        Michael West

    .LINK
        http://michaellwest.blogspot.com

    .EXAMPLE
        PS master:\> Remove-Session -InstanceId tekipna1lk0ccr2z1bdjsua2,wq4bfivfm2tbgkgdccpyzczp

    .EXAMPLE
        PS master:\> Get-Session -Identity michael | Remove-Session

#>