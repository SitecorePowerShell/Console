<#
    .SYNOPSIS
        Gets one or more Sitecore users.

    .DESCRIPTION
        The Get-User cmdlet gets a user or performs a search to retrieve multiple users from Sitecore.

        The Identity parameter specifies the Sitecore user to get. You can specify a user by its local name or fully qualified name.
        You can also specify user object variable, such as $<user>.

        To search for and retrieve more than one user, use the Filter parameter.

    .PARAMETER Filter
        Specifies a simple pattern to match Sitecore users.

        Examples:
        The following examples show how to use the filter syntax.

        To get all the users, use the asterisk wildcard:  
        
            Get-User -Filter *

        To get all the users in a domain use the following command:  
        
            Get-User -Filter "sitecore\*"

    .PARAMETER Identity
        Specifies the Sitecore user by providing one of the following values.

        Local Name:
          
            admin
        
        Fully Qualified Name:
        
            sitecore\admin  

    .INPUTS
        System.String
        Represents the identity of a user.
    
    .OUTPUTS
        Sitecore.Security.Accounts.User
        Returns one or more users.

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .LINK
        Set-User

    .LINK
        New-User

    .LINK
        Remove-User

    .LINK
        Unlock-User

    .EXAMPLE
        PS master:\> Get-User -Identity admin

        Name                     Domain       IsAdministrator IsAuthenticated
        ----                     ------       --------------- ---------------
        sitecore\admin           sitecore     True            False

    .EXAMPLE
        PS master:\> "admin","michael" | Get-User

        Name                     Domain       IsAdministrator IsAuthenticated
        ----                     ------       --------------- ---------------
        sitecore\Admin           sitecore     True            False
        sitecore\michael         sitecore     False           False

    .EXAMPLE
        PS master:\> Get-User -Filter *
 
        Name                     Domain       IsAdministrator IsAuthenticated
        ----                     ------       --------------- ---------------
        default\Anonymous        default      False           False
        extranet\Anonymous       extranet     False           False
        sitecore\Admin           sitecore     True            False
        sitecore\michael         sitecore     False           False

    .EXAMPLE
        PS master:\> Get-User -Filter "michaellwest@*.com"
 
        Name                     Domain       IsAdministrator IsAuthenticated
        ----                     ------       --------------- ---------------
        sitecore\michael         sitecore     False           False
#>