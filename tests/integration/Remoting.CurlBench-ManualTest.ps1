# Remoting.CurlBench-ManualTest.ps1
#
# Generates a curl --parallel config that drives the remoting handler with
# pre-minted JWT + pre-gzipped bodies. Bypasses the SPE module entirely so
# latency isolates the server-side request/response path.
#
# Use this when you need to attribute a perf delta to client-side cost
# (HttpClient pool, JWT mint, gzip overhead) vs server-side cost (handler
# dispatch, ScriptProcessor, audit log). Remoting.LoadTest-ManualTest.ps1
# drives the same endpoint *through* the SPE module - run both and compare.
#
# Usage:
#   .\Remoting.CurlBench-ManualTest.ps1 -Requests 1000
#   .\Remoting.CurlBench-ManualTest.ps1 -Requests 5000 -ConnectionUri https://spe.dev.local
#
# Then drive the generated config:
#   curl -K <output>\curl.cfg --parallel --parallel-immediate --parallel-max 32 -k
#
# Output (under tests/integration/_loadtest-results/curl-bench/ by default,
# which is gitignored):
#   curl.cfg     - curl --config to drive --parallel
#   bodies\*.gz  - one gzipped body per request (matches Compress-SpeRequestBody)

[CmdletBinding()]
param(
    [ValidateRange(1, 100000)]
    [int]$Requests = 1000,

    [string]$ConnectionUri = 'https://spe.dev.local',

    # Output directory. Default lives under tests/integration/_loadtest-results/
    # which is gitignored.
    [string]$OutputDir,

    # JWT validity window. Tune up if you plan to drive a long curl run; a
    # 1000-request --parallel-max 32 batch comfortably finishes inside 600s.
    [ValidateRange(60, 3600)]
    [int]$TokenValiditySeconds = 600
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
. "$repoRoot\scripts\assert-prerequisites.ps1"

$modulePath = "$repoRoot\modules\SPE\SPE.psd1"
Import-Module $modulePath -Force

$sharedSecret = Get-EnvValue "SPE_SHARED_SECRET"
if ([string]::IsNullOrWhiteSpace($sharedSecret)) {
    throw "SPE_SHARED_SECRET is not set. Run 'task init' first."
}

if (-not $OutputDir) {
    $OutputDir = Join-Path $PSScriptRoot '_loadtest-results\curl-bench'
}

# Long-lived JWT so a curl run with thousands of requests stays inside its
# validity window. Reusing one token across requests is intentional: we are
# measuring the server-side handler, not client JWT mint cost.
$mod = Get-Module SPE
$token = & $mod {
    param($secret, $audience, $validForSeconds)
    New-Jwt -Algorithm 'HS256' -Issuer 'SPE Remoting' -Audience $audience `
        -Name 'sitecore\admin' -SecretKey $secret -ValidForSeconds $validForSeconds
} $sharedSecret $ConnectionUri $TokenValiditySeconds

if (Test-Path $OutputDir) { Remove-Item $OutputDir -Recurse -Force }
New-Item -ItemType Directory -Path $OutputDir | Out-Null
$bodyDir = Join-Path $OutputDir 'bodies'
New-Item -ItemType Directory -Path $bodyDir | Out-Null

# Helper: gzip a string body to a file (matches Compress-SpeRequestBody).
function Compress-Body {
    param([string]$Text, [string]$Path)
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Text)
    $ms = New-Object System.IO.MemoryStream
    $gz = New-Object System.IO.Compression.GZipStream($ms, [System.IO.Compression.CompressionMode]::Compress, $true)
    $gz.Write($bytes, 0, $bytes.Length); $gz.Close()
    [System.IO.File]::WriteAllBytes($Path, $ms.ToArray())
}

# Body format Invoke-RemoteScript posts:
#   "<scriptText><#<URL sessionId>#><params>"
# The server splits on `<#{urlSessionId}#>` (ScriptProcessor.cs:139), so each
# request needs a body whose marker matches the URL sessionId. Generate one
# small gzipped body per request.
$script = "'OK'"

$configPath = Join-Path $OutputDir 'curl.cfg'
$writer = [System.IO.StreamWriter]::new($configPath, $false, [System.Text.Encoding]::ASCII)

$baseUrl = "$ConnectionUri/-/script/script/?sessionId={0}&rawOutput=True&outputFormat=Raw&persistentSession=False"

for ($i = 0; $i -lt $Requests; $i++) {
    $sid = [guid]::NewGuid().ToString()
    $bodyPath = Join-Path $bodyDir "$i.gz"
    Compress-Body -Text "$script<#$sid#>" -Path $bodyPath
    # curl config treats backslashes as escapes inside quoted strings, which
    # would mangle Windows paths. Forward slashes work on both sides.
    $bodyPathFwd = $bodyPath -replace '\\', '/'

    $url = $baseUrl -f $sid
    if ($i -gt 0) { $writer.WriteLine('next') }
    $writer.WriteLine("url = `"$url`"")
    $writer.WriteLine("data-binary = `"@$bodyPathFwd`"")
    $writer.WriteLine("header = `"Authorization: Bearer $token`"")
    $writer.WriteLine("header = `"Content-Encoding: gzip`"")
    $writer.WriteLine("header = `"Content-Type: text/plain`"")
    # In --parallel mode, command-line -o / -w only applies to the first
    # transfer; per-request output and write-out have to live in the config.
    $writer.WriteLine('output = "/dev/null"')
    $writer.WriteLine('write-out = "%{http_code},%{time_total}\n"')
}
$writer.Close()

# Sanity: drive one request via the SPE module (real client path) so we know
# the endpoint accepts our payload shape before curl drives it.
$session = New-ScriptSession -Username 'sitecore\admin' -SharedSecret $sharedSecret -ConnectionUri $ConnectionUri
$probe = Invoke-RemoteScript -Session $session -ScriptBlock { 'OK' } -Raw
Stop-ScriptSession -Session $session

$firstSample = Get-ChildItem $bodyDir | Select-Object -First 1
$configFwd = $configPath -replace '\\', '/'

Write-Host ""
Write-Host ("probe (SPE module) : '{0}'" -f $probe)
Write-Host ("requests planned   : {0}" -f $Requests)
Write-Host ("first sample body  : {0} ({1} B)" -f $firstSample.Name, $firstSample.Length)
Write-Host ("curl config        : {0}" -f $configPath)
Write-Host ("token validity     : {0} s" -f $TokenValiditySeconds)
Write-Host ""
Write-Host "Run with:"
Write-Host ("  curl -K {0} --parallel --parallel-immediate --parallel-max 32 -k" -f $configFwd)
