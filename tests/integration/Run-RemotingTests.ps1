param(
    [string]$ConnectionUri = "https://spe.dev.local",
    [string]$TestFile
)

$ErrorActionPreference = "Stop"

# Prerequisites: ensure Sitecore CM is running and SPE Remoting is enabled
. "$PSScriptRoot\..\..\scripts\assert-prerequisites.ps1"
Assert-SpePrerequisites

$moduleRoot = "$PSScriptRoot\..\..\modules\SPE"
Import-Module "$moduleRoot\SPE.psd1" -Force

# Load test framework (reuse from SPE/Tests/)
. "$PSScriptRoot\..\unit\TestRunner.ps1"

$global:protocolHost = $ConnectionUri
$global:sharedSecret = Get-EnvValue "SPE_SHARED_SECRET"

if ($TestFile) {
    $path = Resolve-Path -Path (Join-Path $PSScriptRoot $TestFile) -ErrorAction SilentlyContinue
    if (-not $path) { $path = Resolve-Path -Path $TestFile }
    Invoke-TestFile $path
} else {
    Get-ChildItem "$PSScriptRoot\*.Tests.ps1" -Exclude "Remoting.Maintenance.Tests.ps1" | ForEach-Object { Invoke-TestFile $_.FullName }
}

Show-TestSummary
