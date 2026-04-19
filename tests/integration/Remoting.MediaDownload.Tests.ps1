# Remoting Tests - Media download scenarios
# Complements Remoting.Download.Tests.ps1 with media-specific edge cases:
# missing item 404, GUID addressing + Destination with/without extension,
# Container mode preservation, and Content-Disposition handling.

Write-Host "`n  [Media Download - edge cases]" -ForegroundColor White

function Get-FileHashHex {
    # Get-FileHash is missing in the test harness's PowerShell runtime.
    param([string]$Path)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $stream = [System.IO.File]::OpenRead($Path)
        try {
            $hash = $sha.ComputeHash($stream)
        } finally { $stream.Dispose() }
    } finally { $sha.Dispose() }
    return ([BitConverter]::ToString($hash) -replace '-', '').ToUpperInvariant()
}

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost
$localFilePath = (Resolve-Path (Join-Path $PSScriptRoot "..\fixtures")).Path
$downloadRoot = Join-Path $env:TEMP ("spe-mediadl-" + [guid]::NewGuid().ToString("N").Substring(0,8))
New-Item -Path $downloadRoot -ItemType Directory -Force | Out-Null

try {
    # Seed a known media item to download in all cases.
    Get-Item -Path (Join-Path $localFilePath "kitten.jpg") |
        Send-RemoteItem -Session $session -RootPath Media -Destination "Images/spe-mediadl/kitten.jpg"

    $seedId = Invoke-RemoteScript -Session $session -ScriptBlock {
        (Get-Item -Path "master:\media library\images\spe-mediadl\kitten").ID.ToString()
    } -Raw
    $seedId = $seedId.Trim()
    Assert-True ($seedId -match "^\{[0-9A-Fa-f-]{36}\}$") "Seed media item has a resolvable ID"

    # ---- 1. Download by path with directory Destination ----
    $d1 = Join-Path $downloadRoot "dir1\"
    Receive-RemoteItem -Session $session -Path "/Images/spe-mediadl/kitten" -Destination $d1 -Database master -Force
    $expected1 = Join-Path $d1.TrimEnd('\') "kitten.jpg"
    Assert-True (Test-Path $expected1) "Path addressing with directory destination writes server-named file"

    # ---- 2. Download by GUID with file-extension Destination (early-return path) ----
    $d2 = Join-Path $downloadRoot "renamed.jpg"
    Receive-RemoteItem -Session $session -Path $seedId -Destination $d2 -Database master -Force
    Assert-True (Test-Path $d2) "GUID addressing with explicit filename writes to Destination"

    # ---- 3. Download by GUID with directory Destination ----
    $d3 = Join-Path $downloadRoot "dir3\"
    Receive-RemoteItem -Session $session -Path $seedId -Destination $d3 -Database master -Force
    $files3 = Get-ChildItem -Path $d3 -File
    Assert-True ($files3.Count -ge 1) "GUID addressing with directory destination produces a file (count=$($files3.Count))"

    # ---- 4. Container mode preserves media-library subdirectory structure ----
    $d4 = Join-Path $downloadRoot "mirror"
    Receive-RemoteItem -Session $session -Path "/Images/spe-mediadl/kitten" -Destination $d4 `
        -Database master -Container -Force
    $mirrored = Get-ChildItem -Path $d4 -Recurse -File | Select-Object -First 1
    Assert-NotNull $mirrored "Container mode creates nested directory + file"
    if ($mirrored) {
        Assert-Like $mirrored.FullName "*spe-mediadl*" "Container mode preserves 'spe-mediadl' folder in output path"
    }

    # ---- 5. Missing media item surfaces an error, does not silently write a file ----
    $d5 = Join-Path $downloadRoot "missing.jpg"
    $missingError = $null
    Receive-RemoteItem -Session $session -Path "/Images/spe-mediadl/does-not-exist" -Destination $d5 `
        -Database master -Force -ErrorVariable missingError -ErrorAction SilentlyContinue
    $missingBytes = if (Test-Path $d5) { (Get-Item $d5).Length } else { -1 }
    Assert-True ($missingError.Count -gt 0 -or $missingBytes -le 0) `
        "Missing media: surfaces error OR writes no body (errs=$($missingError.Count) bytes=$missingBytes)"

    # ---- 6. Round-trip byte fidelity ----
    $srcHash = Get-FileHashHex -Path (Join-Path $localFilePath "kitten.jpg")
    $dlHash  = Get-FileHashHex -Path $d2
    Assert-Equal $dlHash $srcHash "Media download SHA256 matches uploaded source"
}
finally {
    Invoke-RemoteScript -Session $session -ScriptBlock {
        Remove-Item -Path "master:\media library\images\spe-mediadl\" -Recurse -ErrorAction SilentlyContinue
    } -ErrorAction SilentlyContinue
    Remove-Item -Path $downloadRoot -Recurse -Force -ErrorAction SilentlyContinue
    Stop-ScriptSession -Session $session
}
