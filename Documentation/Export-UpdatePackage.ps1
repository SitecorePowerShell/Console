<#
    .SYNOPSIS
        Saves a sitecore serialization diff list as a Sitecore Update Package.

    .DESCRIPTION
        Saves a sitecore serialization diff list as a Sitecore Update Package.


    .PARAMETER Name
        Name of the package.

    .PARAMETER CommandList
        List of changes to be included in the package.

    .PARAMETER Path
        Path the update package should be saved under.

    .PARAMETER Readme
        Contents of the "read me" instruction for the package

    .PARAMETER LicenseFileName
        file name of the license to be included with the package.

    .PARAMETER Tag
        Package tag.
    
    .INPUTS
    
    .OUTPUTS        

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Get-UpdatePackageDiff

    .LINK
        Install-UpdatePackage

    .LINK
        http://sitecoresnippets.blogspot.com/2012/10/sitecore-courier-effortless-packaging.html

    .LINK
        https://github.com/adoprog/Sitecore-Courier

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        # Create an update package that transforms the serialized database state defined in C:\temp\SerializationSource into into set defined in C:\temp\SerializationTarget
        $diff = Get-UpdatePackageDiff -SourcePath C:\temp\SerializationSource -TargetPath C:\temp\SerializationTarget
        Export-UpdatePackage -Path C:\temp\SerializationDiff.update -CommandList $diff -Name name
#>
