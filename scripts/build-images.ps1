Write-Host "Generate docker image"
Write-Host ""

$releases = Join-Path (Resolve-Path "$PSScriptRoot/..").Path "_output"

$extractedModule = Join-Path -Path $releases -ChildPath "extracted-module"
if(Test-Path -Path $extractedModule) {
    Remove-Item -Path $extractedModule -Recurse -Force
}

$dockerModulePath = Join-Path -Path $releases -ChildPath "docker-module"
if(Test-Path -Path $dockerModulePath) {
    Remove-Item -Path $dockerModulePath -Recurse -Force
}

$dockerModulePackage = Get-ChildItem -Path $releases -Filter "Sitecore.PowerShell.Extensions-*IAR.scwdp.zip" | Select-Object -ExpandProperty FullName
if(-not (Test-Path -Path $dockerModulePackage)) {
    Write-Error "Unable to locate the web deploy package."
    exit
}

Expand-Archive -Path $dockerModulePackage -DestinationPath $extractedModule -Force

New-Item -ItemType Directory -Path (Join-Path -Path $dockerModulePath -ChildPath "\cd\content")
New-Item -ItemType Directory -Path (Join-Path -Path $dockerModulePath -ChildPath "\cm\content")
New-Item -ItemType Directory -Path (Join-Path -Path $dockerModulePath -ChildPath "\db")
New-Item -ItemType Directory -Path (Join-Path -Path $dockerModulePath -ChildPath "\solr")
New-Item -ItemType Directory -Path (Join-Path -Path $dockerModulePath -ChildPath "\tools")

Copy-Item -Path "$($extractedModule)\Content\Website\*" -Destination (Join-Path -Path $dockerModulePath -ChildPath "\cm\content") -PassThru -Recurse
Copy-Item -Path "$($extractedModule)\core.dacpac" -Destination (Join-Path -Path $dockerModulePath -ChildPath "\db\Sitecore.Core.dacpac") -PassThru

$dockerVersion = [System.IO.Path]::GetFileName($dockermodulepackage).Replace("Sitecore.PowerShell.Extensions-", "").Replace("-IAR.scwdp.zip", "")

docker build --no-cache -t "sitecorepowershell/sitecore-powershell-extensions:$($dockerVersion)-1809" --build-arg BASE_IMAGE=mcr.microsoft.com/windows/nanoserver:1809 $releases
docker build --no-cache -t "sitecorepowershell/sitecore-powershell-extensions:$($dockerVersion)-ltsc2019" --build-arg BASE_IMAGE=mcr.microsoft.com/windows/nanoserver:ltsc2019 $releases
docker build --no-cache -t "sitecorepowershell/sitecore-powershell-extensions:$($dockerVersion)-ltsc2022" --build-arg BASE_IMAGE=mcr.microsoft.com/windows/nanoserver:ltsc2022 $releases