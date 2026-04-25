# Unit tests for JwksKeyResolver.IsJwksUriAcceptable (issue #1485).
# Pure URI-scheme gate: HTTPS always allowed, http only on loopback.
# Size-cap and negative-cache hardening are validated operationally.

Write-Host "`n  [JwksKeyResolver: IsJwksUriAcceptable]" -ForegroundColor White

$abstractionsPath = "$PSScriptRoot\..\..\src\Spe.Abstractions\bin\Debug\Spe.Abstractions.dll"
$spePath          = "$PSScriptRoot\..\..\src\Spe\bin\Debug\Spe.dll"

if (-not ((Test-Path $abstractionsPath) -and (Test-Path $spePath))) {
    Skip-Test "All JwksHardening tests" "Build artifacts not found - run 'task build' first"
    return
}

try { Add-Type -Path $abstractionsPath -ErrorAction SilentlyContinue } catch { }
try { Add-Type -Path $spePath          -ErrorAction SilentlyContinue } catch { }

$asm = [Spe.Core.Settings.Authorization.OAuthBearerTokenAuthenticationProvider].Assembly
$resolverType = $asm.GetType("Spe.Core.Settings.Authorization.JwksKeyResolver")
$method = $resolverType.GetMethod("IsJwksUriAcceptable",
    [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Static)

if (-not $method) {
    Skip-Test "All JwksHardening tests" "IsJwksUriAcceptable not found on JwksKeyResolver"
    return
}

function Check-Uri {
    param([string]$Uri, [bool]$AllowLoopbackHttp = $false)
    $invokeArgs = [object[]]::new(3)
    $invokeArgs[0] = $Uri
    $invokeArgs[1] = $AllowLoopbackHttp
    $invokeArgs[2] = $false
    $ok = $method.Invoke($null, $invokeArgs)
    return [pscustomobject]@{ Acceptable = $ok; Loopback = $invokeArgs[2] }
}

# HTTPS always accepted, never loopback-flagged
$r = Check-Uri "https://idp.example.com/.well-known/openid-configuration/jwks"
Assert-True $r.Acceptable "HTTPS public host accepted"
Assert-True (-not $r.Loopback) "HTTPS public host not flagged loopback"

$r = Check-Uri "https://localhost/jwks"
Assert-True $r.Acceptable "HTTPS localhost accepted"
Assert-True (-not $r.Loopback) "HTTPS localhost not flagged loopback (loopback flag is for http only)"

# http to non-loopback rejected regardless of flag
$r = Check-Uri "http://idp.example.com/jwks"
Assert-True (-not $r.Acceptable) "http public host rejected (default)"

$r = Check-Uri "http://idp.example.com/jwks" -AllowLoopbackHttp $true
Assert-True (-not $r.Acceptable) "http public host rejected even when loopback http allowed"

# Default (allowLoopbackHttp=false): http loopback rejected
$r = Check-Uri "http://localhost/jwks"
Assert-True (-not $r.Acceptable) "Default: http://localhost rejected"

$r = Check-Uri "http://127.0.0.1/jwks"
Assert-True (-not $r.Acceptable) "Default: http://127.0.0.1 rejected"

$r = Check-Uri "http://[::1]/jwks"
Assert-True (-not $r.Acceptable) "Default: http://[::1] rejected"

# allowLoopbackHttp=true: loopback accepted with flag set
$r = Check-Uri "http://localhost/jwks" -AllowLoopbackHttp $true
Assert-True $r.Acceptable "Loopback opt-in: http://localhost accepted"
Assert-True $r.Loopback "Loopback opt-in: http://localhost flagged"

$r = Check-Uri "http://127.0.0.1/jwks" -AllowLoopbackHttp $true
Assert-True $r.Acceptable "Loopback opt-in: http://127.0.0.1 accepted"
Assert-True $r.Loopback "Loopback opt-in: http://127.0.0.1 flagged"

$r = Check-Uri "http://[::1]/jwks" -AllowLoopbackHttp $true
Assert-True $r.Acceptable "Loopback opt-in: http://[::1] (IPv6) accepted"
Assert-True $r.Loopback "Loopback opt-in: http://[::1] flagged"

# Empty / null / malformed rejected regardless of flag
$r = Check-Uri ""
Assert-True (-not $r.Acceptable) "Empty URI rejected"

$r = Check-Uri "not a url"
Assert-True (-not $r.Acceptable) "Malformed URI rejected"

# Other schemes rejected regardless of flag
$r = Check-Uri "ftp://idp.example.com/jwks" -AllowLoopbackHttp $true
Assert-True (-not $r.Acceptable) "ftp scheme rejected even with loopback opt-in"

$r = Check-Uri "file:///etc/passwd" -AllowLoopbackHttp $true
Assert-True (-not $r.Acceptable) "file scheme rejected even with loopback opt-in"
