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

# -- Resolve CM hostname ---------------------------------------------------

function Get-CmHost {
    $projectRoot = (Resolve-Path "$PSScriptRoot/..").Path
    $envFile = Join-Path $projectRoot ".env"
    if (-not (Test-Path $envFile)) {
        $envFile = Join-Path $projectRoot "docker\.env.template"
    }
    $line = Get-Content $envFile | Where-Object { $_ -match "^CM_HOST=" }
    if (-not $line) {
        Write-Host "ERROR: CM_HOST not found in $envFile" -ForegroundColor Red
        exit 1
    }
    return ($line -replace "CM_HOST=", "").Trim()
}

# -- Check: CM responds over HTTPS ----------------------------------------
# This is the real gate. If the endpoint responds we don't care whether
# it is running inside Docker or on local IIS.

function Assert-CmAvailable {
    param([string]$Hostname)

    if (-not $Hostname) { $Hostname = Get-CmHost }

    Write-Host "Checking HTTPS connectivity to $Hostname..." -ForegroundColor Cyan

    try {
        [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
        $response = Invoke-WebRequest -Uri "https://$Hostname/sitecore/login" `
            -UseBasicParsing -TimeoutSec 15 -ErrorAction Stop
        if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 400) {
            Write-Host "  Sitecore CM responded (HTTP $($response.StatusCode))." -ForegroundColor Green
            return
        }
    }
    catch {
        # fall through to diagnostics
    }
    finally {
        [System.Net.ServicePointManager]::ServerCertificateValidationCallback = $null
    }

    # CM is unreachable
    Write-Host "ERROR: Could not reach https://$Hostname/sitecore/login" -ForegroundColor Red
    Write-Host "  Verify that:" -ForegroundColor Yellow
    Write-Host "    - Sitecore is running (Docker: task up / IIS: check app pool)" -ForegroundColor Yellow
    Write-Host "    - $Hostname resolves to your instance (check hosts file)" -ForegroundColor Yellow
    Write-Host "    - The site is healthy and accepting requests" -ForegroundColor Yellow
    exit 1
}

# -- Check: SPE Remoting enabled ------------------------------------------

function Assert-SpeRemoting {
    param([string]$Hostname)

    Write-Host "Checking SPE Remoting endpoint..." -ForegroundColor Cyan

    try {
        [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
        $response = Invoke-WebRequest -Uri "https://$Hostname/-/script/v2/master/keepalive" `
            -UseBasicParsing -TimeoutSec 15 -ErrorAction Stop
        Write-Host "  SPE Remoting is enabled (HTTP $($response.StatusCode))." -ForegroundColor Green
        return
    }
    catch {
        $status = $null
        if ($_.Exception.Response) {
            $status = [int]$_.Exception.Response.StatusCode
        }
        if ($status -eq 403 -or $status -eq 401) {
            Write-Host "  SPE Remoting endpoint responded (HTTP $status) - endpoint exists." -ForegroundColor Green
            return
        }
    }
    finally {
        [System.Net.ServicePointManager]::ServerCertificateValidationCallback = $null
    }

    Write-Host "ERROR: SPE Remoting endpoint not reachable at https://$Hostname/-/script/v2/master/keepalive" -ForegroundColor Red
    Write-Host "  Ensure the SPE module is installed and remoting is enabled in" -ForegroundColor Yellow
    Write-Host "  App_Config\Include\Spe\Spe.config (remoting/enabled = true)." -ForegroundColor Yellow
    exit 1
}

# -- Composite checks -----------------------------------------------------

function Assert-SpePrerequisites {
    $cmHost = Get-CmHost
    Assert-CmAvailable -Hostname $cmHost
    Assert-SpeRemoting -Hostname $cmHost
    Write-Host ""
}
