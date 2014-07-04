<#
    .SYNOPSIS
        Gets the Sitecore users in the role.

    .DESCRIPTION
        The Get-RoleMember cmdlet gets a role and returns the members of the Sitecore role.

        The Identity parameter specifies the Sitecore role to get. You can specify a role by its local name or fully qualified name.

    .PARAMETER Identity
        Specifies the Sitecore role by providing one of the following values.

            Local Name
                Example: developer
            Fully Qualified Name
                Example: sitecore\developer

    .INPUTS
        System.String
        Represents the identity of a role.
    
    .OUTPUTS
        Sitecore.Security.Accounts.User
        Returns one or more users.

        Sitecore.Security.Accounts.Role
        Returns one or more roles.

    .NOTES
        Michael West

    .LINK
        http://michaellwest.blogspot.com

    .EXAMPLE
        PS master:\> Get-RoleMember -Identity developer

        Name                     Domain       IsAdministrator IsAuthenticated
        ----                     ------       --------------- ---------------
        sitecore\michael         sitecore     False           False

    .EXAMPLE
        PS master:\> Get-RoleMember -Identity author

        Name                     Domain       IsAdministrator IsAuthenticated
        ----                     ------       --------------- ---------------
        sitecore\michael         sitecore     False           False
 
        Domain      : sitecore
        IsEveryone  : False
        IsGlobal    : False
        AccountType : Role
        Description : Role
        DisplayName : sitecore\Developer
        LocalName   : sitecore\Developer
        Name        : sitecore\Developer
#>