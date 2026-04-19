#Requires -Version 5

<#
.SYNOPSIS
    Micro-benchmarks for the SPE remoting client hot paths.

.DESCRIPTION
    Measures per-call cost of New-Jwt and New-SpeHttpClient in isolation.
    Use before/after a perf change to verify the delta. No container needed;
    this only exercises in-process code.

.PARAMETER Iterations
    Number of iterations per measurement. Default 2000.

.EXAMPLE
    powershell -File tests/benchmarks/Measure-RemotingPerformance.ps1
#>

param(
    [int]$Iterations = 2000
)

$ErrorActionPreference = 'Stop'

$moduleRoot = "$PSScriptRoot\..\..\Modules\SPE"
Import-Module "$moduleRoot\SPE.psd1" -Force

# Expose module-private functions so the benchmark can call them directly.
$speModule = Get-Module SPE
foreach ($fnName in @('New-SpeHttpClient', 'New-Jwt')) {
    $fn = & $speModule { param($n) Get-Command $n -ErrorAction SilentlyContinue } $fnName
    if ($fn) {
        Set-Item "function:global:$fnName" $fn.ScriptBlock
    }
}

function Measure-Block {
    param(
        [string]$Label,
        [scriptblock]$Block,
        [int]$Iterations
    )
    # Warm-up: one pass for JIT/allocator before timing.
    $null = & $Block
    [System.GC]::Collect()
    [System.GC]::WaitForPendingFinalizers()
    [System.GC]::Collect()

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    for ($i = 0; $i -lt $Iterations; $i++) {
        $null = & $Block
    }
    $sw.Stop()
    $meanUs = ($sw.Elapsed.TotalMilliseconds * 1000) / $Iterations
    [PSCustomObject]@{
        Label        = $Label
        Iterations   = $Iterations
        TotalMs      = [math]::Round($sw.Elapsed.TotalMilliseconds, 2)
        MeanUs       = [math]::Round($meanUs, 2)
    }
}

$secret   = "Bench-Secret-Exactly-Long-Enough-For-Validation-1234"
$audience = "https://bench.local"
$issuer   = "SPE Remoting"
$keyId    = "spe_bench_key_01"
$uri      = [Uri]$audience

Write-Host "`n=== SPE Remoting Micro-Benchmarks ===" -ForegroundColor Cyan
Write-Host "Iterations: $Iterations per test`n"

$results = @()

# 1. New-Jwt cost per call.
$results += Measure-Block -Label "New-Jwt (HS256)" -Iterations $Iterations -Block {
    New-Jwt -Algorithm HS256 -Issuer $issuer -Audience $audience `
        -Name "bench" -SecretKey $secret -ValidForSeconds 30
}

# 2. New-SpeHttpClient cost per call with a stable URI cache.
# Measures the full auth-header refresh path (builds a JWT every call in the
# pre-caching baseline; amortizes near zero after caching lands).
$clientCache = @{}
$results += Measure-Block -Label "New-SpeHttpClient (same uri, stable cache)" -Iterations $Iterations -Block {
    New-SpeHttpClient -SharedSecret $secret -AccessKeyId $keyId -Uri $uri `
        -Cache $clientCache -Algorithm "HS256"
}

# 3. New-SpeHttpClient with Name (config-based shared-secret path).
$clientCache2 = @{}
$results += Measure-Block -Label "New-SpeHttpClient (name-bound)" -Iterations $Iterations -Block {
    New-SpeHttpClient -Username "sitecore\admin" -SharedSecret $secret -Uri $uri `
        -Cache $clientCache2 -Algorithm "HS256"
}

$results | Format-Table -AutoSize
