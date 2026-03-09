# Entry point: load module, expose private functions, run all unit tests
$ErrorActionPreference = "Stop"

$moduleRoot = Split-Path $PSScriptRoot -Parent

# Import the module
Import-Module "$moduleRoot\SPE.psd1" -Force

# Create global proxy functions for private module functions used in tests
$speModule = Get-Module SPE
foreach ($fnName in @('Get-UsingVariables', 'Get-UsingVariableValues', 'Resolve-UsingVariables', 'Expand-ScriptSession', 'New-SpeHttpClient')) {
    $fn = & $speModule { param($n) Get-Command $n -ErrorAction SilentlyContinue } $fnName
    if ($fn) {
        Set-Item "function:global:$fnName" $fn.ScriptBlock
    }
}

# Dot-source Invoke-RemoteScript.ps1 for Parse-Response (private function defined there)
. "$moduleRoot\Invoke-RemoteScript.ps1"

# Load test framework
. "$PSScriptRoot\TestRunner.ps1"

# Run all test files
Get-ChildItem "$PSScriptRoot\*.Tests.ps1" | ForEach-Object { Invoke-TestFile $_.FullName }

Show-TestSummary
