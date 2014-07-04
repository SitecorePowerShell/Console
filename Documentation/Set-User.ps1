<#
    .SYNOPSIS
        Sets the Sitecore user properties.

    .DESCRIPTION
        The Set-User cmdlet sets a user profile properties in Sitecore.

        The Identity parameter specifies the Sitecore user to set. You can specify a user by its local name or fully qualified name.

    .PARAMETER Identity
        Specifies the Sitecore user by providing one of the following values.

            Local Name
                Example: admin
            Fully Qualified Name
                Example: sitecore\admin
    
    .INPUTS
        System.String
        Represents the identity of a user.

        Sitecore.Security.Accounts.User
        Represents the instance of a user.
    
    .OUTPUTS
        None.

    .NOTES
        Michael West

    .LINK
        http://michaellwest.blogspot.com

    .EXAMPLE
        PS master:\> Set-User -Email michaellwest@gmail.com

    .EXAMPLE
        PS master:\> "michael","adam","mike" | Set-User -Enable $false

    .EXAMPLE
        PS master:\> Get-User -Filter * | Set-User -Comment "Sitecore user"

    .EXAMPLE
        PS master:\> Set-User -Identity michael -CustomProperties @{"Date"=(Get-Date)}
        PS master:\>(Get-User michael).Profile.GetCustomProperty("Date")
        
        7/3/2014 4:40:02 PM

#>