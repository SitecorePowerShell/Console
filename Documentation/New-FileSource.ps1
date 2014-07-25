<#
    .SYNOPSIS
        Creates new File source that can be added to a Sitecore package.        

    .DESCRIPTION
        Creates new File source that can be added to a Sitecore package. Folder provided as Root will be added as well as all of its content provided it matches the filters.

    .PARAMETER Name
        Name of the file source.

    .PARAMETER Root
        Root folder to include in the package

    .PARAMETER IncludeFilter
        Filter that defines which files will be included.

    .PARAMETER ExcludeFilter
        Filter that defines which files will NOT be included.
    
    .INPUTS
    
    .OUTPUTS
        Sitecore.Install.Files.FileSource

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Export-Package

    .LINK
        Get-Package

    .LINK
        Import-Package

    .LINK
        Install-UpdatePackage

    .LINK
        New-ExplicitFileSource

    .LINK
        New-ExplicitItemSource

    .LINK
        New-ItemSource

    .LINK
        New-Package

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .LINK
        http://blog.najmanowicz.com/2011/12/19/continuous-deployment-in-sitecore-with-powershell/

    .LINK
        https://gist.github.com/AdamNaj/f4251cb2645a1bfcddae

    .EXAMPLE
	# Following example creates a new package, adds content of the Console folder under the site folder
        # saves it in the Sitecore Package folder + gives you an option to download the saved package.

	# Create package
        $package = new-package "Sitecore PowerShell Extensions";

	# Set package metadata
        $package.Sources.Clear();

        $package.Metadata.Author = "Adam Najmanowicz - Cognifide, Michael West";
        $package.Metadata.Publisher = "Cognifide Limited";
        $package.Metadata.Version = "2.7";
        $package.Metadata.Readme = 'This text will be visible to people installing your package'
        
	# Add content of the Console folder in the site folder to the package
        $source = New-FileSource -Name "Console Assets" -Root "$AppPath\Console"
        $package.Sources.Add($source);

	# Save package
        Export-Package -Project $package -Path "$($package.Name)-$($package.Metadata.Version).zip" -Zip

	# Offer the user to download the package
        Download-File "$SitecorePackageFolder\$($package.Name)-$($package.Metadata.Version).zip"
#>
