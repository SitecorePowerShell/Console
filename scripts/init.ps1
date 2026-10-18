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
    $SqlSaPassword = "Password12345",

    # Populate .env only. Certificate creation installs a CA into the trust store,
    # which prompts for confirmation and cannot run unattended.
    [switch]
    $SkipCertificates
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
            # Strip whitespace-preceded inline comments the same way Compose's
            # dotenv parser does, so init.ps1 and Compose agree on the value.
            # Without this a template placeholder reads back as a real secret.
            return ($Matches[1] -replace '\s+#.*$', '').Trim()
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

function Get-RandomHexString {
    param(
        [Parameter(Mandatory, Position = 0)]
        [int]$ByteCount
    )
    # RandomNumberGenerator::Fill is .NET Core only; Create()/GetBytes works on
    # Windows PowerShell 5.1, which is what the Taskfile invokes.
    $bytes = [byte[]]::new($ByteCount)
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    return ([BitConverter]::ToString($bytes) -replace '-','')
}

# Secrets are generated once and preserved across re-runs so an existing
# environment keeps working after another 'task init'.
function Set-EnvFileVariableIfEmpty {
    param(
        [Parameter(Mandatory, Position = 0)]
        [string]$Variable,
        [Parameter(Mandatory)]
        [scriptblock]$Generator
    )
    if ([string]::IsNullOrEmpty((Get-EnvFileVariable $Variable))) {
        Set-EnvFileVariable $Variable -Value (& $Generator)
    }
}

# Matches SitecoreDockerTools' ConvertTo-CompressedBase64String: GZip then Base64.
# Sitecore 10.5 takes the license by value in Sitecore_License, not by volume mount.
function ConvertTo-CompressedBase64String {
    param(
        [Parameter(Mandatory, Position = 0)]
        [string]$Path
    )
    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $memory = [System.IO.MemoryStream]::new()
        try {
            $gzip = [System.IO.Compression.GZipStream]::new($memory, [System.IO.Compression.CompressionLevel]::Optimal, $false)
            $stream.CopyTo($gzip)
            # Close (not just flush) so the gzip footer is written before reading the buffer.
            $gzip.Close()
            return [System.Convert]::ToBase64String($memory.ToArray())
        } finally {
            $memory.Dispose()
        }
    } finally {
        $stream.Dispose()
    }
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

Set-EnvFileVariableIfEmpty "TELERIK_ENCRYPTION_KEY" -Generator { Get-RandomString 128 }
Set-EnvFileVariableIfEmpty "MEDIA_REQUEST_PROTECTION_SHARED_SECRET" -Generator { Get-RandomString 64 }
Set-EnvFileVariableIfEmpty "SITECORE_IDSECRET" -Generator { Get-RandomString 64 }

# Solr 10 requires Basic Auth. Both passwords end up inside URL-form connection
# strings, so keep them alphanumeric to avoid escaping issues.
Set-EnvFileVariableIfEmpty "SOLR_ADMIN_PASSWORD" -Generator { Get-RandomString 32 }
Set-EnvFileVariableIfEmpty "SOLR_CONNECTION_PASSWORD" -Generator { Get-RandomString 32 }

Set-EnvFileVariableIfEmpty "SPE_SHARED_SECRET" -Generator { Get-RandomHexString 32 }

# Consumed by the IDS spe-remoting client that issues bearer tokens for the
# OAuth provider integration tests.
Set-EnvFileVariableIfEmpty "SPE_OAUTH_CLIENT_SECRET" -Generator { Get-RandomHexString 32 }

# SITECORE_LICENSE is refreshed on every run so a renewed license.xml is picked up.
Write-Host "Encoding license from $LicenseXmlPath..." -ForegroundColor Green
Set-EnvFileVariable "SITECORE_LICENSE" -Value (ConvertTo-CompressedBase64String $LicenseXmlPath)

##################################
# Configure TLS/HTTPS certificates
##################################

if ($SkipCertificates) {
    Write-Host "Skipping certificate generation (-SkipCertificates)." -ForegroundColor Yellow
} else {
    & "$PSScriptRoot\cert.ps1" -HostName $HostName
}

# SITECORE_ID_CERTIFICATE -- reuse the Traefik PFX
$certsDir = Join-Path $projectPath "docker\traefik\certs"
$certificatePath = Join-Path $certsDir "devcert.pfx"
if (Test-Path $certificatePath) {
    $certificatePassword = (Get-Content (Join-Path $certsDir "devcert.password.txt") -Raw).Trim()
    Set-EnvFileVariable "SITECORE_ID_CERTIFICATE" -Value ([Convert]::ToBase64String([IO.File]::ReadAllBytes($certificatePath)))
    Set-EnvFileVariable "SITECORE_ID_CERTIFICATE_PASSWORD" -Value $certificatePassword
} else {
    Write-Host "No devcert.pfx yet; SITECORE_ID_CERTIFICATE will be set on the next run without -SkipCertificates." -ForegroundColor Yellow
}

Write-Host "Done!" -ForegroundColor Green