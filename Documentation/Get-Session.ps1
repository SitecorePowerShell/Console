<#
    .SYNOPSIS
        Gets one or more Sitecore user sessions.

    .DESCRIPTION
        The Get-Session cmdlet gets user sessions in Sitecore.

        The Identity parameter specifies the Sitecore user to get. You can specify a user by its local name or fully qualified name.

    .PARAMETER InstanceId
        Specifies the Sitecore SessionID.

    .PARAMETER Identity
        Specifies the Sitecore user by providing one of the following values.

            Local Name
                Example: admin
            Fully Qualified Name
                Example: sitecore\admin

    .INPUTS
        None.
    
    .OUTPUTS
        Sitecore.Web.Authentication.DomainAccessGuard.Session
        Returns one or more user sessions.

    .NOTES
        Michael West

    .LINK
        http://michaellwest.blogspot.com

    .EXAMPLE
        PS master:\> Get-Session

        Created                LastRequest            SessionID                  UserName
        -------                -----------            ---------                  --------
        7/3/2014 3:30:39 PM    7/3/2014 3:44:27 PM    tekipna1lk0ccr2z1bdjsua2   sitecore\admin
        7/3/2014 4:13:55 PM    7/3/2014 4:13:55 PM    wq4bfivfm2tbgkgdccpyzczp   sitecore\michael

    .EXAMPLE
        PS master:\> Get-Session -Identity admin

        Created                LastRequest            SessionID                  UserName
        -------                -----------            ---------                  --------
        7/3/2014 3:30:39 PM    7/3/2014 3:44:27 PM    tekipna1lk0ccr2z1bdjsua2   sitecore\admin

    .EXAMPLE
        PS master:\> Get-Session -InstanceId tekipna1lk0ccr2z1bdjsua2,wq4bfivfm2tbgkgdccpyzczp

        Created                LastRequest            SessionID                  UserName
        -------                -----------            ---------                  --------
        7/3/2014 3:30:39 PM    7/3/2014 3:44:27 PM    tekipna1lk0ccr2z1bdjsua2   sitecore\admin

#>