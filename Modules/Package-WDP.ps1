# Make sure to import `Sitecore.Cloud.Cmdlets.dll` and not `Sitecore.Cloud.Cmdlets.psm1`
Import-Module -Name "C:\Sitecore\sat\tools\Sitecore.Cloud.Cmdlets.dll"

#$path = "C:\Websites\dev.spe\Data\packages\Sitecore.PowerShell.Extensions-6.0.zip"
#$destination = "C:\Websites\dev.spe\Data\packages"

$destination = "C:\Websites\sc827\Data\packages"
$path = "C:\Websites\sc827\Data\packages\Sitecore PowerShell Extensions-5.1.zip"
ConvertTo-SCModuleWebDeployPackage -Path $path  -Destination $destination  -Verbose  -Force
$path = "C:\Websites\sc827\Data\packages\Sitecore PowerShell Extensions - Authorable Reports-5.1.zip"
ConvertTo-SCModuleWebDeployPackage -Path $path  -Destination $destination  -Verbose  -Force