<#
.SYNOPSIS
    Removes build artifacts and deployment output.
#>
$ErrorActionPreference = "SilentlyContinue"
$projectRoot = (Resolve-Path "$PSScriptRoot/..").Path

# Clean deploy folder (keep .gitkeep)
$deployDir = Join-Path $projectRoot "docker\deploy"
if (Test-Path $deployDir) {
    Get-ChildItem $deployDir -Exclude .gitkeep | Remove-Item -Recurse -Force
}

# Clean solr folder (keep .gitkeep)
$solrDir = Join-Path $projectRoot "docker\data\solr"
if (Test-Path $solrDir) {
    Get-ChildItem $solrDir -Exclude .gitkeep | Remove-Item -Recurse -Force
}

# Clean cm folder (keep .gitkeep)
$cmDir = Join-Path $projectRoot "docker\data\cm"
if (Test-Path $cmDir) {
    Get-ChildItem $cmDir -Exclude .gitkeep | Remove-Item -Recurse -Force
}

# Clean build artifacts
$paths = @(
    "_output\Sitecore.PowerShell.Extensions-*"
    "_output\SPE.*"
    "_output\extracted-module"
    "_output\docker-module"
    "src\*\bin"
    "src\*\obj"
)

foreach ($p in $paths) {
    $full = Join-Path $projectRoot $p
    $resolved = Resolve-Path $full -ErrorAction SilentlyContinue
    if ($resolved) {
        Remove-Item $resolved -Recurse -Force
    }
}
