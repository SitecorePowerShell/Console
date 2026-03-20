# Minimal assert-based test runner for SPE unit tests
# No Pester dependency — just assert functions and a summary

$script:Passed = 0
$script:Failed = 0
$script:Skipped = 0
$script:Errors = @()

function Write-TestResult {
    param([bool]$Pass, [string]$Message)
    if ($Pass) {
        $script:Passed++
        Write-Host "  [PASS] $Message" -ForegroundColor Green
    } else {
        $script:Failed++
        $script:Errors += $Message
        Write-Host "  [FAIL] $Message" -ForegroundColor Red
    }
}

function Assert-Equal {
    param($Actual, $Expected, [string]$Message)
    if ($null -eq $Actual -and $null -eq $Expected) {
        Write-TestResult -Pass $true -Message $Message
        return
    }
    if ($null -eq $Actual -or $null -eq $Expected) {
        Write-TestResult -Pass $false -Message $Message
        Write-Host "         Expected: $Expected" -ForegroundColor Yellow
        Write-Host "         Actual:   $Actual" -ForegroundColor Yellow
        return
    }
    $pass = -not (Compare-Object @($Actual) @($Expected) -SyncWindow 0)
    Write-TestResult -Pass $pass -Message $Message
    if (-not $pass) {
        Write-Host "         Expected: $Expected" -ForegroundColor Yellow
        Write-Host "         Actual:   $Actual" -ForegroundColor Yellow
    }
}

function Assert-NotEqual {
    param($Actual, $Expected, [string]$Message)
    $pass = [bool](Compare-Object @($Actual) @($Expected) -SyncWindow 0)
    Write-TestResult -Pass $pass -Message $Message
    if (-not $pass) {
        Write-Host "         Both values: $Actual" -ForegroundColor Yellow
    }
}

function Assert-True {
    param($Condition, [string]$Message)
    $pass = [bool]$Condition
    Write-TestResult -Pass $pass -Message $Message
    if (-not $pass) {
        Write-Host "         Condition was false" -ForegroundColor Yellow
    }
}

function Assert-Null {
    param($Value, [string]$Message)
    $pass = $null -eq $Value
    Write-TestResult -Pass $pass -Message $Message
    if (-not $pass) {
        Write-Host "         Expected null, got: $Value" -ForegroundColor Yellow
    }
}

function Assert-NotNull {
    param($Value, [string]$Message)
    $pass = $null -ne $Value
    Write-TestResult -Pass $pass -Message $Message
}

function Assert-Throw {
    param([scriptblock]$ScriptBlock, [string]$ExpectedMessage, [string]$Message)
    $threw = $false
    $errorMsg = ""
    try {
        & $ScriptBlock 2>&1 | Out-Null
    } catch {
        $threw = $true
        $errorMsg = $_.Exception.Message
    }
    if (-not $threw) {
        Write-TestResult -Pass $false -Message $Message
        Write-Host "         Expected an exception but none was thrown" -ForegroundColor Yellow
        return
    }
    if ($ExpectedMessage -and $errorMsg -notlike "*$ExpectedMessage*") {
        Write-TestResult -Pass $false -Message $Message
        Write-Host "         Expected message containing: $ExpectedMessage" -ForegroundColor Yellow
        Write-Host "         Actual message: $errorMsg" -ForegroundColor Yellow
        return
    }
    Write-TestResult -Pass $true -Message $Message
}

function Assert-Type {
    param($Object, [string]$TypeName, [string]$Message)
    $actual = $Object.GetType().Name
    $pass = $actual -eq $TypeName
    Write-TestResult -Pass $pass -Message $Message
    if (-not $pass) {
        Write-Host "         Expected type: $TypeName" -ForegroundColor Yellow
        Write-Host "         Actual type:   $actual" -ForegroundColor Yellow
    }
}

function Assert-Like {
    param([string]$Actual, [string]$Pattern, [string]$Message)
    $pass = $Actual -like $Pattern
    Write-TestResult -Pass $pass -Message $Message
    if (-not $pass) {
        Write-Host "         Expected pattern: $Pattern" -ForegroundColor Yellow
        Write-Host "         Actual:           $Actual" -ForegroundColor Yellow
    }
}

function Invoke-TestFile {
    param([string]$Path)
    $name = Split-Path $Path -Leaf
    Write-Host "`n--- $name ---" -ForegroundColor Cyan
    try {
        . $Path
    } catch {
        $script:Failed++
        $script:Errors += "CRASH in $name : $_"
        Write-Host "  [CRASH] $name : $_" -ForegroundColor Red
    }
}

function Skip-Test {
    param([string]$Message, [string]$Reason)
    $script:Skipped++
    Write-Host "  [SKIP] $Message -- $Reason" -ForegroundColor Yellow
}

function Show-TestSummary {
    $total = $script:Passed + $script:Failed + $script:Skipped
    $skipInfo = if ($script:Skipped -gt 0) { "  |  Skipped: $($script:Skipped)" } else { "" }
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "  Total: $total  |  Passed: $($script:Passed)  |  Failed: $($script:Failed)$skipInfo" -ForegroundColor $(if ($script:Failed -gt 0) { 'Red' } else { 'Green' })
    Write-Host "========================================`n" -ForegroundColor Cyan
    if ($script:Errors.Count -gt 0) {
        Write-Host "Failures:" -ForegroundColor Red
        foreach ($e in $script:Errors) { Write-Host "  - $e" -ForegroundColor Red }
        Write-Host ""
    }
    if ($script:Failed -gt 0) { exit 1 }
}
