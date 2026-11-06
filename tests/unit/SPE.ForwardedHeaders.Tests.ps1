# Unit tests for #1447: Spe.Remoting.UseForwardedHeaders gate for X-Forwarded-Proto and X-Forwarded-For
# Verifies the pure helpers that drive the proto check and the IP extraction.

# ============================================================
# Load compiled assemblies
# ============================================================
Write-Host "`n  [Loading assemblies]" -ForegroundColor White

$abstractionsPath = "$PSScriptRoot\..\..\src\Spe.Abstractions\bin\Debug\Spe.Abstractions.dll"
$spePath = "$PSScriptRoot\..\..\src\Spe\bin\Debug\Spe.dll"

$canLoadAssemblies = (Test-Path $abstractionsPath) -and (Test-Path $spePath)
if (-not $canLoadAssemblies) {
    Skip-Test "All ForwardedHeaders tests" "Build artifacts not found -- run 'task build' first"
    return
}

try { Add-Type -Path $abstractionsPath -ErrorAction SilentlyContinue } catch { }
try { Add-Type -Path $spePath -ErrorAction SilentlyContinue } catch { }

$asm = [Spe.Core.Settings.Authorization.WebServiceSettings].Assembly
$helperType = $asm.GetType("Spe.Core.Settings.Authorization.ForwardedHeaderHelper")
if (-not $helperType) {
    Skip-Test "All ForwardedHeaders tests" "ForwardedHeaderHelper type not found in assembly"
    return
}

