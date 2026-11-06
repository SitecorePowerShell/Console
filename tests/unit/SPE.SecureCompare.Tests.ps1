# Unit tests for #1452: SecureCompare.FixedTimeSecretEquals
# Ensures SHA256-based comparison prevents length leakage for variable-length secrets.

# ============================================================
# Load compiled assemblies
# ============================================================
Write-Host "`n  [Loading assemblies]" -ForegroundColor White

$abstractionsPath = "$PSScriptRoot\..\..\src\Spe.Abstractions\bin\Debug\Spe.Abstractions.dll"
$spePath = "$PSScriptRoot\..\..\src\Spe\bin\Debug\Spe.dll"

$canLoadAssemblies = (Test-Path $abstractionsPath) -and (Test-Path $spePath)
if (-not $canLoadAssemblies) {
    Skip-Test "All SecureCompare tests" "Build artifacts not found -- run 'task build' first"
    return
}

try {
    Add-Type -Path $abstractionsPath -ErrorAction SilentlyContinue
} catch { }

try {
    Add-Type -Path $spePath -ErrorAction SilentlyContinue
} catch { }

# SecureCompare is internal -- use reflection to access it
$secureCompareType = [Spe.Core.Settings.Authorization.SharedSecretAuthenticationProvider].Assembly.GetType("Spe.Core.Settings.Authorization.SecureCompare")
if (-not $secureCompareType) {
    Skip-Test "All SecureCompare tests" "SecureCompare type not found in assembly"
    return
}

$fixedTimeEquals = $secureCompareType.GetMethod("FixedTimeEquals", [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Static)
$fixedTimeSecretEquals = $secureCompareType.GetMethod("FixedTimeSecretEquals", [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Static)

if (-not $fixedTimeSecretEquals) {
    Skip-Test "All SecureCompare tests" "FixedTimeSecretEquals method not found"
    return
}

# ============================================================
# Test: Equal strings return true
# ============================================================
Write-Host "`n  [SecureCompare - equal strings]" -ForegroundColor White

$r1 = $fixedTimeSecretEquals.Invoke($null, @("my-secret-key", "my-secret-key"))
Assert-True $r1 "FixedTimeSecretEquals returns true for identical strings"

# ============================================================
# Test: Different strings of same length return false
# ============================================================
Write-Host "`n  [SecureCompare - different strings, same length]" -ForegroundColor White

$r2 = $fixedTimeSecretEquals.Invoke($null, @("secret-aaa", "secret-bbb"))
Assert-True (-not $r2) "FixedTimeSecretEquals returns false for different strings of same length"

# ============================================================
# Test: Different strings of different lengths return false
# ============================================================
Write-Host "`n  [SecureCompare - different strings, different lengths]" -ForegroundColor White

$r3 = $fixedTimeSecretEquals.Invoke($null, @("short", "a-much-longer-secret-value"))
Assert-True (-not $r3) "FixedTimeSecretEquals returns false for different-length strings"

# ============================================================
# Test: Null first argument returns false
# ============================================================
Write-Host "`n  [SecureCompare - null first argument]" -ForegroundColor White

$r4 = $fixedTimeSecretEquals.Invoke($null, @($null, "some-secret"))
Assert-True (-not $r4) "FixedTimeSecretEquals returns false when first argument is null"

# ============================================================
# Test: Null second argument returns false
# ============================================================
Write-Host "`n  [SecureCompare - null second argument]" -ForegroundColor White

$r5 = $fixedTimeSecretEquals.Invoke($null, @("some-secret", $null))
Assert-True (-not $r5) "FixedTimeSecretEquals returns false when second argument is null"

# ============================================================
# Test: Both null returns false
# ============================================================
Write-Host "`n  [SecureCompare - both null]" -ForegroundColor White

$r6 = $fixedTimeSecretEquals.Invoke($null, @($null, $null))
Assert-True (-not $r6) "FixedTimeSecretEquals returns false when both arguments are null"

# ============================================================
# Test: Empty strings are equal
# ============================================================
Write-Host "`n  [SecureCompare - empty strings]" -ForegroundColor White

$r7 = $fixedTimeSecretEquals.Invoke($null, @("", ""))
Assert-True $r7 "FixedTimeSecretEquals returns true for two empty strings"

# ============================================================
# Test: Empty vs non-empty returns false
# ============================================================
Write-Host "`n  [SecureCompare - empty vs non-empty]" -ForegroundColor White

$r8 = $fixedTimeSecretEquals.Invoke($null, @("", "non-empty"))
Assert-True (-not $r8) "FixedTimeSecretEquals returns false for empty vs non-empty"

# ============================================================
# Test: Original FixedTimeEquals still works for equal-length strings
# ============================================================
Write-Host "`n  [SecureCompare - FixedTimeEquals backward compat]" -ForegroundColor White

if ($fixedTimeEquals) {
    $r9 = $fixedTimeEquals.Invoke($null, @("same-length!", "same-length!"))
    Assert-True $r9 "FixedTimeEquals still returns true for identical equal-length strings"

    $r10 = $fixedTimeEquals.Invoke($null, @("same-length!", "diff-length!"))
    Assert-True (-not $r10) "FixedTimeEquals still returns false for different equal-length strings"
} else {
    Skip-Test "FixedTimeEquals backward compat" "FixedTimeEquals method not found"
}
