<#
    .SYNOPSIS
        Creates new Explicit Item Source that can be added to a Sitecore package.

    .DESCRIPTION
        Creates new Item source that can be added to a Sitecore package. This source only includes items explicitly added to it and not their children.


    .PARAMETER Item
        The item to be added to the source. If used in pipeline after e.g. Get-Item the source is created once all items are piped into it.

    .PARAMETER Name
        Name of the item source.

    .PARAMETER SkipVersions
        Add this parameter if you want to skip versions of the item from being included in the source and only include the version provided.

    .PARAMETER InstallMode
	Specifies what installer should do if the item already exists. Possible values:
        - Undefined - User will have to choose one of the below. But they probably don't really know what should be done so not a preferable option.
    	- Overwrite - All versions of the old item are removed and replaced with all versions of the new item. This option basically replaces the old item with new one.
    	- Merge - merge with existing item. How the item will be merged is defined with MergeMode parameter
	- Skip - All versions remains unchanged. Other languages remains unchanged. All children remains unchanged.
    	- SideBySide - all new item will be created.

    .PARAMETER MergeMode
	Specifies what installer should do if the item already exists and InstallMode is specified as Merge. Possible values:
        - Undefined - User will have to choose one of the below. But they probably don't really know what should be done so not a preferable option.
        - Clear - All versions of the old item are removed and replaced with all versions of the new item. This option basically replaces the old item with new one. Other language versions (those which are not in the package) are removed but only for items which are in the package. All child items which are not in the package keep other language versions. All child items which are in the package are changed.
        - Append - All versions of the new item are added on top of versions of the previous item. This option allows for further manual merge because all history is preserved, so user can see what was changed. Other languages remains unchanged. All child items which are not in the package keep other language versions. All child items which are in the package are changed.
        - Merge - All versions with the same number in both packages are replaced with versions from installed package. All versions which are in the package but not in the target are added. All versions which are not in the package but are in the target remains unchanged. This option also preserves history, however it might overwrite some of the changes. Other languages remains unchanged. All child items which are in the package are changed.

    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Install.Items.ExplicitItemSource

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
        New-FileSource

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
	# Following example creates a new package, adds content/home item and all of its children to it and 
        # saves it in the Sitecore Package folder + gives you an option to download the saved package.

	# Create package
        $package = new-package "Sitecore PowerShell Extensions";

	# Set package metadata
        $package.Sources.Clear();

        $package.Metadata.Author = "Adam Najmanowicz - Cognifide, Michael West";
        $package.Metadata.Publisher = "Cognifide Limited";
        $package.Metadata.Version = "2.7";
        $package.Metadata.Readme = 'This text will be visible to people installing your package'
        
	# Add content/home and all of its children to the package
	$source = Get-Item 'master:\content\home' | New-ExplicitItemSource -Name 'Home Page' -InstallMode Overwrite
	$package.Sources.Add($source);

	# Save package
        Export-Package -Project $package -Path "$($package.Name)-$($package.Metadata.Version).zip" -Zip

	# Offer the user to download the package
        Download-File "$SitecorePackageFolder\$($package.Name)-$($package.Metadata.Version).zip"
#>
