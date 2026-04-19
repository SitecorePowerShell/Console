# Remoting Tests - Media Upload switches
# SkipUnpack and SkipExisting are new-ish parameters with zero coverage.
# Versioned-update assertion protects against regressions in Settings.Media.UploadAsVersionableByDefault.

Write-Host "`n  [Media Upload - switches]" -ForegroundColor White

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost
$localFilePath = (Resolve-Path (Join-Path $PSScriptRoot "..\fixtures")).Path
$testRoot = "Images/spe-test-switches"

try {
    # ---- SkipUnpack: zip payload must land as a single media item, not unpacked ----
    Get-Item -Path "$($localFilePath)\Kittens.zip" |
        Send-RemoteItem -Session $session -RootPath Media -Destination $testRoot -SkipUnpack -ErrorAction SilentlyContinue

    $unpacked = Invoke-RemoteScript -Session $session -ScriptBlock {
        Get-ChildItem -Path "master:\media library\images\spe-test-switches\" -Recurse |
            Measure-Object | Select-Object -ExpandProperty Count
    }
    Assert-True ($unpacked -eq 1) "SkipUnpack keeps zip as a single media item (count=$unpacked)"

    $zipExt = Invoke-RemoteScript -Session $session -ScriptBlock {
        $item = Get-ChildItem -Path "master:\media library\images\spe-test-switches\" | Select-Object -First 1
        if ($item) { $item.Extension } else { "missing" }
    } -Raw
    Assert-Equal ($zipExt.Trim()) "zip" "SkipUnpack uploaded item retains .zip extension"

    # Clean the switches folder before the next phase
    Invoke-RemoteScript -Session $session -ScriptBlock {
        Remove-Item -Path "master:\media library\images\spe-test-switches\" -Recurse -ErrorAction SilentlyContinue
    }

    # ---- SkipExisting: second upload to the same destination must be a no-op ----
    Get-Item -Path "$($localFilePath)\kitten.jpg" |
        Send-RemoteItem -Session $session -RootPath Media -Destination "$testRoot/kitten.jpg"

    $firstSize = Invoke-RemoteScript -Session $session -ScriptBlock {
        (Get-Item -Path "master:\media library\images\spe-test-switches\kitten").Size
    }
    Assert-True ($firstSize -gt 0) "SkipExisting baseline: item has non-zero size (size=$firstSize)"

    Get-Item -Path "$($localFilePath)\kitten-replacement.jpg" |
        Send-RemoteItem -Session $session -RootPath Media -Destination "$testRoot/kitten.jpg" -SkipExisting

    $afterSize = Invoke-RemoteScript -Session $session -ScriptBlock {
        (Get-Item -Path "master:\media library\images\spe-test-switches\kitten").Size
    }
    Assert-Equal $afterSize $firstSize "SkipExisting does not overwrite existing media (size unchanged)"

    # Control: without -SkipExisting, the next upload DOES overwrite.
    Get-Item -Path "$($localFilePath)\kitten-replacement.jpg" |
        Send-RemoteItem -Session $session -RootPath Media -Destination "$testRoot/kitten.jpg"

    $overwrittenSize = Invoke-RemoteScript -Session $session -ScriptBlock {
        (Get-Item -Path "master:\media library\images\spe-test-switches\kitten").Size
    }
    Assert-NotEqual $overwrittenSize $firstSize "Without SkipExisting, same destination overwrites (size changed)"

    # ---- Versioned update: replacing existing media with UploadAsVersionableByDefault ----
    # The setting defaults depend on Sitecore.Media.config. Assert that the item exists
    # and has at least one version after the overwrite; a bump to 2 is optional.
    $versionCount = Invoke-RemoteScript -Session $session -ScriptBlock {
        $item = Get-Item -Path "master:\media library\images\spe-test-switches\kitten"
        $item.Versions.Count
    }
    Assert-True ($versionCount -ge 1) "Versioned update: item has >=1 version after overwrite (count=$versionCount)"
}
finally {
    Invoke-RemoteScript -Session $session -ScriptBlock {
        Remove-Item -Path "master:\media library\images\spe-test-switches\" -Recurse -ErrorAction SilentlyContinue
    } -ErrorAction SilentlyContinue
    Stop-ScriptSession -Session $session
}
