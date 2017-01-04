<#
    .SYNOPSIS
        Installs a Sitecore package from the specified path.

    .DESCRIPTION
        Installs Sitecore package with the ability to provide default responses for merge and overwrite actions. The alias for the command is Import-Package.

    .PARAMETER Path
        Path to the package file.

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
    
    .OUTPUTS       

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Export-Package

    .LINK
        Get-Package

    .LINK
        Install-UpdatePackage

    .LINK
        New-ExplicitFileSource

    .LINK
        New-ExplicitItemSource

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

    .LINK
        https://www.youtube.com/watch?v=60BGRDNONo0&list=PLph7ZchYd_nCypVZSNkudGwPFRqf1na0b&index=7

    .EXAMPLE
        PS master:\> Install-Package -Path SitecorePowerShellConsole.zip -InstallMode Merge -MergeMode Merge
#>
