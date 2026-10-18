[CmdletBinding()]
Param (
    [string]$HostName = "dev.local",
    [switch]$Renew,
    [string]$CertDir,
    [int]$Days = 200
)

$ErrorActionPreference = "Stop"

# $PSScriptRoot is not reliably populated inside a param() default when the script
# is invoked with -File, so resolve the default location here instead.
if (-not $CertDir) {
    $scriptRoot = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
    $CertDir = Join-Path $scriptRoot "..\docker\traefik\certs"
}

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
    $devCertChanged = $true
} elseif (-not (Test-Path $PfxFile)) {
    Write-Host "Generating TLS certificate..." -ForegroundColor Green
    & $certz create dev $HostName --san "*.$HostName" --san "localhost" --key-type RSA --pfx-encryption legacy --file $PfxFile --password-file $PasswordFile --cert $CerFile --key $KeyFile --days $Days
    $devCertChanged = $true
} else {
    # Reusing the existing certificate keeps SITECORE_ID_CERTIFICATE stable and
    # avoids re-prompting for trust on every 'task init'. Use -Renew to replace it.
    Write-Host "Reusing existing TLS certificate ($PfxFile)." -ForegroundColor Green
    $devCertChanged = $false
}

# Trust the certificate. Check the Root store rather than keying off whether the
# certificate was just generated: an interrupted run can leave a regenerated
# certificate that was never trusted, and keying off $devCertChanged would then
# skip re-trusting it forever. Checking the store makes this idempotent, and it
# still avoids the Windows confirmation dialog when trust is already in place.
$password = (Get-Content $PasswordFile -Raw).Trim()
$devCert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($PfxFile, $password)
$trusted = $false
foreach ($location in @('CurrentUser', 'LocalMachine')) {
    $store = New-Object System.Security.Cryptography.X509Certificates.X509Store('Root', $location)
    try {
        $store.Open('ReadOnly')
        if ($store.Certificates | Where-Object { $_.Thumbprint -eq $devCert.Thumbprint }) { $trusted = $true }
    } catch {
        # An unreadable store just means we cannot confirm trust from here.
    } finally {
        $store.Close()
    }
    if ($trusted) { break }
}

if ($trusted) {
    Write-Host "Certificate is already trusted ($($devCert.Thumbprint))." -ForegroundColor Green
} else {
    Write-Host "Installing certificate to trusted root store..." -ForegroundColor Green
    Write-Host "  Windows will ask you to confirm this." -ForegroundColor Yellow
    & $certz trust add $PfxFile --password $password --store Root --location CurrentUser
}

##############################################################################
# Shared trust material for the container roles
#
# The Sitecore entrypoints import every .crt found in C:\certs into the
# container's trusted root store on startup. CM self-calls over HTTPS through
# Traefik, so it needs the dev certificate there.
#
# There is deliberately no SQL Server certificate here. The mssql image
# registers a mounted PFX into LocalMachine\My without persisting its private
# key, so SQL cannot complete a TLS handshake and every client connection times
# out. The dev stack lets SQL use its own self-generated certificate instead and
# connects with TrustServerCertificate=true.
##############################################################################

$dockerDir = [System.IO.Path]::GetFullPath((Join-Path $CertDir "..\.."))
$sharedCertDir = Join-Path $dockerDir "shared\certs"

if (-not (Test-Path $sharedCertDir)) {
    New-Item -ItemType Directory -Path $sharedCertDir -Force | Out-Null
}

# certz writes PEM to --cert; copy it in under a .crt name so the import picks it up.
Copy-Item $CerFile -Destination (Join-Path $sharedCertDir "devcert.crt") -Force

Write-Host "Certificates ready." -ForegroundColor Green
