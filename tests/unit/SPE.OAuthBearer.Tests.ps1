# Unit tests for OAuth bearer-token client plumbing.
# Requires TestRunner.ps1 to be dot-sourced first.

Write-Host "`n  [Expand-ScriptSession -AccessToken]" -ForegroundColor White

$bearerSession = [pscustomobject]@{
    Username              = ""
    Password              = ""
    SharedSecret          = ""
    AccessKeyId           = ""
    AccessToken           = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJzaXRlY29yZVxcYWRtaW4ifQ.sig"
    SessionId             = "sess-oauth-001"
    Credential            = $null
    UseDefaultCredentials = $false
    Connection            = @([pscustomobject]@{ BaseUri = "https://spe.dev.local" })
    PersistentSession     = $false
}

$expanded = Expand-ScriptSession -Session $bearerSession
Assert-Equal $expanded.AccessToken $bearerSession.AccessToken "AccessToken surfaced from session"

Write-Host "`n  [New-SpeHttpClient - AccessToken path]" -ForegroundColor White

$uri = [Uri]"https://spe.dev.local/-/script/script/"
$token = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJib3QifQ.sig"

$info = New-SpeHttpClient -AccessToken $token -Uri $uri
Assert-NotNull $info.Client "HttpClient created with AccessToken"
Assert-Null $info.Client.DefaultRequestHeaders.Authorization "Auth header NOT set on shared client (per-request only)"
Assert-Equal $info.AuthHeader.Scheme "Bearer" "Authorization scheme is Bearer"
Assert-Equal $info.AuthHeader.Parameter $token "Bearer parameter is the verbatim token (no minting)"

Write-Host "`n  [New-SpeHttpClient - AccessToken wins over SharedSecret]" -ForegroundColor White

$info2 = New-SpeHttpClient -AccessToken $token -SharedSecret "shared-secret-that-should-be-ignored-0123456789abcdef" -Username "admin" -Uri ([Uri]"https://spe.dev.local/test/")
Assert-Equal $info2.AuthHeader.Parameter $token "AccessToken takes precedence over SharedSecret"

Write-Host "`n  [New-SpeHttpClient - SharedSecret path preserved]" -ForegroundColor White

$hmacUri = [Uri]"https://spe.dev.local/other/"
$info3 = New-SpeHttpClient -SharedSecret "this-is-a-64-char-shared-secret-that-meets-the-rfc7518-minimum-len" -Username "admin" -Uri $hmacUri
Assert-Equal $info3.AuthHeader.Scheme "Bearer" "Shared-secret path still uses Bearer scheme"
Assert-NotEqual $info3.AuthHeader.Parameter $token "HMAC-minted JWT is not the externally-issued token"
Assert-True ($info3.AuthHeader.Parameter.Split('.').Count -eq 3) "HMAC-minted value is a three-part JWT"

Write-Host "`n  [New-SpeHttpClient - no credentials at all uses Basic]" -ForegroundColor White

$info4 = New-SpeHttpClient -Username "u" -Password "p" -Uri ([Uri]"https://spe.dev.local/basic/")
Assert-Equal $info4.AuthHeader.Scheme "Basic" "Fallback to Basic when no SharedSecret or AccessToken"
