# https://doc.sitecore.com/developers/sat/20/sitecore-azure-toolkit/en/web-deploy-packages-for-a-module.html
# Make sure to import `Sitecore.Cloud.Cmdlets.dll` and not `Sitecore.Cloud.Cmdlets.psm1`
Import-Module -Name "C:\Sitecore\sat\tools\Sitecore.Cloud.Cmdlets.dll"

$path = "C:\Websites\dev.spe\Data\packages\Sitecore.PowerShell.Extensions-6.0.zip"
$destination = "C:\Websites\dev.spe\Data\packages"

$wdpPath = ConvertTo-SCModuleWebDeployPackage -Path $path  -Destination $destination -DisableDacPacOptions '*' -Verbose  -Force
#ConvertTo-SCModuleWebDeployPackage $_.fullname $destinationPath -Exclude $excludePaths -DisableDacPacOptions '*'

Get-FileHash -Path $path -Algorithm SHA256
Get-FileHash -Path $wdpPath -Algorithm SHA256