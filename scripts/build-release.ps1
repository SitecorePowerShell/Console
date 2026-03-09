# CONFIGURATION START

$projectPath = (Resolve-Path "$PSScriptRoot/..").Path

# Read CM_HOST from root .env (falls back to docker/.env.template)
$envFile = Join-Path $projectPath ".env"
if (-not (Test-Path $envFile)) {
    $envFile = Join-Path $projectPath "docker\.env.template"
}
$cmHost = (Get-Content $envFile | Where-Object { $_ -match "^CM_HOST=" }) -replace "CM_HOST=",""
$hostname = "https://$cmHost"

# Packages output folder
# Docker: uses $projectPath\_output (default)
# IIS:    uncomment and set your instance path
# $releases = "C:\inetpub\wwwroot\$cmHost\App_Data\packages\"

# Make sure Admin is part of the Sitecore Remoting group inside sitecore
$sharedSecret = '7AF6F59C14A05786E97012F054D1FB98AC756A2E54E5C9ACBAEE147D9ED0E0DB'
$userName = 'sitecore\admin'


# EXECUTION START

Clear-Host

if(!$releases -or !(Test-Path -Path $releases))
{
    $releases = Join-Path -Path $projectPath -ChildPath "_output"
}

Write-Host "Generate dat files"

& "$PSScriptRoot\generate-dat.ps1"

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

Add-Type -AssemblyName "System.IO.Compression"
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
        $content = [System.IO.File]::ReadAllBytes((Join-Path -Path "$projectPath\serialization\_out" -ChildPath $fileName))
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

$packages = Get-ChildItem -Path $releases -Filter "Sitecore.PowerShell.Extensions-*.*.zip" | Select-Object -ExpandProperty FullName
$destination = "$($releases)\"

function Get-SHA256Hash([string]$FilePath) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $stream = [System.IO.File]::OpenRead($FilePath)
        try {
            $hashBytes = $sha.ComputeHash($stream)
            return [BitConverter]::ToString($hashBytes) -replace '-',''
        } finally { $stream.Dispose() }
    } finally { $sha.Dispose() }
}

Write-Host "## File Hashes"
foreach($package in $packages) {

    Write-Host "### $([System.IO.Path]::GetFileNameWithoutExtension($package))"
    $hash = Get-SHA256Hash $package
    Write-Host "SHA256 for $([System.IO.Path]::GetFileName($package))"
    Write-Host "$hash"
    Write-Host ""

    $wdpPath = & "$PSScriptRoot\convert-to-scwdp.ps1" -Path $package -Destination $destination

    $hashwdp = Get-SHA256Hash $wdpPath
    Write-Host "SHA256 for $([System.IO.Path]::GetFileName($wdpPath))"
    Write-Host "$hashwdp"

    Write-Host ""
}

& "$PSScriptRoot\build-images.ps1"