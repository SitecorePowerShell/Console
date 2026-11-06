# Remoting Tests - Media Upload basic paths
# Baseline scenarios that used to live in Remoting.Upload.Tests.ps1.

Write-Host "`n  [Media Upload - basic paths]" -ForegroundColor White

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost
$localFilePath = Join-Path -Path $PSScriptRoot -ChildPath "..\fixtures"

try {
    $filename = "data.xml"
    $uploadError = $null
    Get-Item -Path "$($localFilePath)\$($filename)" |
        Send-RemoteItem -Session $session -RootPath App -ErrorVariable uploadError -ErrorAction SilentlyContinue
    if ($uploadError -and "$uploadError" -match "Forbidden|denied") {
        Skip-Test "upload to the App root path" "server lacks write permission to App root (environment issue)"
    } else {
        $result = Invoke-RemoteScript -Session $session -ScriptBlock { Test-Path -Path "$($AppPath)\$($using:filename)" }
        Assert-Equal $result $true "upload to the App root path"
    }

    $filename = "data.xml"
    $uploadError = $null
    Get-Item -Path "$($localFilePath)\$($filename)" |
        Send-RemoteItem -Session $session -RootPath Package -Destination "\" -ErrorVariable uploadError -ErrorAction SilentlyContinue
    if ($uploadError -and "$uploadError" -match "Forbidden|denied") {
        Skip-Test "upload to the Package root path" "server lacks write permission to Package path (environment issue)"
    } else {
        $result = Invoke-RemoteScript -Session $session -ScriptBlock { Test-Path -Path "$($SitecorePackageFolder)\$($using:filename)" }
        Assert-Equal $result $true "upload to the Package root path"
    }

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
    $result = Invoke-RemoteScript -Session $session -ScriptBlock { Test-Path -Path "master:\media library\images\spe-test\$($using:filenameWithoutExtension)" }
    Assert-Equal $result $true "upload to the Media Library and replace using a guid - initial upload"
    $details = Invoke-RemoteScript -Session $session -ScriptBlock {
        $item = Get-Item -Path "master:media library\images\spe-test\kitten"
        [PSCustomObject]@{
            "Id" = $item.ID
            "Size" = $item.Size
        }
    }
    Assert-NotNull $details "upload to the Media Library and replace using a guid - get details"
    Get-Item -Path "$($localFilePath)\$($filenameReplacement)" | Send-RemoteItem -Session $session -RootPath Media -Destination $details.Id
    $details2 = Invoke-RemoteScript -Session $session -ScriptBlock {
        $item = Get-Item -Path "master:media library\images\spe-test\kitten"
        [PSCustomObject]@{
            "Id" = $item.ID
            "Size" = $item.Size
        }
    }
    Assert-NotEqual $details.Size $details2.Size "upload to the Media Library and replace using a guid - size changed"

    # Zip archive unpacking happy-path (SkipUnpack variants live in Switches suite).
    $filename = "kittens.zip"
    $countBefore = Invoke-RemoteScript -Session $session -ScriptBlock {
        Get-ChildItem -Path "master:\media library\images\spe-test\" -Recurse | Measure-Object | Select-Object -ExpandProperty Count
    }
    Get-Item -Path "$($localFilePath)\$($filename)" |
        Send-RemoteItem -Session $session -RootPath Media -ErrorAction SilentlyContinue -Destination "Images/spe-test"
    $countAfter = Invoke-RemoteScript -Session $session -ScriptBlock {
        Get-ChildItem -Path "master:\media library\images\spe-test\" -Recurse | Measure-Object | Select-Object -ExpandProperty Count
    }
    $added = $countAfter - $countBefore
    Assert-True ($added -ge 3) "upload to the Media Library a compressed archive (added $added items)"
}
finally {
    Invoke-RemoteScript -Session $session -ScriptBlock {
        Remove-Item -Path "master:\media library\images\spe-test\" -Recurse -ErrorAction SilentlyContinue
    } -ErrorAction SilentlyContinue
    Stop-ScriptSession -Session $session
}
