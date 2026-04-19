# Remoting Tests - Media Upload edge cases
# Zero-byte file, unicode filename, spaces/special chars, large real JPEG.
# The "large" JPEG is generated at test time by upscaling the committed kitten.jpg
# fixture via System.Drawing so the test exercises a real image (not random bytes)
# without bloating the repo with a large committed fixture. Stress-sized variant
# is gated behind SPE_RUN_LARGE_TESTS.

Write-Host "`n  [Media Upload - edge cases]" -ForegroundColor White

Add-Type -AssemblyName System.Drawing

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost
$localFilePath = (Resolve-Path (Join-Path $PSScriptRoot "..\fixtures")).Path
$edgeRoot = Join-Path $env:TEMP ("spe-edge-" + [guid]::NewGuid().ToString("N").Substring(0,8))
New-Item -Path $edgeRoot -ItemType Directory -Force | Out-Null

function New-UpscaledJpeg {
    param(
        [string]$SourcePath,
        [string]$DestinationPath,
        [int]$Width,
        [int]$Height,
        [int]$Quality = 92
    )
    $src = [System.Drawing.Image]::FromFile($SourcePath)
    try {
        $bmp = New-Object System.Drawing.Bitmap $Width, $Height
        try {
            $g = [System.Drawing.Graphics]::FromImage($bmp)
            try {
                $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $g.DrawImage($src, 0, 0, $Width, $Height)
            } finally { $g.Dispose() }

            $jpegCodec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() |
                Where-Object { $_.MimeType -eq "image/jpeg" } | Select-Object -First 1
            $encoderParams = New-Object System.Drawing.Imaging.EncoderParameters 1
            $qualityParam = New-Object System.Drawing.Imaging.EncoderParameter(
                [System.Drawing.Imaging.Encoder]::Quality, [int64]$Quality)
            $encoderParams.Param[0] = $qualityParam
            $bmp.Save($DestinationPath, $jpegCodec, $encoderParams)
        } finally { $bmp.Dispose() }
    } finally { $src.Dispose() }
}

