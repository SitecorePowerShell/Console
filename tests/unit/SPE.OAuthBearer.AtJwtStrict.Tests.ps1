# Unit tests for JwtClaimValidator.IsValidTokenType / RequireAccessTokenType
# strict mode (issue #1485). Default accepts no typ / "JWT" / "at+jwt".
# Strict requires "at+jwt" exactly (RFC 9068).

Write-Host "`n  [JwtClaimValidator: IsValidTokenType]" -ForegroundColor White

$abstractionsPath = "$PSScriptRoot\..\..\src\Spe.Abstractions\bin\Debug\Spe.Abstractions.dll"
$spePath          = "$PSScriptRoot\..\..\src\Spe\bin\Debug\Spe.dll"

if (-not ((Test-Path $abstractionsPath) -and (Test-Path $spePath))) {
    Skip-Test "All AtJwtStrict tests" "Build artifacts not found - run 'task build' first"
    return
}

try { Add-Type -Path $abstractionsPath -ErrorAction SilentlyContinue } catch { }
try { Add-Type -Path $spePath          -ErrorAction SilentlyContinue } catch { }

$asm = [Spe.Core.Settings.Authorization.OAuthBearerTokenAuthenticationProvider].Assembly
$validatorType = $asm.GetType("Spe.Core.Settings.Authorization.JwtClaimValidator")
$method = $validatorType.GetMethod("IsValidTokenType",
    [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Static)

if (-not $method) {
    Skip-Test "All AtJwtStrict tests" "IsValidTokenType not found on JwtClaimValidator"
    return
}

function Check-Type {
    param([string]$Typ, [bool]$Strict)
    return $method.Invoke($null, @([object]$Typ, [object]$Strict))
}

# Default mode: no typ accepted
Assert-True (Check-Type -Typ $null -Strict $false) "Default mode: missing typ accepted"
Assert-True (Check-Type -Typ ""    -Strict $false) "Default mode: empty typ accepted"
Assert-True (Check-Type -Typ "JWT" -Strict $false) "Default mode: typ=JWT accepted"
Assert-True (Check-Type -Typ "jwt" -Strict $false) "Default mode: typ=jwt (case-insensitive) accepted"
Assert-True (Check-Type -Typ "at+jwt" -Strict $false) "Default mode: typ=at+jwt accepted"
Assert-True (-not (Check-Type -Typ "Bearer" -Strict $false)) "Default mode: typ=Bearer rejected"
Assert-True (-not (Check-Type -Typ "junk"   -Strict $false)) "Default mode: unknown typ rejected"

# Strict mode: only at+jwt accepted
Assert-True (-not (Check-Type -Typ $null   -Strict $true)) "Strict mode: missing typ rejected"
Assert-True (-not (Check-Type -Typ ""      -Strict $true)) "Strict mode: empty typ rejected"
Assert-True (-not (Check-Type -Typ "JWT"   -Strict $true)) "Strict mode: typ=JWT rejected"
Assert-True (Check-Type -Typ "at+jwt"      -Strict $true)  "Strict mode: typ=at+jwt accepted"
Assert-True (Check-Type -Typ "AT+JWT"      -Strict $true)  "Strict mode: typ=AT+JWT (case-insensitive) accepted"
