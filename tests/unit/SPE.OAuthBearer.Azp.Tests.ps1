# Unit tests for JwtClaimValidator.IsValidAzp / RequireAzpWhenMultiAudience
# (issue #1485). When enabled and aud has >1 value, azp must be present and
# match the resolved client_id used for OAuth Client item lookup.

Write-Host "`n  [JwtClaimValidator: IsValidAzp]" -ForegroundColor White

$abstractionsPath = "$PSScriptRoot\..\..\src\Spe.Abstractions\bin\Debug\Spe.Abstractions.dll"
$spePath          = "$PSScriptRoot\..\..\src\Spe\bin\Debug\Spe.dll"

if (-not ((Test-Path $abstractionsPath) -and (Test-Path $spePath))) {
    Skip-Test "All Azp tests" "Build artifacts not found - run 'task build' first"
    return
}

try { Add-Type -Path $abstractionsPath -ErrorAction SilentlyContinue } catch { }
try { Add-Type -Path $spePath          -ErrorAction SilentlyContinue } catch { }

$asm = [Spe.Core.Settings.Authorization.OAuthBearerTokenAuthenticationProvider].Assembly
$validatorType = $asm.GetType("Spe.Core.Settings.Authorization.JwtClaimValidator")
$method = $validatorType.GetMethod("IsValidAzp",
    [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Static)

if (-not $method) {
    Skip-Test "All Azp tests" "IsValidAzp not found on JwtClaimValidator"
    return
}

function Check-Azp {
    param(
        [string[]]$Audiences,
        [string]$Azp,
        [string]$ClientId,
        [bool]$Required
    )
    $list = New-Object 'System.Collections.Generic.List[string]'
    if ($Audiences) { foreach ($a in $Audiences) { [void]$list.Add($a) } }
    return $method.Invoke($null, @($list.psobject.BaseObject, $Azp, $ClientId, $Required))
}

# Flag off: never checks
Assert-True (Check-Azp -Audiences @("a","b") -Azp $null -ClientId "x" -Required $false) "Flag off: skipped even with multi-aud + missing azp"

# Single audience: skipped regardless of azp
Assert-True (Check-Azp -Audiences @("a") -Azp $null -ClientId "x" -Required $true) "Single-aud + flag on: skipped (no azp needed)"
Assert-True (Check-Azp -Audiences @("a") -Azp "y"  -ClientId "x" -Required $true) "Single-aud + flag on: azp/clientId mismatch ignored"

# Empty / null audiences: skipped (audience validation is a separate gate)
Assert-True (Check-Azp -Audiences @() -Azp $null -ClientId "x" -Required $true) "Empty audiences: skipped"

# Multi-audience + flag on: must have azp
Assert-True (-not (Check-Azp -Audiences @("a","b") -Azp $null -ClientId "client-1" -Required $true)) "Multi-aud + flag on: missing azp rejected"
Assert-True (-not (Check-Azp -Audiences @("a","b") -Azp ""    -ClientId "client-1" -Required $true)) "Multi-aud + flag on: empty azp rejected"

# Multi-audience + flag on: azp must match
Assert-True (Check-Azp -Audiences @("a","b") -Azp "client-1" -ClientId "client-1" -Required $true) "Multi-aud + flag on: matching azp accepted"
Assert-True (-not (Check-Azp -Audiences @("a","b") -Azp "wrong" -ClientId "client-1" -Required $true)) "Multi-aud + flag on: azp mismatch rejected"

# Case-sensitive on the match (client ids are typically opaque tokens)
Assert-True (-not (Check-Azp -Audiences @("a","b") -Azp "Client-1" -ClientId "client-1" -Required $true)) "Multi-aud + flag on: case differences rejected (case-sensitive match)"
