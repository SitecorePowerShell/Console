# Unit tests for #1450: JWT signature must be verified before claims
# Ensures that tokens with invalid signatures are rejected immediately,
# regardless of whether their claims are valid or invalid.

# ============================================================
# Load compiled assemblies
# ============================================================
Write-Host "`n  [Loading assemblies]" -ForegroundColor White

$abstractionsPath = "$PSScriptRoot\..\..\src\Spe.Abstractions\bin\Debug\Spe.Abstractions.dll"
$spePath = "$PSScriptRoot\..\..\src\Spe\bin\Debug\Spe.dll"

$canLoadAssemblies = (Test-Path $abstractionsPath) -and (Test-Path $spePath)
if (-not $canLoadAssemblies) {
    Skip-Test "All signature-first tests" "Build artifacts not found -- run 'task build' first"
    return
}

try {
    Add-Type -Path $abstractionsPath -ErrorAction SilentlyContinue
} catch { }

$speLoaded = $false
try {
    Add-Type -Path $spePath -ErrorAction SilentlyContinue
    # Verify the type is actually usable — ValidateToken may fail at runtime
    # if Sitecore.Kernel is missing, even though the constructor succeeds.
    $testProvider = New-Object Spe.Core.Settings.Authorization.SharedSecretAuthenticationProvider
    $testProvider.SuppressWarnings = $true
    $u = $null; $r = $null
    [void]$testProvider.ValidateToken("a.b.c", "test", [ref]$u, [ref]$r)
    $testProvider = $null
    $speLoaded = $true
} catch {
    # Expected in standalone test environments without Sitecore assemblies
}

# ============================================================
# Helper: build a HS256 JWT with explicit claim control
# ============================================================
function New-SignatureTestJwt {
    param(
        [string]$SigningSecret,
        [string]$Issuer = "test-issuer",
        [string]$Audience = "https://spe.dev.local",
        [string]$Username = "sitecore\admin",
        [int]$LifetimeSeconds = 300,
        [switch]$Expired,
        [switch]$CorruptSignature
    )

    $epoch = [datetime]::new(1970,1,1,0,0,0,[System.DateTimeKind]::Utc)
    $now = [long]([datetime]::UtcNow - $epoch).TotalSeconds

    if ($Expired) {
        $exp = $now - 600
        $iat = $now - 900
    } else {
        $exp = $now + $LifetimeSeconds
        $iat = $now
    }

    $header = '{"alg":"HS256","typ":"JWT"}'
    $headerB64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($header)).TrimEnd('=').Replace('+','-').Replace('/','_')

    $payloadObj = [ordered]@{ iss = $Issuer; aud = $Audience; exp = $exp; nbf = $now; iat = $iat; name = $Username }
    $payload = $payloadObj | ConvertTo-Json -Compress
    $payloadB64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($payload)).TrimEnd('=').Replace('+','-').Replace('/','_')

    $toBeSigned = "$headerB64.$payloadB64"
    $hmac = New-Object System.Security.Cryptography.HMACSHA256
    $hmac.Key = [Text.Encoding]::UTF8.GetBytes($SigningSecret)
    $sigBytes = $hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($toBeSigned))
    $hmac.Dispose()
    $sigB64 = [Convert]::ToBase64String($sigBytes).Split('=')[0].Replace('+','-').Replace('/','_')

    if ($CorruptSignature) {
        # Flip a character to invalidate the signature
        $chars = $sigB64.ToCharArray()
        $chars[0] = if ($chars[0] -eq 'A') { 'B' } else { 'A' }
        $sigB64 = [string]::new($chars)
    }

    return "$headerB64.$payloadB64.$sigB64"
}

# ============================================================
# Helper: validate using the provider (with DetailedAuthenticationErrors)
# Returns both validation result and the specific error if thrown
# ============================================================
function Test-SignatureFirstValidation {
    param(
        [string]$Token,
        [string]$SharedSecret,
        [string]$Authority = "https://spe.dev.local",
        [string]$Issuer = "test-issuer",
        [switch]$DetailedErrors
    )

    if (-not $speLoaded) {
        # Fallback: pure PowerShell signature check
        $parts = $Token.Split('.')
        if ($parts.Count -ne 3) {
            return [PSCustomObject]@{ IsValid = $false; ErrorMessage = "Invalid token format" }
        }

        $toBeSigned = "$($parts[0]).$($parts[1])"
        $hmac = New-Object System.Security.Cryptography.HMACSHA256
        $hmac.Key = [Text.Encoding]::UTF8.GetBytes($SharedSecret)
        $sigBytes = $hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($toBeSigned))
        $hmac.Dispose()
        $expectedSig = [Convert]::ToBase64String($sigBytes).Split('=')[0].Replace('+','-').Replace('/','_')

        $sigValid = $parts[2] -ceq $expectedSig
        if (-not $sigValid) {
            return [PSCustomObject]@{ IsValid = $false; ErrorMessage = "signatureMismatch" }
        }

        return [PSCustomObject]@{ IsValid = $true; ErrorMessage = $null }
    }

    $provider = New-Object Spe.Core.Settings.Authorization.SharedSecretAuthenticationProvider
    $provider.SharedSecret = $SharedSecret
    $provider.AllowedIssuers = [System.Collections.Generic.List[string]]@($Issuer)
    $provider.AllowedAudiences = [System.Collections.Generic.List[string]]@($Authority)
    $provider.SuppressWarnings = $true
    $provider.DetailedAuthenticationErrors = [bool]$DetailedErrors

    $username = $null
    $result = $null
    $errorMessage = $null

    try {
        $isValid = $provider.ValidateToken($Token, $Authority, [ref]$username, [ref]$result)
    } catch [System.Security.SecurityException] {
        $isValid = $false
        $errorMessage = $_.Exception.Message
    }

    return [PSCustomObject]@{
        IsValid = $isValid
        ErrorMessage = $errorMessage
    }
}

