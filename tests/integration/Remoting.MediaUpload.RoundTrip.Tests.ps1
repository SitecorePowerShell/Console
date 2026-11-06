# Remoting Tests - Media Upload round-trip fidelity
# Upload a file, pull it back down, and assert byte-level equality via SHA256.
# This is the single highest-value test we were missing: all previous coverage
# asserted Test-Path only, not whether the media stream matched the source.

Write-Host "`n  [Media Upload - round-trip byte fidelity]" -ForegroundColor White

function Get-FileHashHex {
    # Get-FileHash is missing in the test harness's PowerShell runtime, so use
    # System.Security.Cryptography directly.
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
$downloadRoot = Join-Path $env:TEMP ("spe-roundtrip-" + [guid]::NewGuid().ToString("N").Substring(0,8))
New-Item -Path $downloadRoot -ItemType Directory -Force | Out-Null

try {
    $source = Join-Path $localFilePath "kitten.jpg"
    $sourceHash = Get-FileHashHex -Path $source

    # Case 1: upload by name, download by media path
    $destFolder = "Images/spe-roundtrip"
    Get-Item -Path $source | Send-RemoteItem -Session $session -RootPath Media -Destination "$destFolder/rt1.jpg"

    $downloaded1 = Join-Path $downloadRoot "rt1.jpg"
    Receive-RemoteItem -Session $session -Path "/sitecore/media library/images/spe-roundtrip/rt1" `
        -Destination $downloaded1 -Database master -Force
    Assert-True (Test-Path $downloaded1) "Round-trip 1: downloaded file exists"
    Assert-Equal (Get-FileHashHex -Path $downloaded1) $sourceHash "Round-trip 1: SHA256 matches (path-based)"

    # Case 2: round-trip by item ID
    $itemId = Invoke-RemoteScript -Session $session -ScriptBlock {
        (Get-Item -Path "master:\media library\images\spe-roundtrip\rt1").ID.ToString()
    } -Raw
    $itemId = $itemId.Trim()
    Assert-True ($itemId -match "^\{[0-9A-Fa-f-]{36}\}$") "Round-trip 2: item ID resolved ($itemId)"

    $downloaded2 = Join-Path $downloadRoot "rt2.jpg"
    Receive-RemoteItem -Session $session -Path $itemId -Destination $downloaded2 -Database master -Force
    Assert-Equal (Get-FileHashHex -Path $downloaded2) $sourceHash "Round-trip 2: SHA256 matches (GUID-addressed)"

    # Case 3: replace existing media, round-trip to confirm new stream
    $replacement = Join-Path $localFilePath "kitten-replacement.jpg"
    $replacementHash = Get-FileHashHex -Path $replacement
    Get-Item -Path $replacement | Send-RemoteItem -Session $session -RootPath Media -Destination $itemId

    $downloaded3 = Join-Path $downloadRoot "rt3.jpg"
    Receive-RemoteItem -Session $session -Path $itemId -Destination $downloaded3 -Database master -Force
    Assert-Equal (Get-FileHashHex -Path $downloaded3) $replacementHash "Round-trip 3: SHA256 matches replacement after overwrite"
    Assert-NotEqual (Get-FileHashHex -Path $downloaded3) $sourceHash "Round-trip 3: hash differs from original"
}
finally {
    Invoke-RemoteScript -Session $session -ScriptBlock {
        Remove-Item -Path "master:\media library\images\spe-roundtrip\" -Recurse -ErrorAction SilentlyContinue
    } -ErrorAction SilentlyContinue
    Remove-Item -Path $downloadRoot -Recurse -Force -ErrorAction SilentlyContinue
    Stop-ScriptSession -Session $session
}
