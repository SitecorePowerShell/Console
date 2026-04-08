<#
.SYNOPSIS
    Benchmarks SPE remoting endpoint performance.

.DESCRIPTION
    Measures request latency across multiple scenarios to compare
    before/after performance of RemoteScriptCall.ashx optimizations.

    Run against two branches (e.g. master vs feature/performance)
    and compare the output.

.PARAMETER ConnectionUri
    The Sitecore CM instance URL. Default: https://spe.dev.local

.PARAMETER Iterations
    Number of requests per test scenario. Default: 50

.PARAMETER WarmupIterations
    Number of warmup requests (not counted). Default: 5

.EXAMPLE
    # 1. Start on master branch, deploy, and run:
    git checkout master
    task deploy
    .\tests\benchmark\Measure-RemotingPerformance.ps1 | Tee-Object -FilePath benchmark-master.txt

    # 2. Switch to feature branch, deploy, and run:
    git checkout feature/performance
    task deploy
    .\tests\benchmark\Measure-RemotingPerformance.ps1 | Tee-Object -FilePath benchmark-feature.txt

    # 3. Compare the two output files.
#>

param(
    [string]$ConnectionUri = "https://spe.dev.local",
    [int]$Iterations = 50,
    [int]$WarmupIterations = 5
)

$ErrorActionPreference = "Stop"

# --- Setup ---
$repoRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
. "$repoRoot\scripts\assert-prerequisites.ps1"
Assert-SpePrerequisites

$moduleRoot = "$repoRoot\modules\SPE"
Import-Module "$moduleRoot\SPE.psd1" -Force

$global:protocolHost = $ConnectionUri
$global:sharedSecret = Get-EnvValue "SPE_SHARED_SECRET"

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $ConnectionUri

# --- Helpers ---
function Measure-Scenario {
    param(
        [string]$Name,
        [scriptblock]$ScriptBlock,
        [int]$Iterations,
        [int]$WarmupIterations
    )

    Write-Host "`n--- $Name ---" -ForegroundColor Cyan
    Write-Host "  Warmup: $WarmupIterations iterations"

    for ($i = 0; $i -lt $WarmupIterations; $i++) {
        & $ScriptBlock | Out-Null
    }

    Write-Host "  Measuring: $Iterations iterations"
    $timings = [System.Collections.Generic.List[double]]::new($Iterations)

    for ($i = 0; $i -lt $Iterations; $i++) {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        & $ScriptBlock | Out-Null
        $sw.Stop()
        $timings.Add($sw.Elapsed.TotalMilliseconds)
    }

    $sorted = $timings | Sort-Object
    $avg = ($sorted | Measure-Object -Average).Average
    $min = $sorted[0]
    $max = $sorted[-1]
    $p50 = $sorted[[math]::Floor($Iterations * 0.50)]
    $p95 = $sorted[[math]::Floor($Iterations * 0.95)]
    $p99 = $sorted[[math]::Floor($Iterations * 0.99)]

    $stats = [PSCustomObject]@{
        Scenario = $Name
        Avg      = [math]::Round($avg, 2)
        Min      = [math]::Round($min, 2)
        P50      = [math]::Round($p50, 2)
        P95      = [math]::Round($p95, 2)
        P99      = [math]::Round($p99, 2)
        Max      = [math]::Round($max, 2)
    }

    $stats | Format-Table -AutoSize | Out-String | Write-Host
    return $stats
}

# --- Scenarios ---
Write-Host "============================================" -ForegroundColor Green
Write-Host " SPE Remoting Performance Benchmark" -ForegroundColor Green
Write-Host " Branch: $(git -C $repoRoot rev-parse --abbrev-ref HEAD)" -ForegroundColor Green
Write-Host " Commit: $(git -C $repoRoot rev-parse --short HEAD)" -ForegroundColor Green
Write-Host " Date:   $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Green
Write-Host " Iterations: $Iterations (warmup: $WarmupIterations)" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green

$results = @()

# 1. Minimal script -- measures baseline request overhead
#    (session creation, param setup, script execution, teardown)
$results += Measure-Scenario -Name "Minimal script (echo)" -Iterations $Iterations -WarmupIterations $WarmupIterations -ScriptBlock {
    Invoke-RemoteScript -Session $session -ScriptBlock { "hello" }
}

