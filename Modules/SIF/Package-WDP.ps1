﻿# https://doc.sitecore.com/developers/sat/20/sitecore-azure-toolkit/en/web-deploy-packages-for-a-module.html
# Make sure to import `Sitecore.Cloud.Cmdlets.dll` and not `Sitecore.Cloud.Cmdlets.psm1`
Import-Module -Name "C:\Sitecore\sat\tools\Sitecore.Cloud.Cmdlets.dll"

$path = "C:\Projects\Spe\releases\Sitecore.PowerShell.Extensions-6.3.zip"
$destination = "C:\Projects\Spe\releases\"

$wdpPath = ConvertTo-SCModuleWebDeployPackage -Path $path  -Destination $destination -DisableDacPacOptions '*' -Verbose  -Force

$hash = Get-FileHash -Path $path -Algorithm SHA256
Write-Host "SHA256 for $([System.IO.Path]::GetFileName($path))"
Write-Host "$($hash.Hash)"
Write-Host ""
$hashwdp = Get-FileHash -Path $wdpPath -Algorithm SHA256
Write-Host "SHA256 for $([System.IO.Path]::GetFileName($wdpPath))"
Write-Host "$($hashwdp.Hash)"