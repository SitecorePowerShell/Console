# Measures the cost / payoff of gzip-compressing the request body in
# Compress-SpeRequestBody across realistic script-body sizes. Used to
# pick a sensible "skip gzip below N bytes" threshold for the client
# cmdlet.
#
# What it shows for each size:
#   raw=B in   gz=B out   ratio   gzMs/call   rawMs/call
# Where ratio = gz / raw. A ratio above 1.0 means gzipping makes the
# wire bytes BIGGER (the gzip header is ~18 bytes on its own).
#
# Usage:
#   .\tests\benchmarks\Measure-CompressionThreshold.ps1
#   .\tests\benchmarks\Measure-CompressionThreshold.ps1 -Iterations 5000

[CmdletBinding()]
param(
    [int]$Iterations = 2000
)

# Generate a script-body sample of approximately the requested byte count.
# Uses repeatable PowerShell-flavored text rather than random bytes so the
# compression ratio matches what a real script body produces.
function New-SampleBody {
    param([int]$TargetBytes)
    $unit = "Get-Item -Path 'master:/sitecore/content/Home' | Select-Object Name, Id, TemplateName; "
    $unitBytes = [System.Text.Encoding]::UTF8.GetByteCount($unit)
    $reps = [Math]::Max(1, [int][Math]::Ceiling($TargetBytes / $unitBytes))
    $sb = [System.Text.StringBuilder]::new($TargetBytes + $unitBytes)
    for ($i = 0; $i -lt $reps; $i++) { [void]$sb.Append($unit) }
    $text = $sb.ToString()
    if ($text.Length -gt $TargetBytes) { $text = $text.Substring(0, $TargetBytes) }
    return $text
}

function Compress-Gzip {
    param([byte[]]$Bytes)
    $ms = New-Object System.IO.MemoryStream
    $gz = New-Object System.IO.Compression.GZipStream($ms, [System.IO.Compression.CompressionMode]::Compress, $true)
    $gz.Write($Bytes, 0, $Bytes.Length)
    $gz.Close()
    $out = $ms.ToArray()
    $ms.Close()
    return $out
}

# Mirrors Compress-SpeRequestBody allocations: encode -> gzip -> ByteArrayContent.
function Invoke-CompressLikeCmdlet {
    param([string]$Body)
    $messageBytes = [System.Text.Encoding]::UTF8.GetBytes($Body)
    $ms = New-Object System.IO.MemoryStream
    $gzip = New-Object System.IO.Compression.GZipStream($ms, [System.IO.Compression.CompressionMode]::Compress, $true)
    $gzip.Write($messageBytes, 0, $messageBytes.Length)
    $gzip.Close()
    $ms.Position = 0
    $content = New-Object System.Net.Http.ByteArrayContent(@(, $ms.ToArray()))
    $ms.Close()
    $content.Headers.ContentType = New-Object System.Net.Http.Headers.MediaTypeHeaderValue("text/plain")
    $content.Headers.ContentEncoding.Add("gzip") > $null
    return $content
}

# Mirrors what Compress-SpeRequestBody would do in the no-gzip branch.
function Invoke-CompressRaw {
    param([string]$Body)
    $messageBytes = [System.Text.Encoding]::UTF8.GetBytes($Body)
    $content = New-Object System.Net.Http.ByteArrayContent(@(, $messageBytes))
    $content.Headers.ContentType = New-Object System.Net.Http.Headers.MediaTypeHeaderValue("text/plain")
    return $content
}

Add-Type -AssemblyName System.Net.Http

# Warm up: JIT and allocate the GZipStream type once.
$warmBody = New-SampleBody -TargetBytes 1024
1..50 | ForEach-Object { [void](Invoke-CompressLikeCmdlet -Body $warmBody) }

$sizes = 64, 128, 256, 512, 768, 1024, 2048, 4096, 8192, 16384, 65536
$results = @()

foreach ($size in $sizes) {
    $body = New-SampleBody -TargetBytes $size
    $rawBytes = [System.Text.Encoding]::UTF8.GetByteCount($body)
    $gzipped = Compress-Gzip -Bytes ([System.Text.Encoding]::UTF8.GetBytes($body))
    $gzBytes = $gzipped.Length

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    for ($i = 0; $i -lt $Iterations; $i++) { [void](Invoke-CompressLikeCmdlet -Body $body) }
    $sw.Stop()
    $gzMs = $sw.Elapsed.TotalMilliseconds / $Iterations

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    for ($i = 0; $i -lt $Iterations; $i++) { [void](Invoke-CompressRaw -Body $body) }
    $sw.Stop()
    $rawMs = $sw.Elapsed.TotalMilliseconds / $Iterations

    $results += [pscustomobject]@{
        RawBytes  = $rawBytes
        GzipBytes = $gzBytes
        Ratio     = [Math]::Round($gzBytes / $rawBytes, 2)
        GzMsCall  = [Math]::Round($gzMs, 4)
        RawMsCall = [Math]::Round($rawMs, 4)
        BytesSaved = $rawBytes - $gzBytes
    }
}

$results | Format-Table -AutoSize

Write-Host ""
Write-Host "Read columns as follows:"
Write-Host "  Ratio      gzip out / raw in. >1.0 means gzip is making the body BIGGER."
Write-Host "  GzMsCall   wall time per Compress-SpeRequestBody call (gzip path)."
Write-Host "  RawMsCall  same call without the gzip path (allocation only)."
Write-Host "  BytesSaved raw - gzip; negative means gzip is hurting the wire."
