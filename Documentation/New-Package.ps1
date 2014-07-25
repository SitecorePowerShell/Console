<#
    .SYNOPSIS
        Creates a new Sitecore install package object.

    .DESCRIPTION
        Creates a new Sitecore install package object that allows for further addition of items and files & further export to file.


    .PARAMETER Name
        Package name
    
    .INPUTS
    
    .OUTPUTS
        Sitecore.Install.PackageProject

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Export-Package

    .LINK
        Get-Package

    .LINK
        Import-Package

    .LINK
        New-ExplicitFileSource

    .LINK
        New-ExplicitItemSource

    .LINK
        New-FileSource

    .LINK
        New-ItemSource

    .LINK
        Install-UpdatePackage

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .LINK
        http://blog.najmanowicz.com/2011/12/19/continuous-deployment-in-sitecore-with-powershell/

    .LINK
        https://gist.github.com/AdamNaj/f4251cb2645a1bfcddae

    .EXAMPLE
	# Following example creates a new package, adds content/home item to it and 
        # saves it in the Sitecore Package folder+ gives you an option to download the saved package.

	# Create package
        $package = new-package "Sitecore PowerShell Extensions";

	# Set package metadata
        $package.Sources.Clear();

        $package.Metadata.Author = "Adam Najmanowicz - Cognifide, Michael West";
        $package.Metadata.Publisher = "Cognifide Limited";
        $package.Metadata.Version = "2.7";
        $package.Metadata.Readme = 'This text will be visible to people installing your package'
        
	# Add contnet/home to the package
	$source = Get-Item 'master:\content\home' | New-ItemSource -Name 'Home Page' -InstallMode Overwrite
	$package.Sources.Add($source);

	# Save package
        Export-Package -Project $package -Path "$($package.Name)-$($package.Metadata.Version).zip" -Zip

	# Offer the user to download the package
        Download-File "$SitecorePackageFolder\$($package.Name)-$($package.Metadata.Version).zip"

#>
