[CmdletBinding()]
Param (
    [Parameter()]
    [string]
    [ValidateNotNullOrEmpty()]
    $LicenseXmlPath = "C:\license\license.xml",

    [string]
    $HostName = "dev.local",
    
    # We do not need to use [SecureString] here since the value will be stored unencrypted in .env,
    # and used only for transient local example environment.
    [string]
    $SitecoreAdminPassword = "Password12345",
    
    # We do not need to use [SecureString] here since the value will be stored unencrypted in .env,
    # and used only for transient local example environment.
    [string]
    $SqlSaPassword = "Password12345"
)

$ErrorActionPreference = "Stop";
$projectPath = Resolve-Path "$PSScriptRoot/.."
$envPath = Join-Path -Path $projectPath -ChildPath ".env"

function Get-EnvFileVariable {
    param(
        [Parameter(Mandatory)]
        [string]$Variable,
        [string]$Path = $script:envPath
    )
    if (-not (Test-Path $Path)) { return $null }
    foreach ($line in Get-Content $Path) {
        if ($line -match "^$Variable=(.*)$") {
            return $Matches[1]
        }
    }
    return $null
}

function Set-EnvFileVariable {
    param(
        [Parameter(Mandatory, Position = 0)]
        [string]$Variable,
        [Parameter(Mandatory)]
        [string]$Value,
        [string]$Path = $script:envPath
    )
    $line = "$Variable=$Value"
    if (-not (Test-Path $Path)) {
        Set-Content $Path $line
        return
    }
    $content = Get-Content $Path
    $found = $false
    $content = $content | ForEach-Object {
        if ($_ -match "^$Variable=") {
            $found = $true
            $line
        } else {
            $_
        }
    }
    if (-not $found) {
        $content = @($content) + $line
    }
    Set-Content $Path $content
}

function Get-RandomString {
    param(
        [Parameter(Mandatory, Position = 0)]
        [int]$Length
    )
    $chars = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789'
    $bytes = [byte[]]::new($Length)
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $rng.GetBytes($bytes)
    $rng.Dispose()
    $result = [char[]]::new($Length)
    for ($i = 0; $i -lt $Length; $i++) {
        $result[$i] = $chars[$bytes[$i] % $chars.Length]
    }
    return -join $result
}

if (-not (Test-Path $LicenseXmlPath)) {
    throw "Did not find $LicenseXmlPath"
}
if (-not (Test-Path $LicenseXmlPath -PathType Leaf)) {
    throw "$LicenseXmlPath is not a file"
}
if (-not (Test-Path "$projectPath\.env")) {
    Write-Host "Copying new .env" -ForegroundColor Green
    Copy-Item "$projectPath\docker\.env.template" "$projectPath\.env"
}

###############################
# Populate the environment file
###############################

Write-Host "Populating required .env file variables..." -ForegroundColor Green

# SITECORE_ADMIN_PASSWORD
Set-EnvFileVariable "SITECORE_ADMIN_PASSWORD" -Value $SitecoreAdminPassword

# SQL_SA_PASSWORD
Set-EnvFileVariable "SQL_SA_PASSWORD" -Value $SqlSaPassword

# CM_HOST
$cmHost = Get-EnvFileVariable -Variable "CM_HOST" -Path $envPath
if([string]::IsNullOrEmpty($cmHost)) {
    $cmHost = "cm.$($HostName)"
    Set-EnvFileVariable "CM_HOST" -Value $cmHost
}

# ID_HOST
$idHost = Get-EnvFileVariable -Variable "ID_HOST" -Path $envPath
if([string]::IsNullOrEmpty($idHost)) {
    $idHost = "id.$($HostName)"
    Set-EnvFileVariable "ID_HOST" -Value $idHost
}

# TELERIK_ENCRYPTION_KEY = random 64-128 chars (preserve across re-runs)
if ([string]::IsNullOrEmpty((Get-EnvFileVariable "TELERIK_ENCRYPTION_KEY"))) {
    Set-EnvFileVariable "TELERIK_ENCRYPTION_KEY" -Value (Get-RandomString 128)
}

# MEDIA_REQUEST_PROTECTION_SHARED_SECRET (preserve across re-runs)
if ([string]::IsNullOrEmpty((Get-EnvFileVariable "MEDIA_REQUEST_PROTECTION_SHARED_SECRET"))) {
    Set-EnvFileVariable "MEDIA_REQUEST_PROTECTION_SHARED_SECRET" -Value (Get-RandomString 64)
}

# SITECORE_IDSECRET = random 64 chars (preserve across re-runs)
if ([string]::IsNullOrEmpty((Get-EnvFileVariable "SITECORE_IDSECRET"))) {
    Set-EnvFileVariable "SITECORE_IDSECRET" -Value (Get-RandomString 64)
}

# SPE_SHARED_SECRET = random 64-char hex string (preserve across re-runs)
if ([string]::IsNullOrEmpty((Get-EnvFileVariable "SPE_SHARED_SECRET"))) {
    $bytes = [byte[]]::new(32)
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
    Set-EnvFileVariable "SPE_SHARED_SECRET" -Value ([BitConverter]::ToString($bytes) -replace '-','')
}

# SITECORE_LICENSE_LOCATION and SITECORE_LICENSE_PATH
$licenseLocation = Get-EnvFileVariable -Variable "SITECORE_LICENSE_LOCATION" -Path $envPath
if([string]::IsNullOrEmpty($licenseLocation)) {
    Set-EnvFileVariable "SITECORE_LICENSE_LOCATION" -Value $LicenseXmlPath
    Set-EnvFileVariable "SITECORE_LICENSE_PATH" -Value ([System.IO.Path]::GetDirectoryName($LicenseXmlPath))
}

##################################
# Configure TLS/HTTPS certificates
##################################

& "$PSScriptRoot\cert.ps1" -HostName $HostName

# SITECORE_ID_CERTIFICATE -- reuse the Traefik PFX
$certsDir = Join-Path $projectPath "docker\traefik\certs"
$certificatePath = Join-Path $certsDir "devcert.pfx"
$certificatePassword = (Get-Content (Join-Path $certsDir "devcert.password.txt") -Raw).Trim()
Set-EnvFileVariable "SITECORE_ID_CERTIFICATE" -Value ([Convert]::ToBase64String([IO.File]::ReadAllBytes($certificatePath)))
Set-EnvFileVariable "SITECORE_ID_CERTIFICATE_PASSWORD" -Value $certificatePassword

Write-Host "Done!" -ForegroundColor Green