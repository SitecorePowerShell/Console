[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [hashtable]$WatchDirectoryParameters
)

# Setup
$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"
$timeFormat = "HH:mm:ss:fff"

# Print start message
Write-Host "$(Get-Date -Format $timeFormat): Development ENTRYPOINT: starting..."

# Check to see if we should start the Watch-Directory.ps1 script
$watchDirectoryJobName = "Watch-Directory.ps1"
$useWatchDirectory = $null -ne $WatchDirectoryParameters -bor (Test-Path -Path "C:\deploy" -PathType "Container") -eq $true

if ($useWatchDirectory)
{
    # Setup default parameters if none is supplied
    if ($null -eq $WatchDirectoryParameters)
    {
        $WatchDirectoryParameters = @{ Path = "C:\deploy"; Destination = "C:\inetpub\wwwroot"; }
    }

    Write-Host "$(Get-Date -Format $timeFormat): Development ENTRYPOINT: '$watchDirectoryJobName' validating..."

    # First a trial-run to catch any parameter validation / setup errors
    $WatchDirectoryParameters["WhatIf"] = $true
    & "C:\tools\scripts\Watch-Directory.ps1" @WatchDirectoryParameters
    $WatchDirectoryParameters["WhatIf"] = $false
    
    Write-Host "$(Get-Date -Format $timeFormat): Development ENTRYPOINT: '$watchDirectoryJobName' starting..."

    # Start Watch-Directory.ps1 in background
    Start-Job -Name $watchDirectoryJobName -ArgumentList $WatchDirectoryParameters -ScriptBlock {
        param([hashtable]$params)

        & "C:\tools\scripts\Watch-Directory.ps1" @params

    } | Out-Null

    Write-Host "$(Get-Date -Format $timeFormat): Development ENTRYPOINT: '$watchDirectoryJobName' started."
}
else
{
    Write-Host "$(Get-Date -Format $timeFormat): Development ENTRYPOINT: Skipping start of '$watchDirectoryJobName'. To enable you should mount a directory into 'C:\deploy'."
}

# Apply any patch folders configured in SITECORE_DEVELOPMENT_PATCHES
Write-Host "$(Get-Date -Format $timeFormat): Applying SITECORE_DEVELOPMENT_PATCHES..."
Push-Location $PSScriptRoot\..\..\
try {
    . .\scripts\Get-PatchFolders.ps1
    Get-PatchFolders -Path dev-patches | ForEach-Object {
        Write-Host "$(Get-Date -Format $timeFormat): Applying development patches from $($_.Name)"
        & .\scripts\Invoke-XdtTransform.ps1 -XdtPath $_.FullName -Path $WatchDirectoryParameters.Destination
        & .\scripts\Install-ConfigurationFolder.ps1 -PatchPath $_.FullName -Path $WatchDirectoryParameters.Destination
    }
} finally {
    Pop-Location
}

# Print ready message
Write-Host "$(Get-Date -Format $timeFormat): Development ENTRYPOINT: ready!"

& "C:\LogMonitor\LogMonitor.exe" "powershell" "C:\Run-W3SVCService.ps1"