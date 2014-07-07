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

    .PARAMETER Email
        Specifies the Sitecore user email address. The value is validated for a properly formatted address.

    .PARAMETER StartUrl
        Specifies the url to navigate to once the user is logged in. The values are validated with a pretermined set.

    .PARAMETER Enabled
        Specifies whether the Sitecore user should be enabled.

    .PARAMETER IsAdministrator
        Specifies whether the Sitecore user should be classified as an Administrator. Parameter only available to Administrators.

    .PARAMETER CustomProperties
        Specifies a hashtable of custom properties to assign to the Sitecore user profile.
    
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
        PS master:\> Set-User -Identity michael -Email michaellwest@gmail.com

    .EXAMPLE
        PS master:\> "michael","adam","mike" | Set-User -Enable $false

    .EXAMPLE
        PS master:\> Get-User -Filter * | Set-User -Comment "Sitecore user"

    .EXAMPLE
        PS master:\> Set-User -Identity michael -CustomProperties @{"Date"=(Get-Date)}
        PS master:\>(Get-User michael).Profile.GetCustomProperty("Date")
        
        7/3/2014 4:40:02 PM

    .EXAMPLE
        PS master:\> Set-User -Identity michael -IsAdministrator -CustomProperties @{"HireDate"="03/17/2010"}
        PS master:\>$user = Get-User -Identity michael
        PS master:\>$user.Profile.GetCustomProperty("HireDate")
        
        03/17/2010

#>