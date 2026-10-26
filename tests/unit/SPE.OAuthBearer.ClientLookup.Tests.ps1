# Unit tests for the (issuer, client_id) lookup that maps a validated OAuth
# token to its Remoting Client item. Issuers compare in canonical form (same
# rule as IsValidIssuer); client ids are case-sensitive, so two IdP clients
# that differ only by case never share a Remoting Client.

Write-Host "`n  [RemotingClientProvider: issuer/client_id lookup key]" -ForegroundColor White

$abstractionsPath = "$PSScriptRoot\..\..\src\Spe.Abstractions\bin\Debug\Spe.Abstractions.dll"
$spePath          = "$PSScriptRoot\..\..\src\Spe\bin\Debug\Spe.dll"

if (-not ((Test-Path $abstractionsPath) -and (Test-Path $spePath))) {
    Skip-Test "All ClientLookup tests" "Build artifacts not found - run 'task build' first"
    return
}

try { Add-Type -Path $abstractionsPath -ErrorAction SilentlyContinue } catch { }
try { Add-Type -Path $spePath          -ErrorAction SilentlyContinue } catch { }

$staticAny = [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Static
$providerType = [Spe.Core.Settings.Authorization.RemotingClientProvider]
$makeKey = $providerType.GetMethod("MakeIssuerClientIdKey", $staticAny)
$comparerField = $providerType.GetField("IssuerClientIdComparer", $staticAny)

Assert-NotNull $makeKey "MakeIssuerClientIdKey exists"
Assert-NotNull $comparerField "IssuerClientIdComparer exists"
if (-not $makeKey -or -not $comparerField) { return }

$comparer = $comparerField.GetValue($null)
function Test-SameClient($IssuerA, $ClientA, $IssuerB, $ClientB) {
    $comparer.Equals($makeKey.Invoke($null, @($IssuerA, $ClientA)), $makeKey.Invoke($null, @($IssuerB, $ClientB)))
}

Assert-True (Test-SameClient "https://idp.example/" "App1" "https://IDP.example" "App1") "Trailing slash and host case do not change the lookup"
Assert-True (Test-SameClient " https://idp.example " " App1 " "https://idp.example" "App1") "Surrounding whitespace is ignored"
Assert-True (-not (Test-SameClient "https://idp.example" "App1" "https://idp.example" "app1")) "Client ids that differ only by case are different clients"
Assert-True (-not (Test-SameClient "https://idp.example/realms/Prod" "app" "https://idp.example/realms/prod" "app")) "Issuer path case is significant"
Assert-True (-not (Test-SameClient "https://idp-a.example" "app" "https://idp-b.example" "app")) "Same client id under different issuers stays separate"
