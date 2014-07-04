<#
    .SYNOPSIS
        Adds one or more Sitecore users to the specified role.

    .DESCRIPTION
        The Add-RoleMember cmdlet gets a role and assigns users as members of the Sitecore role.

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
        None.

    .NOTES
        Michael West

    .LINK
        http://michaellwest.blogspot.com

    .EXAMPLE
        PS master:\> Add-RoleMember -Identity developer -Members "michael","adam","mike"

#>