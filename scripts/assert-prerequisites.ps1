<#
.SYNOPSIS
    Shared prerequisite checks for tasks that need Sitecore running.
.DESCRIPTION
    Dot-source this file, then call:
      Assert-CmAvailable            - verifies CM responds over HTTPS (Docker or local IIS)
      Assert-SpePrerequisites       - verifies CM responds and SPE Remoting is reachable
    Both read CM_HOST from the root .env (falls back to docker/.env.template).
    Works with Docker-based and local IIS Sitecore instances alike.
#>

$ErrorActionPreference = "Stop"

# -- Helpers ---------------------------------------------------------------

function Invoke-HttpCheck {
    param(
        [string]$Uri,
        [string]$Method = "GET",
        [int]$TimeoutMs = 15000
    )

    try {
        $ProgressPreference = 'SilentlyContinue'
        $params = @{
            Uri             = $Uri
            Method          = $Method
            TimeoutSec      = [math]::Ceiling($TimeoutMs / 1000)
            UseBasicParsing = $true
            ErrorAction     = 'Stop'
        }

        if ($PSVersionTable.PSVersion.Major -ge 6) {
            # PowerShell 7+ has -SkipCertificateCheck
            $params['SkipCertificateCheck'] = $true
        } else {
            # PowerShell 5.1: use ICertificatePolicy (available on .NET Framework)
            if (-not ([System.Management.Automation.PSTypeName]'TrustAllCertsPolicy').Type) {
                Add-Type @"
public class TrustAllCertsPolicy : System.Net.ICertificatePolicy {
    public bool CheckValidationResult(System.Net.ServicePoint sp,
        System.Security.Cryptography.X509Certificates.X509Certificate cert,
        System.Net.WebRequest req, int problem) { return true; }
}
"@
            }
            [System.Net.ServicePointManager]::CertificatePolicy = [TrustAllCertsPolicy]::new()
        }

        $response = Invoke-WebRequest @params
        return [int]$response.StatusCode
    }
    catch {
        if ($_.Exception.Response) {
            return [int]$_.Exception.Response.StatusCode
        }
        Write-Host $_.Exception.InnerException -ForegroundColor Red
        return $null
    }
}

# -- Resolve .env values ---------------------------------------------------

function Get-EnvValue {
    param([string]$Key)

    $projectRoot = (Resolve-Path "$PSScriptRoot/..").Path
    $envFile = Join-Path $projectRoot ".env"
    if (-not (Test-Path $envFile)) {
        $envFile = Join-Path $projectRoot "docker\.env.template"
    }
    $line = Get-Content $envFile | Where-Object { $_ -match "^$Key=" }
    if (-not $line) { return $null }
    return ($line -replace "^$Key=", "").Trim()
}

# -- Resolve CM hostname ---------------------------------------------------

function Get-CmHost {
    $value = Get-EnvValue "CM_HOST"
    if (-not $value) {
        Write-Host "ERROR: CM_HOST not found in .env" -ForegroundColor Red
        exit 1
    }
    return $value
}

# -- Check: CM responds over HTTPS ----------------------------------------
# This is the real gate. If the endpoint responds we don't care whether
# it is running inside Docker or on local IIS.

function Assert-CmAvailable {
    param([string]$Hostname)

    if (-not $Hostname) { $Hostname = Get-CmHost }

    Write-Host "Checking HTTPS connectivity to $Hostname..." -ForegroundColor Cyan

    $status = Invoke-HttpCheck -Uri "https://$Hostname/sitecore/login"

    if ($status -and $status -ge 200 -and $status -lt 400) {
        Write-Host "  Sitecore CM responded (HTTP $status)." -ForegroundColor Green
        return
    }

    # CM is unreachable
    Write-Host "ERROR: Could not reach https://$Hostname/sitecore/login" -ForegroundColor Red
    Write-Host "  Verify that:" -ForegroundColor Yellow
    Write-Host "    - Sitecore is running (Docker: task up / IIS: check app pool)" -ForegroundColor Yellow
    Write-Host "    - $Hostname resolves to your instance (check hosts file)" -ForegroundColor Yellow
    Write-Host "    - The site is healthy and accepting requests" -ForegroundColor Yellow
    exit 1
}

# -- Check: SPE Remoting enabled and authenticated ------------------------

function Assert-SpeRemoting {
    param([string]$Hostname)

    # Step 1 - verify the endpoint exists (any HTTP response means it's there)
    Write-Host "Checking SPE Remoting endpoint..." -ForegroundColor Cyan

    $status = Invoke-HttpCheck -Uri "https://$Hostname/-/script/script/" -Method "POST"

    if (-not $status) {
        Write-Host "ERROR: SPE Remoting endpoint not reachable at https://$Hostname/-/script/script/" -ForegroundColor Red
        Write-Host "  Ensure the SPE module is installed and remoting is enabled in" -ForegroundColor Yellow
        Write-Host "  App_Config\Include\Spe\Spe.config (remoting/enabled = true)." -ForegroundColor Yellow
        exit 1
    }
    Write-Host "  SPE Remoting endpoint responded (HTTP $status)." -ForegroundColor Green

    # Step 2 - verify authentication using credentials from .env
    Write-Host "Verifying SPE Remoting authentication..." -ForegroundColor Cyan

    $moduleRoot = "$PSScriptRoot\..\modules\SPE"
    Import-Module "$moduleRoot\SPE.psd1" -Force

    $sharedSecret = Get-EnvValue "SPE_SHARED_SECRET"
    if (-not $sharedSecret) {
        Write-Host "ERROR: SPE_SHARED_SECRET not found in .env. Run 'task init' to generate it." -ForegroundColor Red
        exit 1
    }

    $session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri "https://$Hostname"
    try {
        $result = Test-RemoteConnection -Session $session -Quiet
    } catch {
        $result = $false
    }

    if ($result) {
        Write-Host "  Authentication successful." -ForegroundColor Green
    } else {
        Write-Host "ERROR: SPE Remoting authentication failed." -ForegroundColor Red
        Write-Host "  Verify that:" -ForegroundColor Yellow
        Write-Host "    - SPE remoting is enabled (deploy z.Spe.Development.User.config via 'task deploy')" -ForegroundColor Yellow
        Write-Host "    - SPE_SHARED_SECRET in .env matches the sharedSecret in the Sitecore SPE config" -ForegroundColor Yellow
        exit 1
    }
}

# -- Composite checks -----------------------------------------------------

function Assert-SpePrerequisites {
    $cmHost = Get-CmHost
    Assert-CmAvailable -Hostname $cmHost
    Assert-SpeRemoting -Hostname $cmHost
    Write-Host ""
}
