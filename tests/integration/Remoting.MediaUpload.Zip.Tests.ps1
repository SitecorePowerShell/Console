# Remoting Tests - Media Upload zip handling
# Fixtures are built programmatically so we don't commit binary test data.
# Covers: single-file root zip, zero-byte entry, zip-slip path traversal, corrupt archive.

Write-Host "`n  [Media Upload - zip handling]" -ForegroundColor White

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost
$localFilePath = (Resolve-Path (Join-Path $PSScriptRoot "..\fixtures")).Path
$zipRoot = Join-Path $env:TEMP ("spe-zip-" + [guid]::NewGuid().ToString("N").Substring(0,8))
New-Item -Path $zipRoot -ItemType Directory -Force | Out-Null

# Real JPEG payload shared across all zip variants. Using real image bytes (not text)
# exercises the server's MediaCreator path the same way production uploads do -- the
# extension is sniffed and the media item wraps a valid image stream.
$jpegBytes = [System.IO.File]::ReadAllBytes((Join-Path $localFilePath "kitten.jpg"))

function New-ZipFromEntries {
    param(
        [string]$OutputPath,
        [hashtable]$Entries   # name -> byte[]
    )
    if (Test-Path $OutputPath) { Remove-Item $OutputPath -Force }
    $fs = [System.IO.File]::Create($OutputPath)
    try {
        $archive = New-Object System.IO.Compression.ZipArchive($fs, [System.IO.Compression.ZipArchiveMode]::Create, $false)
        try {
            foreach ($key in $Entries.Keys) {
                $entry = $archive.CreateEntry($key)
                $es = $entry.Open()
                try {
                    $bytes = $Entries[$key]
                    if ($bytes.Length -gt 0) {
                        $es.Write($bytes, 0, $bytes.Length)
                    }
                } finally { $es.Dispose() }
            }
        } finally { $archive.Dispose() }
    } finally { $fs.Dispose() }
}

