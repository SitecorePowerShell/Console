# Unit tests for the gzip-skip threshold inside Compress-SpeRequestBody.
# Verifies the threshold gate: small bodies ship as plaintext (no
# Content-Encoding: gzip), large bodies still gzip with the right header.
# Run-Tests.ps1 dot-sources Invoke-RemoteScript.ps1 so the function is in
# scope here.

Add-Type -AssemblyName System.Net.Http

# Helper: pulls a single Content-Encoding value or $null.
function Get-ContentEncodingValue {
    param([System.Net.Http.ByteArrayContent]$Content)
    if (-not $Content -or -not $Content.Headers -or -not $Content.Headers.ContentEncoding) { return $null }
    if ($Content.Headers.ContentEncoding.Count -eq 0) { return $null }
    return ($Content.Headers.ContentEncoding | Select-Object -First 1)
}

# Build bodies just above and just below the threshold so the gate can be
# exercised regardless of what value the threshold is set to.
$threshold = $script:CompressionThresholdBytes
Assert-True ($threshold -gt 0) "CompressionThresholdBytes is set"

$smallBody = "x" * ($threshold - 1)
$largeBody = "x" * ($threshold + 1)

# ============================================================
# Below threshold: plaintext, no Content-Encoding header
# ============================================================
Write-Host "`n  [Compress-SpeRequestBody below threshold]" -ForegroundColor White

$smallContent = Compress-SpeRequestBody -Body $smallBody
Assert-NotNull $smallContent "Returns a content object for sub-threshold body"
Assert-Null (Get-ContentEncodingValue $smallContent) "No Content-Encoding header below threshold"

$smallBytes = $smallContent.ReadAsByteArrayAsync().Result
Assert-Equal $smallBytes.Length ($threshold - 1) "Sub-threshold body wires through unchanged byte length"

# ============================================================
# Above threshold: gzipped, Content-Encoding: gzip set
# ============================================================
Write-Host "`n  [Compress-SpeRequestBody above threshold]" -ForegroundColor White

$largeContent = Compress-SpeRequestBody -Body $largeBody
Assert-NotNull $largeContent "Returns a content object for above-threshold body"
Assert-Equal (Get-ContentEncodingValue $largeContent) "gzip" "Content-Encoding: gzip set above threshold"

$largeBytes = $largeContent.ReadAsByteArrayAsync().Result
Assert-True ($largeBytes.Length -lt ($threshold + 1)) "Above-threshold body is smaller after gzip than raw"

# Verify the gzipped payload round-trips back to the original text.
$ms = New-Object System.IO.MemoryStream(@(, $largeBytes))
$gz = New-Object System.IO.Compression.GZipStream($ms, [System.IO.Compression.CompressionMode]::Decompress)
$reader = New-Object System.IO.StreamReader($gz, [System.Text.Encoding]::UTF8)
$decoded = $reader.ReadToEnd()
$reader.Close()
Assert-Equal $decoded $largeBody "Gzipped payload decompresses to the original body"

# ============================================================
# At threshold boundary: exactly threshold-byte body still skips gzip
# ============================================================
Write-Host "`n  [Compress-SpeRequestBody at threshold boundary]" -ForegroundColor White

# The function uses `-lt` so a body exactly at the threshold gets gzipped.
$exactBody = "x" * $threshold
$exactContent = Compress-SpeRequestBody -Body $exactBody
Assert-Equal (Get-ContentEncodingValue $exactContent) "gzip" "Body exactly at threshold gets gzipped (boundary uses -lt)"
