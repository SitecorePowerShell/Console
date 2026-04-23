# Unit tests for OAuthBearerTokenAuthenticationProvider issuer-routed dispatch
# (issue #1483). Two helpers are exercised:
#
#   TryPeekIssuer(token, out iss) - base64-decode the payload and extract iss.
#                                   Runs before signature verification, so an
#                                   attacker can spoof the value. Callers use
#                                   it only to pick which provider will then
#                                   do the signed validation.
#
#   SelectProvider(providers, alg, iss) - among the registered providers, pick
#                                         one that accepts both the token's
#                                         algorithm and its issuer.
#
# The selection intentionally requires an issuer match so that declaring
# multiple <oauthBearer> entries (one per IdP) routes deterministically
# rather than last-declared-wins on alg alone.

Write-Host "`n  [Loading assemblies]" -ForegroundColor White

$abstractionsPath = "$PSScriptRoot\..\..\src\Spe.Abstractions\bin\Debug\Spe.Abstractions.dll"
$spePath          = "$PSScriptRoot\..\..\src\Spe\bin\Debug\Spe.dll"

if (-not ((Test-Path $abstractionsPath) -and (Test-Path $spePath))) {
    Skip-Test "All IssuerRouting tests" "Build artifacts not found - run 'task build' first"
    return
}

try { Add-Type -Path $abstractionsPath -ErrorAction SilentlyContinue } catch { }
try { Add-Type -Path $spePath          -ErrorAction SilentlyContinue } catch { }

