#Requires -Version 5.1
<#
.SYNOPSIS
    Checks that every SCS module include is packaged by Prepare Console Distribution, and nothing else.
.DESCRIPTION
    Static check, no Sitecore needed. Compares the include paths in serialization/modules/*.module.json
    with the quoted Sitecore paths in the packaging script. Exits 1 when they differ.
#>

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path "$PSScriptRoot/..").Path
$modulesDir = Join-Path $projectRoot "serialization/modules"
$packageScript = Join-Path $modulesDir "serialization/SPE/SPE/Core/Platform/Development/PowerShell Extensions Maintenance/Prepare Console Distribution.yml"

$includes = @{}
foreach ($file in Get-ChildItem -Path $modulesDir -Filter "*.module.json") {
    $module = Get-Content $file.FullName -Raw | ConvertFrom-Json
    if (-not ($module.PSObject.Properties["items"] -and $module.items.PSObject.Properties["includes"])) { continue }
    foreach ($include in $module.items.includes) {
        $includes["$($include.database):$($include.path)"] = $module.namespace
    }
}

# Unprefixed paths are master: the Spe.Rules list adds "master:" when it queries them
$packaged = @{}
foreach ($match in [regex]::Matches((Get-Content $packageScript -Raw), '"(?:(master|core):)?(/sitecore/[^"$]*)"')) {
    $db = if ($match.Groups[1].Success) { $match.Groups[1].Value } else { "master" }
    $packaged["$($db):$($match.Groups[2].Value.TrimEnd('/'))"] = $true
}

$notPackaged = @($includes.Keys | Where-Object { -not $packaged.ContainsKey($_) } | Sort-Object)
$notSerialized = @($packaged.Keys | Where-Object { -not $includes.ContainsKey($_) } | Sort-Object)

foreach ($key in $notPackaged) {
    Write-Host "  FAIL: $($includes[$key]) include $key is not in Prepare Console Distribution" -ForegroundColor Red
}
foreach ($key in $notSerialized) {
    Write-Host "  FAIL: Prepare Console Distribution packages $key, which no module.json includes" -ForegroundColor Red
}

if ($notPackaged.Count + $notSerialized.Count -gt 0) {
    Write-Host "  Keep the Prepare Console Distribution paths in step with the module.json includes (SingleItem: Get-Item, ItemAndDescendants: Get-ChildItem -Recurse -WithParent)." -ForegroundColor Red
    exit 1
}

Write-Host "  PASS: All $($includes.Count) module.json includes are packaged" -ForegroundColor Green
exit 0
