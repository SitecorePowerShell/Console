# Unit tests for JtiReplayCache (issue #1485). Validates replay protection
# at the cache level. End-to-end behaviour through Validate() is covered by
# the integration suite.

Write-Host "`n  [JtiReplayCache: behaviour]" -ForegroundColor White

$abstractionsPath = "$PSScriptRoot\..\..\src\Spe.Abstractions\bin\Debug\Spe.Abstractions.dll"
$spePath          = "$PSScriptRoot\..\..\src\Spe\bin\Debug\Spe.dll"

if (-not ((Test-Path $abstractionsPath) -and (Test-Path $spePath))) {
    Skip-Test "All JtiReplay tests" "Build artifacts not found - run 'task build' first"
    return
}

try { Add-Type -Path $abstractionsPath -ErrorAction SilentlyContinue } catch { }
try { Add-Type -Path $spePath          -ErrorAction SilentlyContinue } catch { }

$asm = [Spe.Core.Settings.Authorization.OAuthBearerTokenAuthenticationProvider].Assembly
$cacheType = $asm.GetType("Spe.Core.Settings.Authorization.JtiReplayCache")
if (-not $cacheType) {
    Skip-Test "All JtiReplay tests" "JtiReplayCache type not found in assembly"
    return
}

$bindAll = [System.Reflection.BindingFlags]::Instance -bor
           [System.Reflection.BindingFlags]::Public -bor
           [System.Reflection.BindingFlags]::NonPublic

$ctor     = $cacheType.GetConstructor($bindAll, $null, [Type[]]@([int]), $null)
$tryClaim = $cacheType.GetMethod("TryClaim", $bindAll)
$countProp = $cacheType.GetProperty("Count", $bindAll)

if (-not $ctor -or -not $tryClaim) {
    Skip-Test "All JtiReplay tests" "JtiReplayCache reflection bindings unavailable"
    return
}

$futureExp = [DateTimeOffset]::UtcNow.AddHours(1).ToUnixTimeSeconds()
$pastExp   = [DateTimeOffset]::UtcNow.AddHours(-1).ToUnixTimeSeconds()

# First-use accepted, second-use rejected
$cache = $ctor.Invoke(@([int]100))
$first = $tryClaim.Invoke($cache, @("https://idp", "abc123", [long]$futureExp))
Assert-True $first "First use of (iss, jti) is accepted"
$second = $tryClaim.Invoke($cache, @("https://idp", "abc123", [long]$futureExp))
Assert-True (-not $second) "Second use of same (iss, jti) is rejected as replay"

# Distinct jti values both accepted
$cache = $ctor.Invoke(@([int]100))
$a = $tryClaim.Invoke($cache, @("https://idp", "tokenA", [long]$futureExp))
$b = $tryClaim.Invoke($cache, @("https://idp", "tokenB", [long]$futureExp))
Assert-True ($a -and $b) "Two distinct jti values both accepted"

# Same jti from two issuers both accepted (iss scopes the key)
$cache = $ctor.Invoke(@([int]100))
$a = $tryClaim.Invoke($cache, @("https://idp1", "shared-jti", [long]$futureExp))
$b = $tryClaim.Invoke($cache, @("https://idp2", "shared-jti", [long]$futureExp))
Assert-True ($a -and $b) "Same jti from different issuers both accepted"

# Empty inputs rejected
$cache = $ctor.Invoke(@([int]100))
$emptyIss = $tryClaim.Invoke($cache, @("", "jti", [long]$futureExp))
Assert-True (-not $emptyIss) "Empty iss is rejected"
$emptyJti = $tryClaim.Invoke($cache, @("iss", "", [long]$futureExp))
Assert-True (-not $emptyJti) "Empty jti is rejected"

# Stale entry replaced - if a recorded jti's exp has passed, a re-presentation
# with a fresh exp is accepted (the original token can no longer be used).
$cache = $ctor.Invoke(@([int]100))
$first = $tryClaim.Invoke($cache, @("https://idp", "stale-jti", [long]$pastExp))
Assert-True $first "Initial claim with past exp accepted"
$second = $tryClaim.Invoke($cache, @("https://idp", "stale-jti", [long]$futureExp))
Assert-True $second "Re-claim after stale entry is accepted (entry replaced)"

# Default constructor produces a usable cache
$default = $ctor.Invoke(@([int]0))
$ok = $tryClaim.Invoke($default, @("https://idp", "default-jti", [long]$futureExp))
Assert-True $ok "Cache constructed with maxEntries=0 falls back to default and accepts"

# Provider integration: ReplayCache property is null when disabled.
$providerType = [Spe.Core.Settings.Authorization.OAuthBearerTokenAuthenticationProvider]
$replayProp = $providerType.GetProperty("ReplayCache", $bindAll)
$provider = $providerType::new()
$provider.JtiReplayCacheEnabled = $false
$disabled = $replayProp.GetValue($provider)
Assert-Null $disabled "ReplayCache is null when JtiReplayCacheEnabled=false"
