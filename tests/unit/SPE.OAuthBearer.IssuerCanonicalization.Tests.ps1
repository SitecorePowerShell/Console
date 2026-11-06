# Unit tests for JwtClaimValidator.CanonicalizeIssuer (issue #1485 follow-up:
# config hardening, item 6). Pure-function URI normalization used by
# IsValidIssuer to compare allowed entries against incoming iss claims:
#
#   - Host portion is lowercased (RFC 3986 - host is case-insensitive).
#   - Path portion case is preserved (RFC 3986 - path IS case-sensitive).
#   - Trailing slash on the path portion is stripped.
#   - Non-URI / opaque issuers pass through unchanged so existing
#     SharedSecret-style values like "SPE Remoting" still work.
#
# Eliminates the silent-rejection class where an operator pastes
# "https://tenant.us.auth0.com" into <allowedIssuers> but the IdP emits
# "https://tenant.us.auth0.com/" (or vice versa).

Write-Host "`n  [JwtClaimValidator: CanonicalizeIssuer]" -ForegroundColor White

$abstractionsPath = "$PSScriptRoot\..\..\src\Spe.Abstractions\bin\Debug\Spe.Abstractions.dll"
$spePath          = "$PSScriptRoot\..\..\src\Spe\bin\Debug\Spe.dll"

if (-not ((Test-Path $abstractionsPath) -and (Test-Path $spePath))) {
    Skip-Test "All IssuerCanonicalization tests" "Build artifacts not found - run 'task build' first"
    return
}

try { Add-Type -Path $abstractionsPath -ErrorAction SilentlyContinue } catch { }
try { Add-Type -Path $spePath          -ErrorAction SilentlyContinue } catch { }

$asm = [Spe.Core.Settings.Authorization.OAuthBearerTokenAuthenticationProvider].Assembly
$validatorType = $asm.GetType("Spe.Core.Settings.Authorization.JwtClaimValidator")
$method = $validatorType.GetMethod("CanonicalizeIssuer",
    [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Static)

if (-not $method) {
    Skip-Test "All IssuerCanonicalization tests" "CanonicalizeIssuer not found on JwtClaimValidator"
    return
}

function Canon {
    param($Issuer)
    $invokeArgs = [object[]]::new(1)
    $invokeArgs[0] = $Issuer
    return $method.Invoke($null, $invokeArgs)
}

# ============================================================
# Trailing slash normalization
# ============================================================
Write-Host "`n  [Trailing slash stripped from canonical form]" -ForegroundColor White
Assert-Equal (Canon "https://tenant.us.auth0.com/") (Canon "https://tenant.us.auth0.com") `
    "With and without trailing slash canonicalize identically"

Assert-Equal (Canon "https://kc/realms/r/") (Canon "https://kc/realms/r") `
    "Trailing slash on multi-segment path is normalized"

# ============================================================
# Host case-insensitivity (RFC 3986 host)
# ============================================================
Write-Host "`n  [Host is lowercased]" -ForegroundColor White
Assert-Equal (Canon "https://SpeID.Dev.Local") (Canon "https://speid.dev.local") `
    "Mixed-case host canonicalizes to lowercase"

Assert-Equal (Canon "https://EXAMPLE.com/path") (Canon "https://example.com/path") `
    "Uppercase host canonicalizes to lowercase, path unchanged"

# ============================================================
# Path case PRESERVED (RFC 3986 path is case-sensitive)
# Use -cne (case-sensitive not-equal) directly because Assert-NotEqual
# delegates to Compare-Object which is case-insensitive on Windows PS.
# ============================================================
Write-Host "`n  [Path case preserved]" -ForegroundColor White
$canonUpper = Canon "https://idp/Realms/Master"
$canonLower = Canon "https://idp/realms/master"
Assert-True ($canonUpper -cne $canonLower) `
    "Path case differences survive canonicalization (case-sensitive comparison)"

# ============================================================
# Combined normalization
# ============================================================
Write-Host "`n  [Host case + trailing slash combined]" -ForegroundColor White
Assert-Equal (Canon "https://Tenant.US.Auth0.com/") (Canon "https://tenant.us.auth0.com") `
    "Mixed-case host with trailing slash matches lowercase no-slash"

# ============================================================
# Opaque (non-URI) issuer support preserved
# Opaque values lowercase so OrdinalIgnoreCase can be replaced by Ordinal
# in the caller without losing the existing case-insensitive semantics.
# ============================================================
Write-Host "`n  [Opaque issuer canonicalizes to lowercase]" -ForegroundColor White
Assert-Equal (Canon "SPE Remoting") "spe remoting" `
    "Non-URI opaque issuer lowercased so caller can compare Ordinal"

Assert-Equal (Canon "Web API") "web api" `
    "Multi-word opaque issuer lowercased"

Assert-Equal (Canon "spe remoting") (Canon "SPE Remoting") `
    "Different cases of an opaque issuer canonicalize to the same value"

# ============================================================
# Edge cases: null / empty / whitespace
# ============================================================
Write-Host "`n  [Null / empty inputs]" -ForegroundColor White
Assert-Null (Canon $null) "Null input canonicalizes to null"
Assert-Equal (Canon "") "" "Empty input returns empty"

# ============================================================
# Scheme preserved
# ============================================================
Write-Host "`n  [Scheme preserved]" -ForegroundColor White
Assert-NotEqual (Canon "http://idp/") (Canon "https://idp/") `
    "http and https canonicalize differently (scheme is significant)"

# ============================================================
# Port preserved
# ============================================================
Write-Host "`n  [Port preserved]" -ForegroundColor White
Assert-NotEqual (Canon "https://idp:8443/") (Canon "https://idp/") `
    "Non-default port survives canonicalization"

Assert-Equal (Canon "https://idp:443/") (Canon "https://idp/") `
    "Default https port (443) is normalized away"
