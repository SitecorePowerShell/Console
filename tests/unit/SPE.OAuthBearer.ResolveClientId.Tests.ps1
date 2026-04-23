# Unit tests for OAuthBearerTokenAuthenticationProvider.ResolveClientId.
# Covers the IdP-specific client id claim fallback chain:
#   client_id -> azp -> appid -> cid
# Rationale per IdP:
#   client_id : OIDC standard (Sitecore IDS, IdentityServer4, Keycloak, Cognito, Ping)
#   azp       : OIDC Authorized Party (Auth0 M2M, Azure AD v2.0, Google)
#   appid     : Azure AD v1.0 legacy
#   cid       : Okta-specific

Write-Host "`n  [Loading assemblies]" -ForegroundColor White

$abstractionsPath = "$PSScriptRoot\..\..\src\Spe.Abstractions\bin\Debug\Spe.Abstractions.dll"
$spePath          = "$PSScriptRoot\..\..\src\Spe\bin\Debug\Spe.dll"

if (-not ((Test-Path $abstractionsPath) -and (Test-Path $spePath))) {
    Skip-Test "All ResolveClientId tests" "Build artifacts not found - run 'task build' first"
    return
}

try { Add-Type -Path $abstractionsPath -ErrorAction SilentlyContinue } catch { }
try { Add-Type -Path $spePath          -ErrorAction SilentlyContinue } catch { }

$providerType = [Spe.Core.Settings.Authorization.OAuthBearerTokenAuthenticationProvider]
$resolveMethod = $providerType.GetMethod("ResolveClientId",
    [System.Reflection.BindingFlags]::Public -bor
    [System.Reflection.BindingFlags]::Static)

if (-not $resolveMethod) {
    Skip-Test "All ResolveClientId tests" "ResolveClientId method not found on OAuthBearerTokenAuthenticationProvider"
    return
}

function Invoke-Resolve {
    param([hashtable]$Claims)
    $dict = New-Object 'System.Collections.Generic.Dictionary[string,object]'
    foreach ($k in $Claims.Keys) { $dict[$k] = $Claims[$k] }
    return [Spe.Core.Settings.Authorization.OAuthBearerTokenAuthenticationProvider]::ResolveClientId($dict)
}

Write-Host "`n  [ResolveClientId - client_id present (OIDC standard)]" -ForegroundColor White
$result = Invoke-Resolve @{ client_id = "spe-remoting" }
Assert-Equal $result "spe-remoting" "client_id is returned when present"

Write-Host "`n  [ResolveClientId - azp fallback (Auth0, Azure v2)]" -ForegroundColor White
$result = Invoke-Resolve @{ azp = "kGmQyirqWAoChuJZ8rkQmI5UWbhhX7wK" }
Assert-Equal $result "kGmQyirqWAoChuJZ8rkQmI5UWbhhX7wK" "azp is used when client_id absent"

Write-Host "`n  [ResolveClientId - appid fallback (Azure AD v1.0)]" -ForegroundColor White
$result = Invoke-Resolve @{ appid = "00000000-0000-0000-0000-000000000001" }
Assert-Equal $result "00000000-0000-0000-0000-000000000001" "appid is used when client_id and azp absent"

Write-Host "`n  [ResolveClientId - cid fallback (Okta)]" -ForegroundColor White
$result = Invoke-Resolve @{ cid = "0oabc123def456" }
Assert-Equal $result "0oabc123def456" "cid is used as last fallback"

Write-Host "`n  [ResolveClientId - client_id wins over azp when both present]" -ForegroundColor White
$result = Invoke-Resolve @{ client_id = "primary"; azp = "authorized-party" }
Assert-Equal $result "primary" "client_id takes precedence over azp (deterministic for token-exchange flows)"

Write-Host "`n  [ResolveClientId - azp wins over appid when both present]" -ForegroundColor White
$result = Invoke-Resolve @{ azp = "auth0-client"; appid = "azure-legacy" }
Assert-Equal $result "auth0-client" "azp takes precedence over appid"

Write-Host "`n  [ResolveClientId - appid wins over cid when both present]" -ForegroundColor White
$result = Invoke-Resolve @{ appid = "azure-app"; cid = "okta-cid" }
Assert-Equal $result "azure-app" "appid takes precedence over cid"

Write-Host "`n  [ResolveClientId - no client-id claim returns null]" -ForegroundColor White
$result = Invoke-Resolve @{ sub = "user@example"; iss = "https://idp.local" }
Assert-True ($null -eq $result) "No matching claim returns null (caller treats as 'clientId=none')"

Write-Host "`n  [ResolveClientId - empty string claim is skipped]" -ForegroundColor White
$result = Invoke-Resolve @{ client_id = ""; azp = "fallback" }
Assert-Equal $result "fallback" "Empty client_id is treated as absent; falls through to azp"

Write-Host "`n  [ResolveClientId - null claim value is skipped]" -ForegroundColor White
$result = Invoke-Resolve @{ client_id = $null; azp = "fallback" }
Assert-Equal $result "fallback" "Null client_id value is treated as absent; falls through to azp"
