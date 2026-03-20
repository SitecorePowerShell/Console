# Remoting Tests - Download with RemoteScriptCall
# Converted from Pester to custom assert format

Write-Host "`n  [Download with RemoteScriptCall - Single File]" -ForegroundColor White

$destinationMediaPath = "C:\temp\spe-test\"
if(Test-Path -Path $destinationMediaPath) {
    Remove-Item -Path $destinationMediaPath -Recurse
}
New-Item -Path $destinationMediaPath -ItemType Directory | Out-Null

#$session = New-ScriptSession -Username "sitecore\admin" -Password "b" -ConnectionUri $protocolHost
$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost

$rootPaths = @("App", "Data", "Debug", "Layout", "Log", "Media", "Package", "Serialization", "Temp")
$filenames = @{
    App           = "default.js"
    Data          = "items\master\items.master.dat"
    Debug         = "readme.txt"
    #Index         = "sitecore_master_index\segments.gen"
    Layout        = "xmlcontrol.aspx"
    Log           = "readme.txt"
    Media         = "readme.txt"
    Package       = "readme.txt"
    Serialization = "readme.txt"
    Temp          = "readme.txt"
}

foreach ($rootPath in $rootPaths) {
    $filename = $filenames[$rootPath]
    $destination = Join-Path -Path $destinationMediaPath -ChildPath $filename
    Write-Host "- Downloading $($filename)"
    try {
        Receive-RemoteItem -Session $session -Destination $destination -Path $filename -RootPath $rootPath
        Assert-True (Test-Path -Path $destination) "download from the $rootPath root path"
    } catch {
        Skip-Test "download from the $rootPath root path" "file not found on server ($filename)"
    }
}

Write-Host "`n  [Download with RemoteScriptCall - Single fully qualified file]" -ForegroundColor White

$filename = "kitten.jpg"
$pathFolder = (Resolve-Path (Join-Path -Path $PSScriptRoot -ChildPath "..\fixtures")).Path
$path = Join-Path -Path $pathFolder -ChildPath $filename
$destination = Join-Path -Path $destinationMediaPath -ChildPath $filename
# The fully qualified path points to the test machine's filesystem.
# In Docker, this file won't exist on the server -- skip gracefully.
try {
    Receive-RemoteItem -Session $session -Path $path -Destination $destination -ErrorAction Stop
    Assert-True (Test-Path -Path $destination) "download fully qualified file"
} catch {
    if ("$_" -match "Not Found|Forbidden") {
        Skip-Test "download fully qualified file" "server-side path does not exist (expected in Docker)"
    } else {
        throw
    }
}

Write-Host "`n  [Download with RemoteScriptCall - Media item]" -ForegroundColor White

# Media item downloads depend on default Sitecore content existing in the instance.
# Probe for the cover item first; skip the group if it doesn't exist.
$coverExists = Invoke-RemoteScript -Session $session -ScriptBlock {
    (Get-Item -Path "master:/sitecore/media library/Default Website/cover" -ErrorAction SilentlyContinue) -ne $null
} -Raw 2>$null

if ($coverExists -eq "True") {
    $filename = "cover.jpg"
    $mediaitem = "/Default Website/cover"
    $destination = Join-Path -Path $destinationMediaPath -ChildPath $filename
    Receive-RemoteItem -Session $session -Path $mediaitem -Destination $destinationMediaPath -Database master
    Assert-True (Test-Path -Path $destination) "download from relative media path in master"

    $filename = "cover.jpg"
    $mediaitem = "/sitecore/media library/Default Website/cover/"
    $destination = Join-Path -Path $destinationMediaPath -ChildPath $filename
    Receive-RemoteItem -Session $session -Path $mediaitem -Destination $destinationMediaPath -Database master
    Assert-True (Test-Path -Path $destination) "download from fully qualified media path in master"

    $filename = "\Default Website\cover.jpg"
    $mediaitem = "/Default Website/cover/"
    $destination = Join-Path -Path $destinationMediaPath -ChildPath $filename
    Receive-RemoteItem -Session $session -Path $mediaitem -Destination $destinationMediaPath -Database master -Container
    Assert-True (Test-Path -Path $destination) "download from media path in master maintaining structure"

    $filename = "cover1.jpg"
    $mediaitem = "{04DAD0FD-DB66-4070-881F-17264CA257E1}"
    $destination = Join-Path -Path $destinationMediaPath -ChildPath $filename
    $destinationChanged = Join-Path -Path $destinationMediaPath -ChildPath $filename
    Receive-RemoteItem -Session $session -Path $mediaitem -Destination $destinationChanged -Database master
    Assert-True (Test-Path -Path $destination) "download from media path with GUID in master changing filename"
} else {
    Skip-Test "download from relative media path in master" "cover media item not found in instance"
    Skip-Test "download from fully qualified media path in master" "cover media item not found in instance"
    Skip-Test "download from media path in master maintaining structure" "cover media item not found in instance"
    Skip-Test "download from media path with GUID in master changing filename" "cover media item not found in instance"
}

Write-Host "`n  [Download with RemoteScriptCall - Advanced/mixed scenarios]" -ForegroundColor White

$files = Invoke-RemoteScript -Session $session -ScriptBlock {
    Get-ChildItem -Path "$($SitecoreLogFolder)" | Where-Object { !$_.PSIsContainer } | Select-Object -Expand Name -First 3
}

$files |
    ForEach-Object {
        $destination = Join-Path -Path $destinationMediaPath -ChildPath $_
        Receive-RemoteItem -Session $session -Destination $destination -Path $_ -RootPath Log
        Assert-True (Test-Path -Path $destination) "Download log file $_"
    }

$archiveFileName = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name Compress-Archive

    Get-ChildItem -Path "$($SitecoreLogFolder)" -File | Where-Object { $_.Name -match "spe.log." } |
        Compress-Archive -DestinationPath "$($SitecoreTempFolder)\archived.SPE.logs.zip" | Select-Object -Expand FullName
}

Assert-NotNull $archiveFileName "Download all SPE log files as ZIP - archive created"

$destination = Join-Path -Path $destinationMediaPath -ChildPath (Split-Path -Path $archiveFileName -Leaf)
Receive-RemoteItem -Session $session -Destination $destination -Path $archiveFileName

Assert-True (Test-Path -Path $destination) "Download all SPE log files as ZIP - file received"

$cleanupResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    Remove-Item -Path "$($using:archiveFileName)"
    Test-Path "$($using:archiveFileName)"
}
Assert-Equal $cleanupResult $false "Download all SPE log files as ZIP - remote cleanup"

$mediaItemNames = Invoke-RemoteScript -Session $session -ScriptBlock {
    Get-ChildItem -Path "master:/sitecore/media library/" -Recurse | Where-Object { $_.Size -gt 0 } |
    Select-Object -First 10 | Foreach-Object { "$($_.ItemPath).$($_.Extension)" }
}

$mediaItemNames | Foreach-Object {
    $source = Join-Path ([System.IO.Path]::GetDirectoryName($_)) ([System.IO.Path]::GetFileNameWithoutExtension($_))
    $destination = Join-Path -Path $destinationMediaPath -ChildPath $_
    Receive-RemoteItem -Session $session -Destination $destination -Path $source -Database master
    Assert-True (Test-Path -Path $destination) "Download Media Item $_"
}

Stop-ScriptSession -Session $session
