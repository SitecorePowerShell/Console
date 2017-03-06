<#
    .SYNOPSIS
        Returns one or more Sitecore roles using the specified criteria.

    .DESCRIPTION
        The Get-Role command returns one or more Sitecore roles using the specified criteria.

        The Identity parameter specifies the Sitecore role to get. You can specify a role by its local name or fully qualified name.
        You can also specify role object variable, such as $<role>.

        To search for and retrieve more than one role, use the Filter parameter.

    .PARAMETER Filter
        Specifies a simple pattern to match Sitecore roles.

        Examples:
        The following examples show how to use the filter syntax.

        To get all the roles, use the asterisk wildcard:
        Get-Role -Filter *

        To get all the roles in a domain use the following command:
        Get-Role -Filter "sitecore\*"

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
        Sitecore.Security.Accounts.Role
        Returns one or more roles.

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .LINK
        Get-RoleMember       

    .EXAMPLE
        PS master:\> Get-Role -Identity sitecore\developer

        Name                                     Domain       IsEveryone
        ----                                     ------       ----------
        sitecore\developer                       sitecore     False

    .EXAMPLE
        PS master:\> "sitecore\developer","sitecore\author" | Get-Role

        Name                                     Domain       IsEveryone
        ----                                     ------       ----------
        sitecore\author                          sitecore     False
        sitecore\developer                       sitecore     False

    .EXAMPLE
        PS master:\> Get-Role -Filter sitecore\d*
 
        Name                                     Domain       IsEveryone
        ----                                     ------       ----------
        sitecore\Designer                        sitecore     False
        sitecore\Developer                       sitecore     False

    .EXAMPLE
        # Expand the MemberOf property to see a list of roles that the specified role is a member.
        PS master:\> Get-Role -Identity sitecore\developer | Select-Object -ExpandProperty MemberOf

        Name                                     Domain       IsEveryone
        ----                                     ------       ----------
        sitecore\Sitecore Client Configuring     sitecore     False
        sitecore\Sitecore Client Developing      sitecore     False
        sitecore\Designer                        sitecore     False
        sitecore\Author                          sitecore     False
        sitecore\Sitecore Client Maintaining     sitecore     False
#>