$providerType = [Spe.Core.Settings.Authorization.OAuthBearerTokenAuthenticationProvider]
$peekMethod   = $providerType.GetMethod("TryPeekIssuer",
    [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Static)
$selectMethod = $providerType.GetMethod("SelectProvider",
    [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Static)

if (-not $peekMethod -or -not $selectMethod) {
    Skip-Test "All IssuerRouting tests" "TryPeekIssuer / SelectProvider methods not found on OAuthBearerTokenAuthenticationProvider"
    return
}

# ------------------------------------------------------------
# JWT builder - header.payload.signature with fake signature.
# Valid for dispatch-level tests because signature is only checked
# by the provider's Validate, not by TryPeekIssuer / SelectProvider.
# ------------------------------------------------------------
function New-FakeJwt {
    param([hashtable]$Header, [hashtable]$Payload)
    $headerJson  = ($Header  | ConvertTo-Json -Compress)
    $payloadJson = ($Payload | ConvertTo-Json -Compress)
    $enc = {
        param($s)
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($s)
        ([Convert]::ToBase64String($bytes)).TrimEnd('=').Replace('+','-').Replace('/','_')
    }
    return "$(& $enc $headerJson).$(& $enc $payloadJson).fake-signature"
}

# ============================================================
# TryPeekIssuer
# ============================================================
Write-Host "`n  [TryPeekIssuer - returns iss from payload]" -ForegroundColor White
$jwt = New-FakeJwt -Header @{ alg = "RS256"; typ = "JWT" } -Payload @{ iss = "https://speid.dev.local"; aud = "spe-remoting" }
$outArgs = [object[]]::new(2); $outArgs[0] = $jwt; $outArgs[1] = $null
$ok = $peekMethod.Invoke($null, $outArgs)
Assert-True $ok "TryPeekIssuer returns true on a well-formed JWT"
Assert-Equal $outArgs[1] "https://speid.dev.local" "TryPeekIssuer extracts the iss claim"

Write-Host "`n  [TryPeekIssuer - missing iss returns false]" -ForegroundColor White
$jwt = New-FakeJwt -Header @{ alg = "RS256" } -Payload @{ aud = "spe-remoting" }
$outArgs = [object[]]::new(2); $outArgs[0] = $jwt; $outArgs[1] = $null
$ok = $peekMethod.Invoke($null, $outArgs)
Assert-True (-not $ok) "TryPeekIssuer returns false when iss claim is absent"

Write-Host "`n  [TryPeekIssuer - malformed token returns false]" -ForegroundColor White
$outArgs = [object[]]::new(2); $outArgs[0] = "not-a-jwt"; $outArgs[1] = $null
$ok = $peekMethod.Invoke($null, $outArgs)
Assert-True (-not $ok) "TryPeekIssuer returns false on a non-JWT string"

Write-Host "`n  [TryPeekIssuer - null token returns false]" -ForegroundColor White
$outArgs = [object[]]::new(2); $outArgs[0] = $null; $outArgs[1] = $null
$ok = $peekMethod.Invoke($null, $outArgs)
Assert-True (-not $ok) "TryPeekIssuer returns false on null token"

# ============================================================
# SelectProvider
# ============================================================
function New-Provider {
    param([string[]]$Issuers, [string[]]$Algorithms = @("RS256"))
    $p = New-Object Spe.Core.Settings.Authorization.OAuthBearerTokenAuthenticationProvider
    $p.AllowedIssuers = [System.Collections.Generic.List[string]]::new()
    foreach ($i in $Issuers) { [void]$p.AllowedIssuers.Add($i) }
    $p.AllowedAlgorithms = [System.Collections.Generic.List[string]]::new()
    foreach ($a in $Algorithms) { [void]$p.AllowedAlgorithms.Add($a) }
    return $p
}

Write-Host "`n  [SelectProvider - single provider matches issuer]" -ForegroundColor White
$ids = New-Provider -Issuers @("https://speid.dev.local")
$list = [System.Collections.Generic.List[Spe.Abstractions.VersionDecoupling.Interfaces.ISpeAuthenticationProvider]]::new()
[void]$list.Add($ids)
$result = $selectMethod.Invoke($null, [object[]]@($list, "RS256", "https://speid.dev.local"))
Assert-Equal $result $ids "Returns the sole provider whose AllowedIssuers contains the token's iss"

Write-Host "`n  [SelectProvider - routes by iss when two providers share alg]" -ForegroundColor White
$ids   = New-Provider -Issuers @("https://speid.dev.local")
$auth0 = New-Provider -Issuers @("https://tenant.us.auth0.com/")
$list = [System.Collections.Generic.List[Spe.Abstractions.VersionDecoupling.Interfaces.ISpeAuthenticationProvider]]::new()
[void]$list.Add($ids); [void]$list.Add($auth0)
$r1 = $selectMethod.Invoke($null, [object[]]@($list, "RS256", "https://speid.dev.local"))
$r2 = $selectMethod.Invoke($null, [object[]]@($list, "RS256", "https://tenant.us.auth0.com/"))
Assert-Equal $r1 $ids   "IDS token routes to IDS provider"
Assert-Equal $r2 $auth0 "Auth0 token routes to Auth0 provider (not last-declared-wins)"

Write-Host "`n  [SelectProvider - unknown issuer returns null]" -ForegroundColor White
$list = [System.Collections.Generic.List[Spe.Abstractions.VersionDecoupling.Interfaces.ISpeAuthenticationProvider]]::new()
[void]$list.Add((New-Provider -Issuers @("https://speid.dev.local")))
$result = $selectMethod.Invoke($null, [object[]]@($list, "RS256", "https://attacker.example/"))
Assert-True ($null -eq $result) "No provider returned for unknown issuer (caller 401s)"

Write-Host "`n  [SelectProvider - alg mismatch filters provider out]" -ForegroundColor White
$es = New-Provider -Issuers @("https://speid.dev.local") -Algorithms @("ES256")
$rs = New-Provider -Issuers @("https://speid.dev.local") -Algorithms @("RS256")
$list = [System.Collections.Generic.List[Spe.Abstractions.VersionDecoupling.Interfaces.ISpeAuthenticationProvider]]::new()
[void]$list.Add($es); [void]$list.Add($rs)
$r = $selectMethod.Invoke($null, [object[]]@($list, "RS256", "https://speid.dev.local"))
Assert-Equal $r $rs "RS256 token picks the RS-accepting provider, skipping the ES-only sibling even though iss matches"

Write-Host "`n  [SelectProvider - case-insensitive issuer match]" -ForegroundColor White
$p = New-Provider -Issuers @("https://SpeID.Dev.Local")
$list = [System.Collections.Generic.List[Spe.Abstractions.VersionDecoupling.Interfaces.ISpeAuthenticationProvider]]::new()
[void]$list.Add($p)
$r = $selectMethod.Invoke($null, [object[]]@($list, "RS256", "https://speid.dev.local"))
Assert-Equal $r $p "Issuer comparison is case-insensitive (scheme + host)"

Write-Host "`n  [SelectProvider - null issuer returns null]" -ForegroundColor White
$list = [System.Collections.Generic.List[Spe.Abstractions.VersionDecoupling.Interfaces.ISpeAuthenticationProvider]]::new()
[void]$list.Add((New-Provider -Issuers @("https://speid.dev.local")))
$result = $selectMethod.Invoke($null, [object[]]@($list, "RS256", $null))
Assert-True ($null -eq $result) "Null iss returns null (OIDC requires iss; caller 401s)"

Write-Host "`n  [SelectProvider - skips non-OAuth providers in the list]" -ForegroundColor White
# SharedSecret providers may share the list; SelectProvider must ignore them.
$shared = New-Object Spe.Core.Settings.Authorization.SharedSecretAuthenticationProvider
$oauth  = New-Provider -Issuers @("https://speid.dev.local")
$list = [System.Collections.Generic.List[Spe.Abstractions.VersionDecoupling.Interfaces.ISpeAuthenticationProvider]]::new()
[void]$list.Add($shared); [void]$list.Add($oauth)
$r = $selectMethod.Invoke($null, [object[]]@($list, "RS256", "https://speid.dev.local"))
Assert-Equal $r $oauth "SharedSecret providers are skipped when selecting an OAuth provider"
