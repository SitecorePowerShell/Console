# Unit tests for ConvertFrom-JwtPayload / ConvertFrom-JwtHeader.
#
# These cmdlets are diagnostic helpers: they base64url-decode and JSON-parse the
# header or payload segment of a JWT. They do NOT verify the signature, exp,
# iss, aud, or any other claim. Tests here confirm:
#   - Known-shape payloads decode to the expected claim values.
#   - Base64url padding is correct across all valid length-mod-4 cases (0, 2, 3).
#   - Malformed inputs (wrong segment count, empty, invalid base64, non-JSON)
#     throw clear errors.
#   - -Raw returns the decoded JSON string untouched.
#   - Pipeline input is supported.

# ============================================================
# Test helpers: mint known JWTs without crypto (signature is opaque)
# ============================================================
function Encode-Base64Url {
    param([byte[]]$Bytes)
    [Convert]::ToBase64String($Bytes).Split('=')[0].Replace('+', '-').Replace('/', '_')
}

function New-TestJwt {
    param(
        [hashtable]$Header,
        [object]$Payload,
        [string]$Signature = 'opaque-sig'
    )
    if (-not $Header) { $Header = [ordered]@{ alg = 'RS256'; typ = 'JWT' } }
    $headerJson = $Header | ConvertTo-Json -Compress
    $payloadJson = if ($Payload -is [string]) { $Payload } else { $Payload | ConvertTo-Json -Compress }
    $h = Encode-Base64Url ([System.Text.Encoding]::UTF8.GetBytes($headerJson))
    $p = Encode-Base64Url ([System.Text.Encoding]::UTF8.GetBytes($payloadJson))
    $s = Encode-Base64Url ([System.Text.Encoding]::UTF8.GetBytes($Signature))
    "$h.$p.$s"
}

# ============================================================
# ConvertFrom-JwtPayload: typical claims round-trip
# ============================================================
Write-Host "`n  [ConvertFrom-JwtPayload - typical claims]" -ForegroundColor White

$token = New-TestJwt -Payload ([ordered]@{
    iss = 'https://speid.dev.local'
    aud = 'spe-remoting'
    exp = 1777777777
    scope = 'spe.remoting'
})

$claims = ConvertFrom-JwtPayload -Token $token
Assert-Equal $claims.iss 'https://speid.dev.local' "iss claim decodes correctly"
Assert-Equal $claims.aud 'spe-remoting' "aud claim decodes correctly"
Assert-Equal $claims.exp 1777777777 "exp claim decodes correctly"
Assert-Equal $claims.scope 'spe.remoting' "scope claim decodes correctly"

# ============================================================
# ConvertFrom-JwtPayload: array aud (Auth0/IDS shape)
# ============================================================
Write-Host "`n  [ConvertFrom-JwtPayload - array aud]" -ForegroundColor White

$token = New-TestJwt -Payload ([ordered]@{
    iss = 'https://dev-tenant.us.auth0.com/'
    aud = @('https://spe-remoting', 'https://speid.dev.local/resources')
    azp = 'kGmQyirqWAoChuJZ8rkQmI5UWbhhX7wK'
    scope = 'spe.remoting'
})

$claims = ConvertFrom-JwtPayload -Token $token
Assert-Equal $claims.aud.Count 2 "array aud preserves element count"
Assert-Equal $claims.aud[0] 'https://spe-remoting' "array aud first element decodes"
Assert-Equal $claims.aud[1] 'https://speid.dev.local/resources' "array aud second element decodes"
Assert-Equal $claims.azp 'kGmQyirqWAoChuJZ8rkQmI5UWbhhX7wK' "azp claim decodes"

# ============================================================
# ConvertFrom-JwtPayload: pipeline input
# ============================================================
Write-Host "`n  [ConvertFrom-JwtPayload - pipeline input]" -ForegroundColor White

$token = New-TestJwt -Payload ([ordered]@{ iss = 'pipe-test' })
$claims = $token | ConvertFrom-JwtPayload
Assert-Equal $claims.iss 'pipe-test' "payload cmdlet accepts pipeline input"

# ============================================================
# ConvertFrom-JwtPayload: AccessToken alias
# ============================================================
Write-Host "`n  [ConvertFrom-JwtPayload - AccessToken alias]" -ForegroundColor White

$token = New-TestJwt -Payload ([ordered]@{ iss = 'alias-test' })
$claims = ConvertFrom-JwtPayload -AccessToken $token
Assert-Equal $claims.iss 'alias-test' "payload cmdlet accepts -AccessToken alias"

# ============================================================
# ConvertFrom-JwtPayload: -Raw returns JSON string
# ============================================================
Write-Host "`n  [ConvertFrom-JwtPayload - -Raw returns JSON]" -ForegroundColor White

