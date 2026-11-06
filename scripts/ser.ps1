<#
.SYNOPSIS
    Runs a Sitecore CLI command after ensuring login.
.PARAMETER Category
    The CLI category: ser (serialization) or index.
.PARAMETER Command
    The sub-command to run within the category.
    ser  : pull, push, watch
    index: list, rebuild, schema-populate
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet("ser", "index")]
    [string]$Category,

    [Parameter(Mandatory)]
    [string]$Command
)

# Validate command against category
$validCommands = @{
    "ser"   = @("pull", "push", "watch")
    "index" = @("list", "rebuild", "schema-populate")
}

if ($Command -notin $validCommands[$Category]) {
    $allowed = $validCommands[$Category] -join ", "
    Write-Host "ERROR: Invalid command '$Command' for category '$Category'. Allowed: $allowed" -ForegroundColor Red
    exit 1
}

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

# -- Check if login is needed ----------------------------------------------

function Test-TokenValid {
    $userFile = Join-Path (Join-Path $serRoot ".sitecore") "user.json"
    if (-not (Test-Path $userFile)) { return $false }

    $user = Get-Content $userFile -Raw | ConvertFrom-Json
    $endpoint = $user.endpoints.default
    if (-not $endpoint -or -not $endpoint.accessToken) { return $false }

    # Check that the cached endpoint matches our current hosts
    if ($endpoint.host -ne "https://$cmHost") { return $false }
    if ($endpoint.authority -ne "https://$idHost") { return $false }

    # If a refresh token exists, the CLI can silently renew -- treat as valid
    if ($endpoint.refreshToken) { return $true }

    # No refresh token -- check access token expiry
    if (-not $endpoint.lastUpdated -or -not $endpoint.expiresIn) { return $false }
    $expiry = ([datetime]$endpoint.lastUpdated).AddSeconds($endpoint.expiresIn)
    return (Get-Date).ToUniversalTime() -lt $expiry
}

# -- Ensure tools are restored --------------------------------------------

Push-Location $serRoot
try {
    dotnet tool restore | Out-Null

    # Login to Sitecore only when needed
    if (Test-TokenValid) {
        Write-Host "Using cached Sitecore CLI credentials." -ForegroundColor DarkGray
    } else {
        Write-Host "Logging in to Sitecore (CM: $cmHost, ID: $idHost)..." -ForegroundColor Cyan
        dotnet sitecore login --authority "https://$idHost" --cm "https://$cmHost" --allow-write true
    }

    # Run the CLI command
    Write-Host "Running: dotnet sitecore $Category $Command" -ForegroundColor Cyan
    $output = dotnet sitecore $Category $Command 2>&1
    $outputStr = $output | Out-String

    # Detect invalid_grant (expired/revoked refresh token) and retry after re-login
    if ($outputStr -match "invalid_grant") {
        Write-Host "Refresh token expired. Re-authenticating..." -ForegroundColor Yellow
        dotnet sitecore login --authority "https://$idHost" --cm "https://$cmHost" --allow-write true
        Write-Host "Retrying: dotnet sitecore $Category $Command" -ForegroundColor Cyan
        dotnet sitecore $Category $Command
    } else {
        Write-Output $output
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
} finally {
    Pop-Location
}