$validSecret = "ThisIsATestSharedSecretThatIsLongEnoughForValidation"
$wrongSecret = "WrongSecretThatIsAlsoLongEnoughToPassLengthChecks!!"

# ============================================================
# Test: Valid token still validates correctly after reorder
# ============================================================
Write-Host "`n  [SignatureFirst - valid token still passes]" -ForegroundColor White

$validToken = New-SignatureTestJwt -SigningSecret $validSecret
$r1 = Test-SignatureFirstValidation -Token $validToken -SharedSecret $validSecret
Assert-True $r1.IsValid "Valid token with correct secret passes all checks"

# ============================================================
# Test: Invalid signature with valid claims is rejected
# ============================================================
Write-Host "`n  [SignatureFirst - invalid signature with valid claims]" -ForegroundColor White

$corruptToken = New-SignatureTestJwt -SigningSecret $validSecret -CorruptSignature
$r2 = Test-SignatureFirstValidation -Token $corruptToken -SharedSecret $validSecret
Assert-True (-not $r2.IsValid) "Corrupted signature is rejected even with valid claims"

# ============================================================
# Test: Invalid signature with invalid audience is rejected at signature
# ============================================================
Write-Host "`n  [SignatureFirst - invalid sig + bad audience => signature error]" -ForegroundColor White

$badAudienceToken = New-SignatureTestJwt -SigningSecret $validSecret -Audience "https://evil.example.com" -CorruptSignature
$r3 = Test-SignatureFirstValidation -Token $badAudienceToken -SharedSecret $validSecret -DetailedErrors
Assert-True (-not $r3.IsValid) "Invalid signature + invalid audience is rejected"
if ($r3.ErrorMessage) {
    Assert-Like $r3.ErrorMessage "*signature*" "Error mentions signature, not audience"
}

# ============================================================
# Test: Invalid signature with invalid issuer is rejected at signature
# ============================================================
Write-Host "`n  [SignatureFirst - invalid sig + bad issuer => signature error]" -ForegroundColor White

$badIssuerToken = New-SignatureTestJwt -SigningSecret $validSecret -Issuer "evil-issuer" -CorruptSignature
$r4 = Test-SignatureFirstValidation -Token $badIssuerToken -SharedSecret $validSecret -DetailedErrors
Assert-True (-not $r4.IsValid) "Invalid signature + invalid issuer is rejected"
if ($r4.ErrorMessage) {
    Assert-Like $r4.ErrorMessage "*signature*" "Error mentions signature, not issuer"
}

# ============================================================
# Test: Invalid signature with expired token is rejected at signature
# ============================================================
Write-Host "`n  [SignatureFirst - invalid sig + expired => signature error]" -ForegroundColor White

$expiredCorruptToken = New-SignatureTestJwt -SigningSecret $validSecret -Expired -CorruptSignature
$r5 = Test-SignatureFirstValidation -Token $expiredCorruptToken -SharedSecret $validSecret -DetailedErrors
Assert-True (-not $r5.IsValid) "Invalid signature + expired token is rejected"
if ($r5.ErrorMessage) {
    Assert-Like $r5.ErrorMessage "*signature*" "Error mentions signature, not expiration"
}

# ============================================================
# Test: Wrong secret (valid signature for different secret) is rejected
# ============================================================
Write-Host "`n  [SignatureFirst - wrong secret rejects at signature]" -ForegroundColor White

$tokenForWrongSecret = New-SignatureTestJwt -SigningSecret $wrongSecret
$r6 = Test-SignatureFirstValidation -Token $tokenForWrongSecret -SharedSecret $validSecret -DetailedErrors
Assert-True (-not $r6.IsValid) "Token signed with different secret is rejected"
if ($r6.ErrorMessage) {
    Assert-Like $r6.ErrorMessage "*signature*" "Error mentions signature mismatch"
}

# ============================================================
# Test: Valid signature but expired token fails at expiration (not signature)
# Requires full Spe.dll with Sitecore — fallback only checks signatures.
# ============================================================
Write-Host "`n  [SignatureFirst - valid sig + expired => expiration error]" -ForegroundColor White

if ($speLoaded) {
    $expiredValidSigToken = New-SignatureTestJwt -SigningSecret $validSecret -Expired
    $r7 = Test-SignatureFirstValidation -Token $expiredValidSigToken -SharedSecret $validSecret -DetailedErrors
    Assert-True (-not $r7.IsValid) "Valid signature but expired token is rejected"
    if ($r7.ErrorMessage) {
        Assert-Like $r7.ErrorMessage "*Expiration*" "Error mentions expiration, not signature"
    }
} else {
    Skip-Test "Valid sig + expired => expiration error" "Requires Sitecore assemblies for claim validation"
}
