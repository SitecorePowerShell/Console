param(
    [string]$ConnectionUri = "https://spe.dev.local",
    [string]$TestFile
)

$ErrorActionPreference = "Stop"
Import-Module -Name SPE -Force

# Load test framework (reuse from SPE/Tests/)
. "$PSScriptRoot\..\SPE\Tests\TestRunner.ps1"

$protocolHost = $ConnectionUri

if ($TestFile) {
    $path = Resolve-Path -Path (Join-Path $PSScriptRoot $TestFile) -ErrorAction SilentlyContinue
    if (-not $path) { $path = Resolve-Path -Path $TestFile }
    Invoke-TestFile $path
} else {
    Get-ChildItem "$PSScriptRoot\*.Tests.ps1" | ForEach-Object { Invoke-TestFile $_.FullName }
}

Show-TestSummary
