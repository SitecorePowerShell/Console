<#
.SYNOPSIS
    Runs a Sitecore Content Serialization (SCS) command after ensuring login.
.PARAMETER Command
    The SCS sub-command to run: pull, push, or watch.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet("pull", "push", "watch")]
    [string]$Command
)

$ErrorActionPreference = "Stop"
$projectRoot = (Resolve-Path "$PSScriptRoot/..").Path
$serRoot = Join-Path $projectRoot "serialization"

# -- Read .env -------------------------------------------------------------

$envFile = Join-Path $projectRoot ".env"
if (-not (Test-Path $envFile)) {
    Write-Host "ERROR: Missing .env file. Run 'task init' first." -ForegroundColor Red
    exit 1
}

function Get-EnvValue {
    param([string]$Key)
    $line = Get-Content $envFile | Where-Object { $_ -match "^$Key=" }
    if (-not $line) { return $null }
    return ($line -replace "^$Key=", "").Trim()
}

$cmHost = Get-EnvValue "CM_HOST"
if (-not $cmHost) {
    Write-Host "ERROR: CM_HOST not found in .env" -ForegroundColor Red
    exit 1
}

$idHost = Get-EnvValue "ID_HOST"
if (-not $idHost) {
    Write-Host "ERROR: ID_HOST not found in .env" -ForegroundColor Red
    exit 1
}

# -- Ensure tools are restored --------------------------------------------

Push-Location $serRoot
try {
    dotnet tool restore | Out-Null

    # Login to Sitecore
    Write-Host "Logging in to Sitecore (CM: $cmHost, ID: $idHost)..." -ForegroundColor Cyan
    dotnet sitecore login --authority "https://$idHost" --cm "https://$cmHost" --allow-write true

    # Run the serialization command
    Write-Host "Running: dotnet sitecore ser $Command" -ForegroundColor Cyan
    dotnet sitecore ser $Command
} finally {
    Pop-Location
}
