# Remoting Tests - Upload with RemoteScriptCall
# Converted from Pester to custom assert format

Write-Host "`n  [Upload with RemoteScriptCall]" -ForegroundColor White

#$session = New-ScriptSession -Username "sitecore\admin" -Password "b" -ConnectionUri $protocolHost
$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost
$localFilePath = Join-Path -Path $PSScriptRoot -ChildPath "..\fixtures"

$filename = "data.xml"
Get-Item -Path "$($localFilePath)\$($filename)" |
    Send-RemoteItem -Session $session -RootPath App -ErrorAction SilentlyContinue
$result = Invoke-RemoteScript -Session $session -ScriptBlock { Test-Path -Path "$($AppPath)\$($using:filename)" }
Assert-Equal $result $true "upload to the App root path"

$filename = "data.xml"
Get-Item -Path "$($localFilePath)\$($filename)" |
    Send-RemoteItem -Session $session -RootPath Package -Destination "\" -ErrorAction SilentlyContinue
$result = Invoke-RemoteScript -Session $session -ScriptBlock { Test-Path -Path "$($SitecorePackageFolder)\$($using:filename)" }
Assert-Equal $result $true "upload to the Package root path"

$filename = "kitten.jpg"
$filenameWithoutExtension = [System.IO.Path]::GetFileNameWithoutExtension($filename)
Get-Item -Path "$($localFilePath)\$($filename)" |
    Send-RemoteItem -Session $session -RootPath Media -ErrorAction SilentlyContinue -Destination "Images/spe-test"
$result = Invoke-RemoteScript -Session $session -ScriptBlock { Test-Path -Path "master:\media library\images\spe-test\$($using:filenameWithoutExtension)" }
Assert-Equal $result $true "upload to the Media Library"

$filename = "kitten.jpg"
$filenameWithoutExtension = [System.IO.Path]::GetFileNameWithoutExtension($filename)
Get-Item -Path "$($localFilePath)\$($filename)" |
    Send-RemoteItem -Session $session -RootPath Media -ErrorAction SilentlyContinue -Destination "Images/spe-test/kitten1.jpg"
$result = Invoke-RemoteScript -Session $session -ScriptBlock { Test-Path -Path "master:\media library\images\spe-test\$($using:filenameWithoutExtension)1" }
Assert-Equal $result $true "upload to the Media Library with different name"

$filename = "kitten.jpg"
$filenameReplacement = "kitten-replacement.jpg"
$filenameWithoutExtension = [System.IO.Path]::GetFileNameWithoutExtension($filename)
Get-Item -Path "$($localFilePath)\$($filename)" |
    Send-RemoteItem -Session $session -RootPath Media -ErrorAction SilentlyContinue -Destination "Images/spe-test/"
# Verify the file was uploaded
$result = Invoke-RemoteScript -Session $session -ScriptBlock { Test-Path -Path "master:\media library\images\spe-test\$($using:filenameWithoutExtension)" }
Assert-Equal $result $true "upload to the Media Library and replace using a guid - initial upload"
# Keep track of the current Id and Size
$details = Invoke-RemoteScript -Session $session -ScriptBlock {
    $item = Get-Item -Path "master:media library\images\spe-test\kitten"
    [PSCustomObject]@{
        "Id" = $item.ID
        "Size" = $item.Size
    }
}
# Verify that we can get details about the file
Assert-NotNull $details "upload to the Media Library and replace using a guid - get details"
Get-Item -Path "$($localFilePath)\$($filenameReplacement)" | Send-RemoteItem -Session $session -RootPath Media -Destination $details.Id
# Verify that the file size has changed
$details2 = Invoke-RemoteScript -Session $session -ScriptBlock {
    $item = Get-Item -Path "master:media library\images\spe-test\kitten"
    [PSCustomObject]@{
        "Id" = $item.ID
        "Size" = $item.Size
    }
}
Assert-NotEqual $details.Size $details2.Size "upload to the Media Library and replace using a guid - size changed"

$filename = "kittens.zip"
Get-Item -Path "$($localFilePath)\$($filename)" |
    Send-RemoteItem -Session $session -RootPath Media -ErrorAction SilentlyContinue -Destination "Images/spe-test"
$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Get-ChildItem -Path "master:\media library\images\spe-test\" -Recurse | Measure-Object | Select-Object -ExpandProperty Count
}
Assert-Equal $result 5 "upload to the Media Library a compressed archive"

# Cleanup
Invoke-RemoteScript -Session $session -ScriptBlock { Remove-Item -Path "master:\media library\images\spe-test\" -Recurse }
Stop-ScriptSession -Session $session
