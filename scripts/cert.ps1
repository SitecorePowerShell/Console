[CmdletBinding()]
Param (
    [string]$HostName = "dev.local",
    [switch]$Renew,
    [string]$CertDir = (Join-Path $PSScriptRoot "..\docker\traefik\certs"),
    [int]$Days = 200
)

$ErrorActionPreference = "Stop"

# Pinned certz version
$certzVersion = "0.4.0"
$certzUrl = "https://github.com/michaellwest/certz/releases/download/$($certzVersion)/certz-$($certzVersion)-win-x64.exe"
$certzHash = "035B7BA695306E357B9886593A1BB5B3F4799D64E9DEF74B3D73D38108E8B08D"

function Get-SHA256Hash([string]$FilePath) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $stream = [System.IO.File]::OpenRead($FilePath)
        try {
            $hashBytes = $sha.ComputeHash($stream)
            return [BitConverter]::ToString($hashBytes) -replace '-',''
        } finally { $stream.Dispose() }
    } finally { $sha.Dispose() }
}

$CertDir = [System.IO.Path]::GetFullPath($CertDir)
if (-not (Test-Path $CertDir)) {
    New-Item -ItemType Directory -Path $CertDir -Force | Out-Null
}

$certzPath = Join-Path $CertDir "certz.exe"
$PfxFile = Join-Path $CertDir "devcert.pfx"
$CerFile = Join-Path $CertDir "devcert.cer"
$KeyFile = Join-Path $CertDir "devcert.key"
$PasswordFile = Join-Path $CertDir "devcert.password.txt"

# Resolve certz executable
$certz = $null
if ($null -ne (Get-Command certz.exe -ErrorAction SilentlyContinue)) {
    $certz = "certz"
} elseif (Test-Path $certzPath) {
    $certz = $certzPath
    # Verify hash of existing download
    $existingHash = Get-SHA256Hash $certzPath
    if ($existingHash -ne $certzHash) {
        Write-Host "Existing certz.exe hash mismatch, re-downloading..." -ForegroundColor Yellow
        Remove-Item $certzPath -Force
        $certz = $null
    }
}

if (-not $certz) {
    Write-Host "Downloading certz $certzVersion..." -ForegroundColor Green
    (New-Object System.Net.WebClient).DownloadFile($certzUrl, $certzPath)

    $downloadHash = Get-SHA256Hash $certzPath
    if ($downloadHash -ne $certzHash) {
        Remove-Item $certzPath -Force
        throw "certz.exe hash mismatch! Expected: $certzHash Got: $downloadHash"
    }
    $certz = $certzPath
}

# Check for newer certz release
try {
    $release = Invoke-RestMethod "https://api.github.com/repos/michaellwest/certz/releases/latest" -ErrorAction SilentlyContinue
    if ($release.tag_name -and $release.tag_name -ne $certzVersion) {
        Write-Host "Info: A newer certz version is available: $($release.tag_name) (pinned: $certzVersion)" -ForegroundColor Cyan
    }
} catch {
    # Non-fatal -- skip version check if offline or rate-limited
}

if ($Renew) {
    if (-not (Test-Path $PfxFile)) {
        throw "Cannot renew: $PfxFile not found. Run without -Renew first."
    }
    $password = (Get-Content $PasswordFile -Raw).Trim()
    Write-Host "Renewing certificate..." -ForegroundColor Green
    & $certz renew $PfxFile --password $password --days $Days --out $PfxFile
} else {
    Write-Host "Generating TLS certificate..." -ForegroundColor Green
    & $certz create dev $HostName --san "*.$HostName" --san "localhost" --key-type RSA --pfx-encryption legacy --file $PfxFile --password-file $PasswordFile --cert $CerFile --key $KeyFile --days $Days
}

# Trust the certificate
$password = (Get-Content $PasswordFile -Raw).Trim()
Write-Host "Installing certificate to trusted root store..." -ForegroundColor Green
& $certz trust add $PfxFile --password $password --store Root --location CurrentUser
