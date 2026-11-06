#Requires -Version 5

<#
.SYNOPSIS
    End-to-end micro-benchmark for the SPE remoting server handler.

.DESCRIPTION
    Measures round-trip wall-clock time for common shapes of request that
    flow through RemoteScriptCall.ashx. Intended for before/after
    comparisons when changing server-side hot paths: body read, parameter
    filter, IP lookup, script hashing, ApiScripts cache.

    Requires a running Sitecore CM container (task up) and a
    SPE_SHARED_SECRET in .env. End-to-end latency includes TLS, IIS
    worker-thread scheduling and the Sitecore pipeline; small per-request
    wins (a few microseconds of CPU) sit below the noise floor here. Use
    this to catch regressions of order ~1ms and up, not micro-optimisation
    deltas.

.PARAMETER Iterations
    Samples per measurement. Default 200.

.PARAMETER ConnectionUri
    Target host. Defaults to https://spe.dev.local.

.EXAMPLE
    powershell -File tests/benchmarks/Measure-ServerHandler.ps1
#>

param(
    [int]$Iterations = 200,
    [string]$ConnectionUri = "https://spe.dev.local"
)

$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\..\..\scripts\assert-prerequisites.ps1"

$moduleRoot = "$PSScriptRoot\..\..\modules\SPE"
Import-Module "$moduleRoot\SPE.psd1" -Force

$sharedSecret = Get-EnvValue "SPE_SHARED_SECRET"

function Measure-Samples {
    param(
        [string]$Label,
        [scriptblock]$Block,
        [int]$Iterations
    )
    # Warm up - one pass so JIT/TLS/handler caches are hot before timing.
    $null = & $Block

    $samples = New-Object 'System.Collections.Generic.List[double]' $Iterations
    for ($i = 0; $i -lt $Iterations; $i++) {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $null = & $Block
        $sw.Stop()
        $samples.Add($sw.Elapsed.TotalMilliseconds)
    }

    $sorted = $samples | Sort-Object
    $p50 = $sorted[[int]($Iterations * 0.50)]
    $p95 = $sorted[[int]($Iterations * 0.95)]
    $mean = ($samples | Measure-Object -Average).Average

    [PSCustomObject]@{
        Label      = $Label
        Iterations = $Iterations
        MeanMs     = [math]::Round($mean, 2)
        P50Ms      = [math]::Round($p50, 2)
        P95Ms      = [math]::Round($p95, 2)
    }
}

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $ConnectionUri
try {
    # Build payloads up front so the timed block measures only server-side cost.
    $smallScript = { Get-Item "master:/sitecore" | Select-Object -ExpandProperty Name }

    # ~200 KB script body: many small statements so parsing is non-trivial but
    # the body read path dominates. Stresses #1 (single-copy body read).
    $largeStatements = (1..4000 | ForEach-Object { "`$x$_ = $_" }) -join "`n"
    $largeScript = [scriptblock]::Create("$largeStatements`n'OK'")

    Write-Host "`n=== SPE Server Handler Benchmarks ===" -ForegroundColor Cyan
    Write-Host "Target: $ConnectionUri"
    Write-Host "Iterations: $Iterations per test`n"

    $results = @()

    $results += Measure-Samples -Label "Small script round-trip" -Iterations $Iterations -Block {
        Invoke-RemoteScript -Session $session -ScriptBlock $smallScript
    }

    $results += Measure-Samples -Label "Large body (~200KB) round-trip" -Iterations $Iterations -Block {
        Invoke-RemoteScript -Session $session -ScriptBlock $largeScript
    }

    # v2 endpoint: exercises GetApiScripts cache on every hit (second call onwards).
    # Depends on "Getting Started" API scripts being enabled; skip gracefully.
    $v2Uri = "$ConnectionUri/-/script/v2/master/ChildrenAsJson?user=sitecore%5Cadmin&password=b"
    try {
        $null = Invoke-RestMethod -Uri $v2Uri -ErrorAction Stop
        $results += Measure-Samples -Label "v2 cache-hit round-trip" -Iterations $Iterations -Block {
            Invoke-RestMethod -Uri $v2Uri
        }
    } catch {
        Write-Host "  Skipping v2 cache-hit benchmark: $($_.Exception.Message)" -ForegroundColor Yellow
    }

    $results | Format-Table -AutoSize
}
finally {
    Stop-ScriptSession -Session $session -ErrorAction SilentlyContinue
}
