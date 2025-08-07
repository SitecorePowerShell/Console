# CONFIGURATION START

# URl to your Sitecore instance
$hostname = "https://sc103cm.dev.local"

# If you have the packages folder mapped to ProjectPath\releases comment out the next line
$releases = "C:\inetpub\wwwroot\sc103cm.dev.local\App_Data\packages\"

# Make sure Admin is part of the Sitecore Remoting group inside sitecore
$sharedSecret = '7AF6F59C14A05786E97012F054D1FB98AC756A2E54E5C9ACBAEE147D9ED0E0DB'
$userName = 'sitecore\admin'

$projectPath = $PSScriptRoot


# EXECUTION START

Clear-Host

if(!$releases)
{
    $releases = Join-Path -Path $projectPath -ChildPath "releases"
}

$sat = Join-Path -Path $releases -ChildPath "sat"
if(-not (Test-Path -Path $sat)) {
    New-Item -Path $sat -ItemType Directory
}

$satConfig = Get-ChildItem -Path $releases -Filter "configuration.json" | 
    Get-Content | ConvertFrom-Json | Select-Object -ExpandProperty "SitecoreAzureToolkit"

$satPackage = Join-Path -Path $sat -ChildPath $satConfig.Filename
if(-not (Test-Path -Path $satPackage)) {
    Get-ChildItem -Path $sat -Recurse | Remove-Item -Recurse

    Write-Host "Downloading $($satConfig.Filename)"
    $webClient = New-Object System.Net.WebClient
    $webClient.Downloadfile($satConfig.Url, $satPackage)
    
    Write-Host "Unblocking $($satConfig.Filename)"
    Unblock-File -Path $satPackage

    Expand-Archive -Path $satPackage -DestinationPath $sat
}

Write-Host "Generate dat files"

$cli = Join-Path -Path $projectPath -ChildPath "cli"
& $cli\generate.bat

Write-Host "Remove old packages from $releases"
Get-ChildItem -Path $releases -Filter "Sitecore.PowerShell.Extensions-*" | Remove-Item
Get-ChildItem -Path $releases -Filter "SPE.*" | Remove-Item

Write-Host "Generate packages from running Sitecore instance."

Import-Module -Name SPE

# TODO: Generate normal package with dat files instead of items. Maybe use a temporary file name like Sitecore.PowerShell.Extensions-6.3-IAR.temp.zip
$session = New-ScriptSession -Username $userName -SharedSecret $sharedSecret -ConnectionUri $hostname
Invoke-RemoteScript -ScriptBlock {
    # Prepare Console Distribution
    Invoke-Script -Path "master:{AC05422C-A1B1-41BA-A1FD-4EC7E944DE3B}"
} -Session $session -Raw
Stop-ScriptSession -Session $session

Write-Host "Swap out IAR files"

Add-Type -AssemblyName "System.IO.Compression.FileSystem"
$archiveMode = [System.IO.Compression.ZipArchiveMode]::Update
$iarPackageFileGlob = "$releases\Sitecore.PowerShell.Extensions-*-IAR.zip"
$iarfileName = (Get-Item $iarPackageFileGlob | Select-Object -First 1).FullName
$zip = [System.IO.Compression.ZipFile]::Open($iarfileName, $archiveMode)
$packageZipEntry = $zip.Entries | Where-Object { $_.Name -eq "package.zip" }

$stream = $packageZipEntry.Open()
$packageArchive = New-Object System.IO.Compression.ZipArchive($stream, $archiveMode)
$iarEntries = $packageArchive.Entries | Where-Object { $_.Name -like "*spe.dat*" }
foreach($iarEntry in $iarEntries) {
    $fullname = $iarEntry.FullName.Replace(".tmp", "")
    $iarEntry.Delete()
    $iarEntry = $packageArchive.CreateEntry($fullname)

    if($fullname.StartsWith("files")) {
        $fileName = [System.IO.Path]::GetFileName($fullname)
        $content = [System.IO.File]::ReadAllBytes((Join-Path -Path "$projectPath\cli\_out" -ChildPath $fileName))
        $ms = New-Object System.IO.MemoryStream(,$content)
        $zipStream = $iarEntry.Open()
        $ms.CopyTo($zipStream)
        $zipStream.Dispose()
        $zipStream.Close()
        $ms.Dispose()
        $ms.Close()
    }
}

$packageArchive.Dispose()

$stream.Close()
$stream.Dispose()

$zip.Dispose()

Write-Host "Generate wdp module"
Write-Host ""

Import-Module -Name (Join-Path -Path $sat -ChildPath "tools\Sitecore.Cloud.Cmdlets.dll")

$packages = Get-ChildItem -Path $releases -Filter "Sitecore.PowerShell.Extensions-*.*.zip" | Select-Object -ExpandProperty FullName
$destination = "$($releases)\"

Write-Host "## File Hashes"
foreach($package in $packages) {

    Write-Host "### $([System.IO.Path]::GetFileNameWithoutExtension($package))"
    $hash = Get-FileHash -Path $package -Algorithm SHA256
    Write-Host "SHA256 for $([System.IO.Path]::GetFileName($package))"
    Write-Host "$($hash.Hash)"
    Write-Host ""

    $wdpPath = ConvertTo-SCModuleWebDeployPackage -Path $package  -Destination $destination -DisableDacPacOptions '*' -Force
    
    $hashwdp = Get-FileHash -Path $wdpPath -Algorithm SHA256
    Write-Host "SHA256 for $([System.IO.Path]::GetFileName($wdpPath))"
    Write-Host "$($hashwdp.Hash)"

    Write-Host ""
}