$token = New-TestJwt -Payload ([ordered]@{ sub = 'abc123'; iat = 111 })
$raw = ConvertFrom-JwtPayload -Token $token -Raw
Assert-Type $raw 'String' "-Raw returns a string"
Assert-True ($raw -match '"sub"\s*:\s*"abc123"') "-Raw contains sub claim as JSON"
Assert-True ($raw -match '"iat"\s*:\s*111') "-Raw contains iat claim as JSON"

# ============================================================
# ConvertFrom-JwtPayload: base64url padding (len mod 4 in {0, 2, 3})
# ============================================================
Write-Host "`n  [ConvertFrom-JwtPayload - base64url padding]" -ForegroundColor White

# Construct payloads of specific JSON byte lengths to drive each padding branch.
# '{"x":"..."}'  -> 8 + len of "..." bytes. Tune filler length to force each mod.
foreach ($fillerLen in 0..8) {
    $filler = 'a' * $fillerLen
    $payloadObj = [ordered]@{ x = $filler }
    $token = New-TestJwt -Payload $payloadObj
    $decoded = ConvertFrom-JwtPayload -Token $token
    Assert-Equal $decoded.x $filler "filler length $fillerLen round-trips through base64url padding"
}

# ============================================================
# ConvertFrom-JwtPayload: malformed inputs
# ============================================================
Write-Host "`n  [ConvertFrom-JwtPayload - malformed inputs]" -ForegroundColor White

Assert-Throw { ConvertFrom-JwtPayload -Token '' } 'empty' `
    "empty token throws"
Assert-Throw { ConvertFrom-JwtPayload -Token '   ' } 'null or empty' `
    "whitespace-only token throws"
Assert-Throw { ConvertFrom-JwtPayload -Token 'only.two' } '3-segment' `
    "two-segment token throws"
Assert-Throw { ConvertFrom-JwtPayload -Token 'a.b.c.d' } '3-segment' `
    "four-segment token throws"
Assert-Throw { ConvertFrom-JwtPayload -Token 'aaa.!!!.ccc' } 'base64url' `
    "non-base64url payload throws"

# Valid base64url but not JSON: encode plain text 'not-json' as segment 2.
$garbage = Encode-Base64Url ([System.Text.Encoding]::UTF8.GetBytes('not-json'))
Assert-Throw { ConvertFrom-JwtPayload -Token "aaa.$garbage.ccc" } 'JSON' `
    "non-JSON payload throws"

# ============================================================
# ConvertFrom-JwtHeader: header claims round-trip
# ============================================================
Write-Host "`n  [ConvertFrom-JwtHeader - typical claims]" -ForegroundColor White

$token = New-TestJwt -Header ([ordered]@{
    alg = 'RS256'
    typ = 'JWT'
    kid = 'signing-key-1'
}) -Payload ([ordered]@{ iss = 'whatever' })

$header = ConvertFrom-JwtHeader -Token $token
Assert-Equal $header.alg 'RS256' "header alg decodes"
Assert-Equal $header.typ 'JWT' "header typ decodes"
Assert-Equal $header.kid 'signing-key-1' "header kid decodes"

# ============================================================
# ConvertFrom-JwtHeader: pipeline + -Raw
# ============================================================
Write-Host "`n  [ConvertFrom-JwtHeader - pipeline and -Raw]" -ForegroundColor White

$token = New-TestJwt -Header ([ordered]@{ alg = 'HS256'; typ = 'JWT' }) -Payload ([ordered]@{ iss = 'x' })
$header = $token | ConvertFrom-JwtHeader
Assert-Equal $header.alg 'HS256' "header cmdlet accepts pipeline input"

$raw = ConvertFrom-JwtHeader -Token $token -Raw
Assert-Type $raw 'String' "header -Raw returns a string"
Assert-True ($raw -match '"alg"\s*:\s*"HS256"') "header -Raw contains alg as JSON"

# ============================================================
# ConvertFrom-JwtHeader: malformed inputs
# ============================================================
Write-Host "`n  [ConvertFrom-JwtHeader - malformed inputs]" -ForegroundColor White

Assert-Throw { ConvertFrom-JwtHeader -Token '' } 'empty' `
    "header empty token throws"
Assert-Throw { ConvertFrom-JwtHeader -Token 'only.two' } '3-segment' `
    "header two-segment token throws"
Assert-Throw { ConvertFrom-JwtHeader -Token '!!!.bbb.ccc' } 'base64url' `
    "header non-base64url throws"

$garbage = Encode-Base64Url ([System.Text.Encoding]::UTF8.GetBytes('not-json'))
Assert-Throw { ConvertFrom-JwtHeader -Token "$garbage.bbb.ccc" } 'JSON' `
    "header non-JSON throws"
