# Unit tests for the WWW-Authenticate header value builder (issue #1485
# follow-up: config hardening, item 5). RFC 6750 Section 3 mandates that
# bearer-protected endpoints emit `WWW-Authenticate: Bearer ...` on 401.
# Today we emit only the SPE-specific X-SPE-AuthFailureReason header, which
# standard libraries do not parse.
#
# The helper builds the *header value* string (everything after
# "WWW-Authenticate: "). Callers concatenate the scheme themselves.
#
# Mapping:
#   invalid  -> Bearer error="invalid_token"
#   expired  -> Bearer error="invalid_token", error_description="..."
#   replay   -> Bearer error="invalid_token", error_description="..."
#   disabled -> Bearer error="invalid_token"  (collapsed to resist enumeration)
#   missing_scope -> Bearer error="insufficient_scope"
#   null / unknown -> Bearer error="invalid_token"

Write-Host "`n  [JwtClaimValidator: BuildWwwAuthenticate]" -ForegroundColor White

$abstractionsPath = "$PSScriptRoot\..\..\src\Spe.Abstractions\bin\Debug\Spe.Abstractions.dll"
$spePath          = "$PSScriptRoot\..\..\src\Spe\bin\Debug\Spe.dll"

if (-not ((Test-Path $abstractionsPath) -and (Test-Path $spePath))) {
    Skip-Test "All AuthHeader tests" "Build artifacts not found - run 'task build' first"
    return
}

try { Add-Type -Path $abstractionsPath -ErrorAction SilentlyContinue } catch { }
try { Add-Type -Path $spePath          -ErrorAction SilentlyContinue } catch { }

$asm = [Spe.Core.Settings.Authorization.OAuthBearerTokenAuthenticationProvider].Assembly
$validatorType = $asm.GetType("Spe.Core.Settings.Authorization.JwtClaimValidator")
$method = $validatorType.GetMethod("BuildWwwAuthenticate",
    [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Static)

if (-not $method) {
    Skip-Test "All AuthHeader tests" "BuildWwwAuthenticate not found on JwtClaimValidator"
    return
}

function Build {
    param([string]$Reason)
    return [string]$method.Invoke($null, [object[]]@($Reason))
}

# ============================================================
# Every valid response starts with `Bearer ` (capital B per RFC 6750)
# ============================================================
Write-Host "`n  [Header always starts with `"Bearer `"]" -ForegroundColor White
$h = Build "invalid"
Assert-True ($h.StartsWith("Bearer ")) "invalid -> starts with 'Bearer '"

$h = Build $null
Assert-True ($h.StartsWith("Bearer ")) "null reason -> still starts with 'Bearer '"

# ============================================================
# RFC 6750 error codes
# ============================================================
Write-Host "`n  [invalid -> error=`"invalid_token`"]" -ForegroundColor White
$h = Build "invalid"
Assert-Like $h '*error="invalid_token"*' "invalid maps to invalid_token"
Assert-True ($h -notmatch 'error_description') "invalid carries no description (avoids enumeration)"

Write-Host "`n  [expired -> error=`"invalid_token`" with description]" -ForegroundColor White
$h = Build "expired"
Assert-Like $h '*error="invalid_token"*' "expired maps to invalid_token"
Assert-Like $h '*error_description=*' "expired carries an error_description"
Assert-Like $h '*expired*' "expired description mentions expiration"

Write-Host "`n  [replay -> error=`"invalid_token`" with description]" -ForegroundColor White
$h = Build "replay"
Assert-Like $h '*error="invalid_token"*' "replay maps to invalid_token"
Assert-Like $h '*error_description=*' "replay carries an error_description"

Write-Host "`n  [disabled -> error=`"invalid_token`" only (no description)]" -ForegroundColor White
$h = Build "disabled"
Assert-Like $h '*error="invalid_token"*' "disabled collapses to invalid_token"
Assert-True ($h -notmatch 'disabled') "disabled does NOT leak the reason in description (resist enumeration)"

Write-Host "`n  [missing_scope -> error=`"insufficient_scope`"]" -ForegroundColor White
$h = Build "missing_scope"
Assert-Like $h '*error="insufficient_scope"*' "missing_scope maps to RFC 6750 insufficient_scope"

Write-Host "`n  [null reason -> error=`"invalid_token`"]" -ForegroundColor White
$h = Build $null
Assert-Like $h '*error="invalid_token"*' "Null reason defaults to invalid_token (safe fallback)"

Write-Host "`n  [unknown reason -> error=`"invalid_token`"]" -ForegroundColor White
$h = Build "some-future-bucket-we-do-not-recognize"
Assert-Like $h '*error="invalid_token"*' "Unknown reason defaults to invalid_token (safe fallback)"

# ============================================================
# Quoting / format correctness (RFC 6750 challenge syntax)
# ============================================================
Write-Host "`n  [Format: error parameter is quoted]" -ForegroundColor White
$h = Build "invalid"
Assert-True ($h -match 'error="[^"]+"') "error value is double-quoted per RFC 6750"

Write-Host "`n  [Format: error_description is quoted when present]" -ForegroundColor White
$h = Build "expired"
Assert-True ($h -match 'error_description="[^"]+"') "error_description value is double-quoted when emitted"
