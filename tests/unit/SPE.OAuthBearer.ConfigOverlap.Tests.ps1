# Unit tests for AuthProviderConfigValidator (issue #1485 follow-up: config
# hardening, item 7). Pure-function check that scans the registered auth
# providers at startup and emits Warn-level findings for misconfigurations
# that route deterministically but probably aren't what the operator wanted.
#
# Three findings:
#   - Two OAuthBearer providers sharing an (issuer, alg) tuple.
#   - An OAuthBearer issuer also declared on a SharedSecret AllowedIssuers.
#   - An OAuthBearer provider with RequiredScopes but empty AllowedAudiences.
#
# The validator never refuses to start; it returns a list of strings that
# the manager logs at Warn level. A clean configuration returns an empty list.

Write-Host "`n  [AuthProviderConfigValidator]" -ForegroundColor White

$abstractionsPath = "$PSScriptRoot\..\..\src\Spe.Abstractions\bin\Debug\Spe.Abstractions.dll"
$spePath          = "$PSScriptRoot\..\..\src\Spe\bin\Debug\Spe.dll"

if (-not ((Test-Path $abstractionsPath) -and (Test-Path $spePath))) {
    Skip-Test "All ConfigOverlap tests" "Build artifacts not found - run 'task build' first"
    return
}

try { Add-Type -Path $abstractionsPath -ErrorAction SilentlyContinue } catch { }
try { Add-Type -Path $spePath          -ErrorAction SilentlyContinue } catch { }

$asm = [Spe.Core.Settings.Authorization.OAuthBearerTokenAuthenticationProvider].Assembly
$validatorType = $asm.GetType("Spe.Core.Settings.Authorization.AuthProviderConfigValidator")

if (-not $validatorType) {
    Skip-Test "All ConfigOverlap tests" "AuthProviderConfigValidator type not found"
    return
}

