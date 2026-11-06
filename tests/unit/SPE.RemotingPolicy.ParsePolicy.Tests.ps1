# Unit tests for RemotingPolicyManager.NormalizeAllowedCommands.
#
# Pure-function helper that converts (FullLanguage, AllowedCommands text)
# into the (RestrictCommands, AllowedCommands set) pair stored on a
# RemotingPolicy. Extracted from ParsePolicy so the FullLanguage matrix
# can be exercised without mocking a Sitecore Item.
#
# Behavior contract (post-fix):
#   FullLanguage=true  -> RestrictCommands=false ALWAYS (allowlist ignored)
#                         Reason: type expressions and .NET method calls
#                         bypass CommandAst-based validation, so a populated
#                         allowlist under FL is security theater. Operators
#                         who want command filtering must use ConstrainedLanguage.
#   FullLanguage=false -> RestrictCommands=true ALWAYS
#                         Empty allowlist denies all inline commands except
#                         the StreamBaseline (Write-* / Out-*).

Write-Host "`n  [RemotingPolicyManager: NormalizeAllowedCommands]" -ForegroundColor White

$abstractionsPath = "$PSScriptRoot\..\..\src\Spe.Abstractions\bin\Debug\Spe.Abstractions.dll"
$spePath          = "$PSScriptRoot\..\..\src\Spe\bin\Debug\Spe.dll"

if (-not ((Test-Path $abstractionsPath) -and (Test-Path $spePath))) {
    Skip-Test "All NormalizeAllowedCommands tests" "Build artifacts not found - run 'task build' first"
    return
}

try { Add-Type -Path $abstractionsPath -ErrorAction SilentlyContinue } catch { }
try { Add-Type -Path $spePath          -ErrorAction SilentlyContinue } catch { }

$asm = [Spe.Core.Settings.Authorization.RemotingPolicy].Assembly
$managerType = $asm.GetType("Spe.Core.Settings.Authorization.RemotingPolicyManager")
$method = $managerType.GetMethod("NormalizeAllowedCommands",
    [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Static)

if (-not $method) {
    Skip-Test "All NormalizeAllowedCommands tests" "NormalizeAllowedCommands not found - helper not yet extracted"
    return
}

# Helper signature:
#   public static NormalizationResult NormalizeAllowedCommands(bool fullLanguage, string allowedCommandsText)
# where NormalizationResult exposes:
#   bool RestrictCommands
#   string[] AllowedCommands
function Normalize {
    param([bool]$FullLanguage, [string]$AllowedCommandsText)
    $invokeArgs = [object[]]::new(2)
    $invokeArgs[0] = $FullLanguage
    $invokeArgs[1] = $AllowedCommandsText
    return $method.Invoke($null, $invokeArgs)
}

# ============================================================
# FullLanguage=true matrix - RestrictCommands ALWAYS false
# ============================================================
Write-Host "`n  [FullLanguage=true: allowlist ignored regardless of contents]" -ForegroundColor White

$r = Normalize -FullLanguage $true -AllowedCommandsText $null
Assert-True (-not $r.RestrictCommands) "FL=true, null allowlist text -> RestrictCommands=false"
Assert-Equal $r.AllowedCommands.Count 0 "FL=true, null allowlist text -> empty AllowedCommands"

$r = Normalize -FullLanguage $true -AllowedCommandsText ""
Assert-True (-not $r.RestrictCommands) "FL=true, empty allowlist text -> RestrictCommands=false"
Assert-Equal $r.AllowedCommands.Count 0 "FL=true, empty allowlist text -> empty AllowedCommands"

# This is the regression case the fix targets. Pre-fix code flips
# RestrictCommands to true here, but the restriction is bypassable via
# [System.IO.File]::WriteAllText etc. so it's security theater.
$r = Normalize -FullLanguage $true -AllowedCommandsText "Get-Item`r`nGet-ChildItem"
Assert-True (-not $r.RestrictCommands) "FL=true, populated allowlist -> RestrictCommands=false (REGRESSION GUARD: pre-fix flipped to true)"
Assert-Equal $r.AllowedCommands.Count 0 "FL=true, populated allowlist -> AllowedCommands ignored (REGRESSION GUARD)"

# ============================================================
# FullLanguage=false matrix - RestrictCommands ALWAYS true
# ============================================================
Write-Host "`n  [FullLanguage=false: command allowlist enforced]" -ForegroundColor White

$r = Normalize -FullLanguage $false -AllowedCommandsText $null
Assert-True $r.RestrictCommands "FL=false, null allowlist text -> RestrictCommands=true"
Assert-Equal $r.AllowedCommands.Count 0 "FL=false, null allowlist text -> empty AllowedCommands (deny all inline)"

$r = Normalize -FullLanguage $false -AllowedCommandsText ""
Assert-True $r.RestrictCommands "FL=false, empty allowlist text -> RestrictCommands=true"
Assert-Equal $r.AllowedCommands.Count 0 "FL=false, empty allowlist text -> empty AllowedCommands"

$r = Normalize -FullLanguage $false -AllowedCommandsText "Get-Item`r`nGet-ChildItem"
Assert-True $r.RestrictCommands "FL=false, populated allowlist -> RestrictCommands=true"
Assert-Equal $r.AllowedCommands.Count 2 "FL=false, populated allowlist -> AllowedCommands count=2"
Assert-True ($r.AllowedCommands -contains "Get-Item") "FL=false, allowlist contains Get-Item"
Assert-True ($r.AllowedCommands -contains "Get-ChildItem") "FL=false, allowlist contains Get-ChildItem"

# Whitespace and blank-line tolerance
$r = Normalize -FullLanguage $false -AllowedCommandsText "  Get-Item  `r`n`r`n  Get-ChildItem`r`n"
Assert-Equal $r.AllowedCommands.Count 2 "FL=false: surrounding whitespace and blank lines stripped"
Assert-True ($r.AllowedCommands -contains "Get-Item") "FL=false: trimmed Get-Item present"

# Both LF and CRLF accepted
$r = Normalize -FullLanguage $false -AllowedCommandsText "Get-Item`nGet-ChildItem"
Assert-Equal $r.AllowedCommands.Count 2 "FL=false: LF-only line endings accepted"

# IsCommandAllowed semantics are intentionally not covered here - constructing a
# RemotingPolicy requires HashSet<Sitecore.Data.ID>, which forces Sitecore.Kernel
# load that the unit test harness can't satisfy. The end-to-end semantics are
# exercised in the integration tier (Remoting.RemotingPolicies.Tests.ps1) where
# a real container provides the Sitecore types.
