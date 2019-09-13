# Make sure to import `Sitecore.Cloud.Cmdlets.dll` and not `Sitecore.Cloud.Cmdlets.psm1`
Import-Module -Name "C:\Sitecore\sat\tools\Sitecore.Cloud.Cmdlets.dll"

$path = "C:\Websites\dev.spe\Data\packages\Sitecore.PowerShell.Extensions-6.0-beta6.zip"
$destination = "C:\Websites\dev.spe\Data\packages"

ConvertTo-SCModuleWebDeployPackage -Path $path  -Destination $destination -DisableDacPacOptions '*' -Verbose  -Force
#ConvertTo-SCModuleWebDeployPackage $_.fullname $destinationPath -Exclude $excludePaths -DisableDacPacOptions '*'