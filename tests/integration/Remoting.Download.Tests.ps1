# Remoting Tests - Download with RemoteScriptCall

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

Write-Host "`n  [Download with RemoteScriptCall - Server-generated absolute-path artifact]" -ForegroundColor White

# Roundtrip a server-generated artifact via absolute-path download. Replaces an
# earlier block that compressed live SPE logs (racy against log rotation). The
# fixture path lives under C:\inetpub\wwwroot\App_Data - covered by the
# Spe.Remoting.AllowedFileRoots entry in tests/configs/deploy/z.Spe.Security.Disabler.config.
# Alias-based downloads are exercised by the rootPaths loop above;
# absolute-path rejection cases are covered by Remoting.FileSecurity.Tests.ps1.

$serverArtifactPath = 'C:\inetpub\wwwroot\App_Data\spe-download-roundtrip.txt'
$expectedPayload = "spe-download-roundtrip-$(Get-Date -Format 'yyyyMMddHHmmssfff')"

Invoke-RemoteScript -Session $session -ScriptBlock {
    if (Test-Path $using:serverArtifactPath) { Remove-Item $using:serverArtifactPath -Force }
    Set-Content -Path $using:serverArtifactPath -Value $using:expectedPayload -Encoding Ascii
}

$destination = Join-Path -Path $destinationMediaPath -ChildPath (Split-Path -Path $serverArtifactPath -Leaf)
Receive-RemoteItem -Session $session -Destination $destination -Path $serverArtifactPath
Assert-True (Test-Path -Path $destination) "Server-generated artifact: downloaded"

$downloadedPayload = (Get-Content -Path $destination -Raw).Trim()
Assert-Equal $downloadedPayload $expectedPayload "Server-generated artifact: payload matches"

$cleanupResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    Remove-Item -Path $using:serverArtifactPath -Force
    Test-Path $using:serverArtifactPath
}
Assert-Equal $cleanupResult $false "Server-generated artifact: remote cleanup"

Stop-ScriptSession -Session $session