$method = $validatorType.GetMethod("FindWarnings",
    [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Static)

if (-not $method) {
    Skip-Test "All ConfigOverlap tests" "FindWarnings static method not found"
    return
}

# ------------------------------------------------------------
# Provider builders
# ------------------------------------------------------------
function New-OAuth {
    param(
        [string[]]$Issuers,
        [string[]]$Algorithms = @("RS256"),
        [string[]]$Audiences = @("spe-remoting"),
        [string[]]$Scopes = @()
    )
    $p = New-Object Spe.Core.Settings.Authorization.OAuthBearerTokenAuthenticationProvider
    $p.AllowedIssuers   = [System.Collections.Generic.List[string]]::new()
    $p.AllowedAlgorithms = [System.Collections.Generic.List[string]]::new()
    $p.AllowedAudiences = [System.Collections.Generic.List[string]]::new()
    $p.RequiredScopes   = [System.Collections.Generic.List[string]]::new()
    foreach ($i in $Issuers)    { [void]$p.AllowedIssuers.Add($i) }
    foreach ($a in $Algorithms) { [void]$p.AllowedAlgorithms.Add($a) }
    foreach ($a in $Audiences)  { [void]$p.AllowedAudiences.Add($a) }
    foreach ($s in $Scopes)     { [void]$p.RequiredScopes.Add($s) }
    return $p
}

function New-Shared {
    param([string[]]$Issuers = @("SPE Remoting"))
    $p = New-Object Spe.Core.Settings.Authorization.SharedSecretAuthenticationProvider
    $p.AllowedIssuers = [System.Collections.Generic.List[string]]::new()
    foreach ($i in $Issuers) { [void]$p.AllowedIssuers.Add($i) }
    return $p
}

function Find-Warnings {
    param([object[]]$Providers)
    $list = [System.Collections.Generic.List[Spe.Abstractions.VersionDecoupling.Interfaces.ISpeAuthenticationProvider]]::new()
    foreach ($p in $Providers) { [void]$list.Add($p) }
    # Single-element [object[]] - using @() would enumerate the generic list
    # itself and trip "Parameter count mismatch" against the 1-arg method.
    $invokeArgs = [object[]]::new(1)
    $invokeArgs[0] = $list
    $result = $method.Invoke($null, $invokeArgs)
    # Materialise to a PowerShell array so .Count works even if zero items.
    return @($result)
}

# ============================================================
# Clean configurations produce no warnings
# ============================================================
Write-Host "`n  [Empty provider list]" -ForegroundColor White
$w = Find-Warnings @()
Assert-Equal $w.Count 0 "Empty provider list produces no warnings"

Write-Host "`n  [Single OAuth provider, scopes + audiences set]" -ForegroundColor White
$w = Find-Warnings @((New-OAuth -Issuers @("https://idp1") -Scopes @("spe.remoting") -Audiences @("api1")))
Assert-Equal $w.Count 0 "Single well-formed OAuth provider produces no warnings"

Write-Host "`n  [Two OAuth providers with disjoint issuers]" -ForegroundColor White
$w = Find-Warnings @(
    (New-OAuth -Issuers @("https://idp1")),
    (New-OAuth -Issuers @("https://idp2"))
)
Assert-Equal $w.Count 0 "Disjoint OAuth providers produce no warnings"

Write-Host "`n  [SharedSecret + OAuth with disjoint issuers]" -ForegroundColor White
$w = Find-Warnings @(
    (New-Shared -Issuers @("SPE Remoting")),
    (New-OAuth -Issuers @("https://idp1"))
)
Assert-Equal $w.Count 0 "SharedSecret and OAuth with different issuers produce no warnings"

# ============================================================
# Two OAuth providers sharing (issuer, alg) -> warning
# ============================================================
Write-Host "`n  [Two OAuth providers share (issuer, alg)]" -ForegroundColor White
$w = Find-Warnings @(
    (New-OAuth -Issuers @("https://idp1") -Algorithms @("RS256")),
    (New-OAuth -Issuers @("https://idp1") -Algorithms @("RS256"))
)
Assert-True ($w.Count -ge 1) "At least one warning emitted for OAuth (issuer, alg) overlap"
Assert-True (($w -join " ") -like "*https://idp1*") "Warning text mentions the overlapping issuer"

Write-Host "`n  [Two OAuth providers share issuer but different alg]" -ForegroundColor White
$w = Find-Warnings @(
    (New-OAuth -Issuers @("https://idp1") -Algorithms @("RS256")),
    (New-OAuth -Issuers @("https://idp1") -Algorithms @("ES256"))
)
Assert-Equal $w.Count 0 "Same issuer with disjoint algorithms is intentional and not warned"

# ============================================================
# OAuth issuer overlapping with SharedSecret AllowedIssuers -> warning
# ============================================================
Write-Host "`n  [SharedSecret + OAuth share issuer]" -ForegroundColor White
$w = Find-Warnings @(
    (New-Shared -Issuers @("https://idp1", "Web API")),
    (New-OAuth  -Issuers @("https://idp1"))
)
Assert-True ($w.Count -ge 1) "Cross-provider issuer overlap emits a warning"
Assert-True (($w -join " ") -like "*https://idp1*") "Warning text mentions the overlapping issuer"

# ============================================================
# OAuth with RequiredScopes but no AllowedAudiences -> warning
# ============================================================
Write-Host "`n  [OAuth with scopes but no audiences]" -ForegroundColor White
$w = Find-Warnings @(
    (New-OAuth -Issuers @("https://idp1") -Audiences @() -Scopes @("spe.remoting"))
)
Assert-True ($w.Count -ge 1) "Scopes without audiences emits a warning"
Assert-True (($w -join " ") -like "*audience*" -or ($w -join " ") -like "*scope*") `
    "Warning text mentions either audience or scope"

# ============================================================
# Multiple findings produce multiple warnings
# ============================================================
Write-Host "`n  [Multiple distinct findings]" -ForegroundColor White
$w = Find-Warnings @(
    (New-OAuth -Issuers @("https://idp1") -Algorithms @("RS256")),
    (New-OAuth -Issuers @("https://idp1") -Algorithms @("RS256")),
    (New-Shared -Issuers @("https://idp2")),
    (New-OAuth -Issuers @("https://idp2") -Audiences @() -Scopes @("api"))
)
Assert-True ($w.Count -ge 3) "Three independent findings produce three (or more) warnings"
