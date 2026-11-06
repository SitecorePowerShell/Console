$ErrorActionPreference = "Stop"
$projectRoot = (Resolve-Path "$PSScriptRoot/..").Path

$envFile = Join-Path $projectRoot ".env"
if (-not (Test-Path $envFile)) {
    Write-Host "ERROR: Missing .env file. Run 'task init' first." -ForegroundColor Red
    exit 1
}

$line = Get-Content $envFile | Where-Object { $_ -match "^CM_HOST=" }
if (-not $line) {
    Write-Host "ERROR: CM_HOST not found in .env" -ForegroundColor Red
    exit 1
}
$cmHost = ($line -replace "^CM_HOST=", "").Trim()

docker compose --project-directory $projectRoot up -d
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: 'docker compose up' failed. Is Docker Desktop running?" -ForegroundColor Red
    exit 1
}
Start-Process "https://$cmHost/sitecore/login"
