<#
    .SYNOPSIS
        Loads package from the package definition (xml file).

    .DESCRIPTION
        Loads package from the package definition (xml file). Package definitions can be created by PowerShell scripts by using Export-Package commandlet (without the -Zip parameter)


    .PARAMETER Path
        Path to the package file. If the path is not absolute the path needs to be relative to the Sitecore Package path defined in the "PackagePath" setting and later exposed in the Sitecore.Shell.Applications.Install.PackageProjectPath
    
    .INPUTS
    
    .OUTPUTS
        Sitecore.Install.PackageProject

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Export-Package

    .LINK
        Import-Package

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
        PS master:\> Get-Package -Path master:\content\home
#>