# 2. Script with parameters -- exercises the Request.Params loop
$results += Measure-Scenario -Name "Script with query params" -Iterations $Iterations -WarmupIterations $WarmupIterations -ScriptBlock {
    Invoke-RemoteScript -Session $session -ScriptBlock {
        param($foo, $bar)
        "$foo-$bar"
    } -Arguments @{ foo = "value1"; bar = "value2" }
}

# 3. Raw output mode -- exercises the rawOutput path
$results += Measure-Scenario -Name "Raw output" -Iterations $Iterations -WarmupIterations $WarmupIterations -ScriptBlock {
    Invoke-RemoteScript -Session $session -ScriptBlock { "raw" } -Raw
}

# 4. Larger payload -- returns more data, exercises serialization
$heavyScript = {
    1..500 | ForEach-Object {
        [PSCustomObject]@{
            Index = $_; Name = "Item_$_"; Path = "/sitecore/content/Home/Item_$_"
            ID = [guid]::NewGuid(); Template = "Sample Item"; Language = "en"
            Version = 1; Created = [datetime]::Now; Updated = [datetime]::Now
            Fields = @{ Title = "Title_$_"; Body = "Lorem ipsum dolor sit amet " * 10 }
        }
    }
}

$results += Measure-Scenario -Name "Heavy result (500 objects, CliXml)" -Iterations $Iterations -WarmupIterations $WarmupIterations -ScriptBlock {
    Invoke-RemoteScript -Session $session -ScriptBlock $heavyScript
}

# 5. JSON output mode -- exercises the JSON serialization path
$results += Measure-Scenario -Name "Heavy result (500 objects, JSON)" -Iterations $Iterations -WarmupIterations $WarmupIterations -ScriptBlock {
    Invoke-RemoteScript -Session $session -OutputFormat Json -ScriptBlock $heavyScript
}

# 6. Raw output mode with heavy payload for comparison
$results += Measure-Scenario -Name "Heavy result (500 objects, Raw)" -Iterations $Iterations -WarmupIterations $WarmupIterations -ScriptBlock {
    Invoke-RemoteScript -Session $session -OutputFormat Raw -ScriptBlock $heavyScript
}

# 7. API v2 endpoint -- exercises the dictionary lookup path (GetApiScripts)
#    This requires a script registered in the Web API integration point.
#    If none exist, this scenario is skipped.
Write-Host "`n--- API v2 lookup (GetApiScripts cache) ---" -ForegroundColor Cyan
try {
    $testUri = "$ConnectionUri/-/script/v2/master/dummy?apiVersion=2&script=dummy"
    $headers = @{ Authorization = "Bearer $(New-Jwt -Algorithm 'HS256' -Issuer 'SPE Remoting' -Audience ($ConnectionUri) -Name 'sitecore\admin' -SecretKey $sharedSecret -ValidforSeconds 30)" }
    # Just hit the endpoint to exercise GetApiScripts; 404 is expected if no scripts registered
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try { Invoke-WebRequest -Uri $testUri -Headers $headers -UseBasicParsing -ErrorAction SilentlyContinue } catch {}
    $sw.Stop()
    Write-Host "  Single API v2 probe: $([math]::Round($sw.Elapsed.TotalMilliseconds, 2))ms (exercises dictionary lookup + cache)" -ForegroundColor Yellow
} catch {
    Write-Host "  Skipped -- could not probe API v2 endpoint" -ForegroundColor Yellow
}

# 8. Persistent session -- measures without session teardown overhead
$persistentSession = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $ConnectionUri

$results += Measure-Scenario -Name "Persistent session (no teardown)" -Iterations $Iterations -WarmupIterations $WarmupIterations -ScriptBlock {
    Invoke-RemoteScript -Session $persistentSession -ScriptBlock { "hello" }
}

# --- Summary ---
Write-Host "`n============================================" -ForegroundColor Green
Write-Host " Summary (all times in ms)" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
$results | Format-Table -AutoSize