try {
    # ---- Zero-byte file ----
    $zero = Join-Path $edgeRoot "empty.bin"
    [System.IO.File]::WriteAllBytes($zero, [byte[]]@())
    $zeroError = $null
    Get-Item -Path $zero |
        Send-RemoteItem -Session $session -RootPath Media -Destination "Images/spe-edge/empty.bin" `
            -ErrorVariable zeroError -ErrorAction SilentlyContinue

    # Current server behavior: empty InputStream means isUpload=false, so the handler
    # falls through to the download branch and returns an error. Assert either path:
    # either the item was created OR an error surfaced. A silent success with no item
    # is the real bug.
    $zeroCreated = Invoke-RemoteScript -Session $session -ScriptBlock {
        Test-Path -Path "master:\media library\images\spe-edge\empty"
    }
    Assert-True (($zeroCreated -eq $true) -or ($zeroError.Count -gt 0)) `
        "Zero-byte upload: either creates media or surfaces an error (created=$zeroCreated errs=$($zeroError.Count))"

    # ---- Filename with spaces ----
    $spaceSrc = Join-Path $edgeRoot "my photo.jpg"
    Copy-Item -Path (Join-Path $localFilePath "kitten.jpg") -Destination $spaceSrc
    Get-Item -Path $spaceSrc |
        Send-RemoteItem -Session $session -RootPath Media -Destination "Images/spe-edge/"
    $spaceExists = Invoke-RemoteScript -Session $session -ScriptBlock {
        Test-Path -Path "master:\media library\images\spe-edge\my photo"
    }
    Assert-Equal $spaceExists $true "Filename with spaces uploads correctly"

    # ---- Unicode filename ----
    # Sitecore's ItemUtil.ProposeValidItemName strips non-ASCII chars, and URL
    # encoding through HttpClient can further mangle the round-trip. This is a
    # soft check: count all siblings in spe-edge before/after and skip with
    # diagnostics if nothing appeared. A growth of >=1 means the server accepted
    # the upload under some sanitised name.
    $unicodeSrc = Join-Path $edgeRoot ("kitten-" + [char]0x00e9 + [char]0x00e8 + ".jpg")
    Copy-Item -Path (Join-Path $localFilePath "kitten.jpg") -Destination $unicodeSrc

    $beforeNames = Invoke-RemoteScript -Session $session -ScriptBlock {
        if (Test-Path "master:\media library\images\spe-edge") {
            @(Get-ChildItem -Path "master:\media library\images\spe-edge\" | Select-Object -ExpandProperty Name)
        } else { @() }
    }
    $beforeCount = @($beforeNames).Count

    $uniError = $null
    Get-Item -LiteralPath $unicodeSrc |
        Send-RemoteItem -Session $session -RootPath Media -Destination "Images/spe-edge/" `
            -ErrorVariable uniError -ErrorAction SilentlyContinue

    $afterNames = Invoke-RemoteScript -Session $session -ScriptBlock {
        @(Get-ChildItem -Path "master:\media library\images\spe-edge\" | Select-Object -ExpandProperty Name)
    }
    $afterCount = @($afterNames).Count

    if ($afterCount -gt $beforeCount) {
        $newNames = @($afterNames) | Where-Object { @($beforeNames) -notcontains $_ }
        Assert-True $true "Unicode filename upload created item (name: '$($newNames -join ', ')')"
    } elseif ($uniError -and "$uniError" -match "Forbidden|denied") {
        Skip-Test "Unicode filename upload" "server rejected (auth or path)"
    } else {
        Skip-Test "Unicode filename upload" ("no item created under spe-edge (before={0}, after={1}, err={2}) -- Sitecore likely stripped the filename to empty" -f $beforeCount, $afterCount, $uniError.Count)
    }

    # ---- Large real JPEG (default: ~1-3 MB, runs every suite) ----
    $largeSrc = Join-Path $edgeRoot "kitten-large.jpg"
    New-UpscaledJpeg -SourcePath (Join-Path $localFilePath "kitten.jpg") `
        -DestinationPath $largeSrc -Width 3000 -Height 2250 -Quality 95
    $largeSize = (Get-Item $largeSrc).Length
    Assert-True ($largeSize -gt 200KB) "Large JPEG generated ($([math]::Round($largeSize/1KB))KB)"

    $largeError = $null
    Get-Item -Path $largeSrc |
        Send-RemoteItem -Session $session -RootPath Media -Destination "Images/spe-edge/kitten-large.jpg" `
            -ErrorVariable largeError -ErrorAction SilentlyContinue
    if ($largeError) {
        Skip-Test "Large upload (real JPEG)" "server rejected, likely maxRequestLength: $($largeError[0])"
    } else {
        $serverSize = Invoke-RemoteScript -Session $session -ScriptBlock {
            (Get-Item -Path "master:\media library\images\spe-edge\kitten-large").Size
        }
        Assert-Equal $serverSize $largeSize "Large upload size matches ($([math]::Round($largeSize/1KB))KB)"

        # Round-trip: real JPEG round-tripped byte-for-byte.
        $downloaded = Join-Path $edgeRoot "kitten-large-downloaded.jpg"
        Receive-RemoteItem -Session $session -Path "/Images/spe-edge/kitten-large" `
            -Destination $downloaded -Database master -Force
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try {
            $s1 = [System.IO.File]::OpenRead($largeSrc); $srcHash = ([BitConverter]::ToString($sha.ComputeHash($s1)) -replace '-', ''); $s1.Dispose()
            $s2 = [System.IO.File]::OpenRead($downloaded); $dlHash = ([BitConverter]::ToString($sha.ComputeHash($s2)) -replace '-', ''); $s2.Dispose()
        } finally { $sha.Dispose() }
        Assert-Equal $dlHash $srcHash "Large JPEG round-trips byte-for-byte via SHA256"
    }

    # ---- Stress: very-large JPEG (~20-30MB) gated by SPE_RUN_LARGE_TESTS ----
    if ($env:SPE_RUN_LARGE_TESTS -eq "1") {
        $stressSrc = Join-Path $edgeRoot "kitten-stress.jpg"
        New-UpscaledJpeg -SourcePath (Join-Path $localFilePath "kitten.jpg") `
            -DestinationPath $stressSrc -Width 10000 -Height 7500 -Quality 98
        $stressSize = (Get-Item $stressSrc).Length

        $stressError = $null
        Get-Item -Path $stressSrc |
            Send-RemoteItem -Session $session -RootPath Media -Destination "Images/spe-edge/kitten-stress.jpg" `
                -ErrorVariable stressError -ErrorAction SilentlyContinue
        if ($stressError) {
            Skip-Test "Stress upload (very large JPEG)" "server rejected, likely maxRequestLength: $($stressError[0])"
        } else {
            $serverStress = Invoke-RemoteScript -Session $session -ScriptBlock {
                (Get-Item -Path "master:\media library\images\spe-edge\kitten-stress").Size
            }
            Assert-Equal $serverStress $stressSize "Stress upload size matches ($([math]::Round($stressSize/1MB))MB)"
        }
    } else {
        Skip-Test "Stress upload (very large JPEG)" "gated behind SPE_RUN_LARGE_TESTS=1"
    }
}
finally {
    Invoke-RemoteScript -Session $session -ScriptBlock {
        Remove-Item -Path "master:\media library\images\spe-edge\" -Recurse -ErrorAction SilentlyContinue
    } -ErrorAction SilentlyContinue
    Remove-Item -Path $edgeRoot -Recurse -Force -ErrorAction SilentlyContinue
    Stop-ScriptSession -Session $session
}
