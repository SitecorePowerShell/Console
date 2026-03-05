$ErrorActionPreference = "Stop"
Import-Module -Name SPE -Force

# Load test framework (reuse from SPE/Tests/)
. "$PSScriptRoot\..\SPE\Tests\TestRunner.ps1"

# Default protocol host for remoting tests
$protocolHost = if ($args[0]) { $args[0] } else { "https://spe.dev.local" }

# Run all remoting test files
Get-ChildItem "$PSScriptRoot\*.Tests.ps1" | ForEach-Object { Invoke-TestFile $_.FullName }

Show-TestSummary