$tryGetClientIp = $helperType.GetMethod("TryGetClientIp", [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Static)
$shouldAcceptProto = $helperType.GetMethod("ShouldAcceptForwardedProto", [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Static)

if (-not $tryGetClientIp) {
    Skip-Test "TryGetClientIp tests" "Method not found"
}
if (-not $shouldAcceptProto) {
    Skip-Test "ShouldAcceptForwardedProto tests" "Method not found"
}

function Invoke-TryGetClientIp {
    param([string]$HeaderValue)
    $args = [object[]]@($HeaderValue, $null)
    $ok = $tryGetClientIp.Invoke($null, $args)
    [pscustomobject]@{ Ok = [bool]$ok; Ip = [string]$args[1] }
}

# ============================================================
# Test: TryGetClientIp - empty / null
# ============================================================
if ($tryGetClientIp) {
    Write-Host "`n  [TryGetClientIp - empty/null]" -ForegroundColor White

    $r = Invoke-TryGetClientIp -HeaderValue $null
    Assert-True (-not $r.Ok) "Null header returns false"

    $r = Invoke-TryGetClientIp -HeaderValue ""
    Assert-True (-not $r.Ok) "Empty header returns false"

    $r = Invoke-TryGetClientIp -HeaderValue "   "
    Assert-True (-not $r.Ok) "Whitespace-only header returns false"

    # ============================================================
    # Test: TryGetClientIp - valid IPv4
    # ============================================================
    Write-Host "`n  [TryGetClientIp - valid IPv4]" -ForegroundColor White

    $r = Invoke-TryGetClientIp -HeaderValue "203.0.113.42"
    Assert-True $r.Ok "Plain IPv4 accepted"
    Assert-Equal $r.Ip "203.0.113.42" "Plain IPv4 returned verbatim"

    $r = Invoke-TryGetClientIp -HeaderValue "  203.0.113.42  "
    Assert-True $r.Ok "Whitespace-padded IPv4 accepted"
    Assert-Equal $r.Ip "203.0.113.42" "Whitespace trimmed"

    # ============================================================
    # Test: TryGetClientIp - valid IPv6
    # ============================================================
    Write-Host "`n  [TryGetClientIp - valid IPv6]" -ForegroundColor White

    $r = Invoke-TryGetClientIp -HeaderValue "2001:db8::1"
    Assert-True $r.Ok "IPv6 accepted"
    Assert-Equal $r.Ip "2001:db8::1" "IPv6 returned verbatim"

    # ============================================================
    # Test: TryGetClientIp - comma-separated chain
    # ============================================================
    Write-Host "`n  [TryGetClientIp - comma chain]" -ForegroundColor White

    $r = Invoke-TryGetClientIp -HeaderValue "203.0.113.42, 198.51.100.7, 10.0.0.1"
    Assert-True $r.Ok "Comma chain accepted"
    Assert-Equal $r.Ip "203.0.113.42" "First token in chain returned"

    # ============================================================
    # Test: TryGetClientIp - garbage / log-injection
    # ============================================================
    Write-Host "`n  [TryGetClientIp - garbage rejected]" -ForegroundColor White

    $r = Invoke-TryGetClientIp -HeaderValue "not-an-ip"
    Assert-True (-not $r.Ok) "Plain garbage rejected"

    $r = Invoke-TryGetClientIp -HeaderValue "1.2.3.4`r`nINJECTED"
    Assert-True (-not $r.Ok) "CR/LF in value rejected"

    $r = Invoke-TryGetClientIp -HeaderValue "999.999.999.999"
    Assert-True (-not $r.Ok) "Out-of-range octets rejected"

    $r = Invoke-TryGetClientIp -HeaderValue "[2001:db8::1]:8080"
    Assert-True (-not $r.Ok) "IPv6 with bracketed port rejected (don't infer port stripping)"

    $r = Invoke-TryGetClientIp -HeaderValue "garbage, 198.51.100.7"
    Assert-True (-not $r.Ok) "Garbage first token does not fall through to second"
}

# ============================================================
# Test: ShouldAcceptForwardedProto - decision matrix
# ============================================================
if ($shouldAcceptProto) {
    Write-Host "`n  [ShouldAcceptForwardedProto - decision matrix]" -ForegroundColor White

    # Args: requireSecureConnection, isSecureConnection, forwardedProtoHeader, useForwardedHeaders
    $r = $shouldAcceptProto.Invoke($null, @($false, $false, $null, $false))
    Assert-True $r "RequireSecure=false: always accept"

    $r = $shouldAcceptProto.Invoke($null, @($true, $true, $null, $false))
    Assert-True $r "Direct HTTPS accepted regardless of UseForwardedHeaders"

    $r = $shouldAcceptProto.Invoke($null, @($true, $true, "http", $false))
    Assert-True $r "Direct HTTPS still accepted even when header disagrees"

    $r = $shouldAcceptProto.Invoke($null, @($true, $false, "https", $false))
    Assert-True (-not $r) "Spoofed X-Forwarded-Proto rejected when UseForwardedHeaders=false"

    $r = $shouldAcceptProto.Invoke($null, @($true, $false, $null, $true))
    Assert-True (-not $r) "Plain HTTP with no proto header rejected even when trusting headers"

    $r = $shouldAcceptProto.Invoke($null, @($true, $false, "http", $true))
    Assert-True (-not $r) "Forwarded proto=http rejected when trusting headers"

    $r = $shouldAcceptProto.Invoke($null, @($true, $false, "https", $true))
    Assert-True $r "Forwarded proto=https accepted when trusting headers"

    $r = $shouldAcceptProto.Invoke($null, @($true, $false, "HTTPS", $true))
    Assert-True $r "Forwarded proto matched case-insensitively"
}

# ============================================================
# Test: WebServiceSettings.UseForwardedHeaders is readable
# ============================================================
Write-Host "`n  [WebServiceSettings - UseForwardedHeaders property]" -ForegroundColor White

$prop = [Spe.Core.Settings.Authorization.WebServiceSettings].GetProperty("UseForwardedHeaders", [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Static)
Assert-NotNull $prop "WebServiceSettings.UseForwardedHeaders property exists"
if ($prop) {
    # Default is true in 9.0 line: preserves pre-9.0 behavior so existing proxy
    # deployments and audit-log consumers are not broken on upgrade. Hardened
    # mode (false) is opt-in. Default flips to false in 10.0.
    try {
        $value = $prop.GetValue($null)
        Assert-Equal $value $true "UseForwardedHeaders defaults to true (preserves pre-9.0 behavior; hardened mode is opt-in)"
    } catch {
        Skip-Test "UseForwardedHeaders default value" "Static ctor unavailable outside Sitecore: $_"
    }
}