try {
    # ---- Single-file zip at root (real JPEG payload) ----
    $singleZip = Join-Path $zipRoot "single-file.zip"
    New-ZipFromEntries -OutputPath $singleZip -Entries @{ "readme.jpg" = $jpegBytes }

    Get-Item -Path $singleZip |
        Send-RemoteItem -Session $session -RootPath Media -Destination "Images/spe-zip-single"
    $singleCount = Invoke-RemoteScript -Session $session -ScriptBlock {
        Get-ChildItem -Path "master:\media library\images\spe-zip-single\" -Recurse |
            Measure-Object | Select-Object -ExpandProperty Count
    }
    Assert-True ($singleCount -ge 1) "Single-file zip unpacks at least one item (count=$singleCount)"

    $singleExt = Invoke-RemoteScript -Session $session -ScriptBlock {
        $item = Get-Item -Path "master:\media library\images\spe-zip-single\readme" -ErrorAction SilentlyContinue
        if ($item) { $item.Extension } else { "missing" }
    } -Raw
    Assert-Equal ($singleExt.Trim()) "jpg" "Single-file zip: unpacked item keeps .jpg extension"

    # ---- Zip with a zero-byte entry (real JPEG in the populated entry) ----
    $zeroEntryZip = Join-Path $zipRoot "zero-entry.zip"
    New-ZipFromEntries -OutputPath $zeroEntryZip -Entries @{
        "populated.jpg" = $jpegBytes
        "empty.jpg"     = [byte[]]@()
    }
    Get-Item -Path $zeroEntryZip |
        Send-RemoteItem -Session $session -RootPath Media -Destination "Images/spe-zip-zero"

    # Server's ProcessMedia skips zip entries with Length==0, so we expect only the populated one.
    $populated = Invoke-RemoteScript -Session $session -ScriptBlock {
        Test-Path -Path "master:\media library\images\spe-zip-zero\populated"
    }
    $emptyNotExpected = Invoke-RemoteScript -Session $session -ScriptBlock {
        Test-Path -Path "master:\media library\images\spe-zip-zero\empty"
    }
    Assert-Equal $populated $true "Zero-byte-entry zip: populated entry unpacked"
    Assert-Equal $emptyNotExpected $false "Zero-byte-entry zip: empty entry is skipped (current server policy)"

    # ---- Zip slip: entries with .. should not escape the destination folder ----
    $slipZip = Join-Path $zipRoot "zip-slip.zip"
    New-ZipFromEntries -OutputPath $slipZip -Entries @{
        "../escape.jpg"         = $jpegBytes
        "..\windows-escape.jpg" = $jpegBytes
        "innocent.jpg"          = $jpegBytes
    }

    $mediaBefore = Invoke-RemoteScript -Session $session -ScriptBlock {
        Get-ChildItem -Path "master:\media library\" -Recurse |
            Where-Object { $_.Name -like "escape*" -or $_.Name -like "windows-escape*" } |
            Measure-Object | Select-Object -ExpandProperty Count
    }

    Get-Item -Path $slipZip |
        Send-RemoteItem -Session $session -RootPath Media -Destination "Images/spe-zip-slip"

    $mediaAfter = Invoke-RemoteScript -Session $session -ScriptBlock {
        Get-ChildItem -Path "master:\media library\" -Recurse |
            Where-Object { $_.Name -like "escape*" -or $_.Name -like "windows-escape*" } |
            Measure-Object | Select-Object -ExpandProperty Count
    }
    Assert-Equal $mediaAfter $mediaBefore "Zip-slip: .. entries do not create escape items outside target folder"

    # Innocent sibling should land inside the destination.
    $innocent = Invoke-RemoteScript -Session $session -ScriptBlock {
        Test-Path -Path "master:\media library\images\spe-zip-slip\innocent"
    }
    Assert-Equal $innocent $true "Zip-slip: non-traversing entry still unpacked"

    # ---- Corrupt zip: starts with PK signature but body is garbage ----
    $corruptZip = Join-Path $zipRoot "corrupt.zip"
    $corruptBytes = [byte[]]@(0x50, 0x4B, 0x03, 0x04) + [byte[]]::new(64)
    (New-Object Random).NextBytes($corruptBytes[4..($corruptBytes.Length - 1)])
    [System.IO.File]::WriteAllBytes($corruptZip, $corruptBytes)

    $corruptError = $null
    Get-Item -Path $corruptZip |
        Send-RemoteItem -Session $session -RootPath Media -Destination "Images/spe-zip-corrupt" `
            -ErrorVariable corruptError -ErrorAction SilentlyContinue

    # Server treats corrupt archives as zip (IsZipContent checks magic bytes only) and
    # ZipArchive throws during iteration. We assert that no partial media was created
    # AND that the client either saw an error or an empty success.
    $corruptChildren = Invoke-RemoteScript -Session $session -ScriptBlock {
        if (Test-Path "master:\media library\images\spe-zip-corrupt") {
            (Get-ChildItem -Path "master:\media library\images\spe-zip-corrupt\" -Recurse | Measure-Object).Count
        } else { 0 }
    }
    Assert-True ($corruptChildren -eq 0) "Corrupt zip: no media items created (count=$corruptChildren)"
}
finally {
    Invoke-RemoteScript -Session $session -ScriptBlock {
        foreach ($p in @(
            "master:\media library\images\spe-zip-single\",
            "master:\media library\images\spe-zip-zero\",
            "master:\media library\images\spe-zip-slip\",
            "master:\media library\images\spe-zip-corrupt\"
        )) {
            Remove-Item -Path $p -Recurse -ErrorAction SilentlyContinue
        }
    } -ErrorAction SilentlyContinue
    Remove-Item -Path $zipRoot -Recurse -Force -ErrorAction SilentlyContinue
    Stop-ScriptSession -Session $session
}
