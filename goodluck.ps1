Clear-Host

Write-Host "Generate dat files"

$cli = Join-Path -Path $PSScriptRoot -ChildPath "cli"
& $cli\generate.bat

$releases = Join-Path -Path $PSScriptRoot -ChildPath "releases"

Write-Host "Remove old packages from $releases"
Get-ChildItem -Path $releases -Filter "Sitecore.PowerShell.Extensions-*" | Remove-Item
Get-ChildItem -Path $releases -Filter "SPE.*" | Remove-Item

Write-Host "Generate packages from running Sitecore instance."

Import-Module -Name SPE

$sharedSecret = '7AF6F59C14A05786E97012F054D1FB98AC756A2E54E5C9ACBAEE147D9ED0E0DB'
$name = 'sitecore\PowerShellExtensionsAPI'
$hostname = "https://spe.dev.local"

# TODO: Generate normal package with dat files instead of items. Maybe use a temporary file name like Sitecore.PowerShell.Extensions-6.3-IAR.temp.zip
$session = New-ScriptSession -Username $name -SharedSecret $sharedSecret -ConnectionUri $hostname
Invoke-RemoteScript -ScriptBlock {
    # Prepare Console Distribution
    Invoke-Script -Path "master:{AC05422C-A1B1-41BA-A1FD-4EC7E944DE3B}"
} -Session $session -Raw
Stop-ScriptSession -Session $session

Write-Host "Swap out IAR files"

Add-Type -AssemblyName "System.IO.Compression.FileSystem"
$file = "C:\Projects\Spe\releases\Sitecore.PowerShell.Extensions-6.4-IAR.zip"
$zip = [System.IO.Compression.ZipFile]::Open($file, [System.IO.Compression.ZipArchiveMode]::Update)
$packageZipEntry = $zip.Entries | Where-Object { $_.Name -eq "package.zip" }

$stream = $packageZipEntry.Open()
$packageArchive = New-Object System.IO.Compression.ZipArchive($stream, [System.IO.Compression.ZipArchiveMode]::Update)
$iarEntries = $packageArchive.Entries | Where-Object { $_.Name -like "*spe.dat*" }
foreach($iarEntry in $iarEntries) {
    $fullname = $iarEntry.FullName.Replace(".tmp", "")
    $iarEntry.Delete()
    $iarEntry = $packageArchive.CreateEntry($fullname)

    if($fullname.StartsWith("files")) {
        $writer = New-Object System.IO.StreamWriter($iarEntry.Open())
        $name = [System.IO.Path]::GetFileName($fullname)
        $content = Get-Content -Path (Join-Path -Path ".\cli\_out" -ChildPath $name) -Raw
        $writer.Write($content)
        $writer.Dispose()
        $writer.Close()
    }
}

$packageArchive.Dispose()

$stream.Close()
$stream.Dispose()

$zip.Dispose()

Write-Host "Generate wdp module"
Write-Host ""

Import-Module -Name "C:\Sitecore\sat\tools\Sitecore.Cloud.Cmdlets.dll"

$packages = Get-ChildItem -Path $releases -Filter "Sitecore.PowerShell.Extensions-*.*.zip" | Select-Object -ExpandProperty FullName
$destination = "$($releases)\"

foreach($package in $packages) {

    Write-Host "$([System.IO.Path]::GetFileNameWithoutExtension($package))"
    Write-Host "-----"
    $hash = Get-FileHash -Path $package -Algorithm SHA256
    Write-Host "SHA256 for $([System.IO.Path]::GetFileName($package))"
    Write-Host "$($hash.Hash)"
    Write-Host ""

    Write-Host "Converting $($package) into an scwdp file"
    $wdpPath = ConvertTo-SCModuleWebDeployPackage -Path $package  -Destination $destination -DisableDacPacOptions '*' -Force
    
    $hashwdp = Get-FileHash -Path $wdpPath -Algorithm SHA256
    Write-Host "SHA256 for $([System.IO.Path]::GetFileName($wdpPath))"
    Write-Host "$($hashwdp.Hash)"

    Write-Host ""
}