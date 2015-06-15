<#
    .SYNOPSIS
        Creates new User & Role source that can be added to a Sitecore package.

    .DESCRIPTION
        Creates new User & Role source that can be added to a Sitecore package.

    .PARAMETER Account
        User or Role provided from e.g. Get-Role or Get-User Cmdlet.

    .PARAMETER Identity
        User or role name including domain for which the access rule is being created. If no domain is specified - 'sitecore' will be used as the default domain.

        Specifies the Sitecore user by providing one of the following values.

            Local Name
                Example: adam
            Fully Qualified Name
                Example: sitecore\adam

        if -AccountType parameter is specified as Role - only roles will be taken into consideration.
        if -AccountType parameter is specified as User - only users will be taken into consideration.

    .PARAMETER Filter
        Specifies a simple pattern to match Sitecore roles & users.

        Examples:
        The following examples show how to use the filter syntax.

        To get security for all roles, use the asterisk wildcard:
        Get-ItemAcl -Filter *

        To security got all roles in a domain use the following command:
        Get-ItemAcl -Filter "sitecore\*"

        if -AccountType parameter is specified as Role - only roles will be taken into consideration.
        if -AccountType parameter is specified as User - only users will be taken into consideration.

    .PARAMETER AccountType
        - Unknown - Both Roles and users will be taken into consideration when looking for accounts through either -Identity or -Filter parameters
    	- Role - Only Roles will be taken into consideration when looking for accounts through either -Identity or -Filter parameters
	- User - Only Users will be taken into consideration when looking for accounts through either -Identity or -Filter parameters

    .PARAMETER Name
        Name of the security source.
    
    .INPUTS
        Sitecore.Security.Accounts.Account
    
    .OUTPUTS
        Sitecore.Install.Security.SecuritySource

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
	# Following example creates a new package, adds sitecore\admin user to it and 
        # saves it in the Sitecore Package folder+ gives you an option to download the saved package.

	# Create package
        $package = new-package "Sitecore PowerShell Extensions";

	# Set package metadata
        $package.Sources.Clear();

        $package.Metadata.Author = "Adam Najmanowicz - Cognifide, Michael West";
        $package.Metadata.Publisher = "Cognifide Limited";
        $package.Metadata.Version = "2.7";
        $package.Metadata.Readme = 'This text will be visible to people installing your package'
        
        # Create security source with Sitecore Administrator only
        $source = New-SecuritySource -Identity sitecore\admin -Name "Sitecore Admin" 
	$package.Sources.Add($source);

	# Save package
        Export-Package -Project $package -Path "$($package.Name)-$($package.Metadata.Version).zip" -Zip

	# Offer the user to download the package
        Download-File "$SitecorePackageFolder\$($package.Name)-$($package.Metadata.Version).zip"

    .EXAMPLE
	# Following example creates a new package, adds all roles within the "sitecore" domain to it and 
        # saves it in the Sitecore Package folder+ gives you an option to download the saved package.

	# Create package
        $package = new-package "Sitecore PowerShell Extensions";

	# Set package metadata
        $package.Sources.Clear();

        $package.Metadata.Author = "Adam Najmanowicz - Cognifide, Michael West";
        $package.Metadata.Publisher = "Cognifide Limited";
        $package.Metadata.Version = "2.7";
        $package.Metadata.Readme = 'This text will be visible to people installing your package'
        
        # Create security source with all roles within the sitecore domain
        $source = New-SecuritySource -Filter sitecore\* -Name "Sitecore Roles" -AccountType Role
	$package.Sources.Add($source);

	# Save package
        Export-Package -Project $package -Path "$($package.Name)-$($package.Metadata.Version).zip" -Zip

	# Offer the user to download the package
        Download-File "$SitecorePackageFolder\$($package.Name)-$($package.Metadata.Version).zip"

#>
