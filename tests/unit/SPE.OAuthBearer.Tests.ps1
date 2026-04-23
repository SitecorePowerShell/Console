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
    _HttpClients          = @{}
}

$expanded = Expand-ScriptSession -Session $bearerSession
Assert-Equal $expanded.AccessToken $bearerSession.AccessToken "AccessToken surfaced from session"

Write-Host "`n  [New-SpeHttpClient - AccessToken path]" -ForegroundColor White

$uri = [Uri]"https://spe.dev.local/-/script/script/"
$cache = @{}
$token = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJib3QifQ.sig"

$client = New-SpeHttpClient -AccessToken $token -Uri $uri -Cache $cache
Assert-NotNull $client "HttpClient created with AccessToken"
Assert-Equal $client.DefaultRequestHeaders.Authorization.Scheme "Bearer" "Authorization scheme is Bearer"
Assert-Equal $client.DefaultRequestHeaders.Authorization.Parameter $token "Bearer parameter is the verbatim token (no minting)"

Write-Host "`n  [New-SpeHttpClient - AccessToken wins over SharedSecret]" -ForegroundColor White

$cache2 = @{}
$client2 = New-SpeHttpClient -AccessToken $token -SharedSecret "shared-secret-that-should-be-ignored-0123456789abcdef" -Username "admin" -Uri ([Uri]"https://spe.dev.local/test/") -Cache $cache2
Assert-Equal $client2.DefaultRequestHeaders.Authorization.Parameter $token "AccessToken takes precedence over SharedSecret"

Write-Host "`n  [New-SpeHttpClient - SharedSecret path preserved]" -ForegroundColor White

$cache3 = @{}
$hmacUri = [Uri]"https://spe.dev.local/other/"
$client3 = New-SpeHttpClient -SharedSecret "this-is-a-64-char-shared-secret-that-meets-the-rfc7518-minimum-len" -Username "admin" -Uri $hmacUri -Cache $cache3
Assert-Equal $client3.DefaultRequestHeaders.Authorization.Scheme "Bearer" "Shared-secret path still uses Bearer scheme"
Assert-NotEqual $client3.DefaultRequestHeaders.Authorization.Parameter $token "HMAC-minted JWT is not the externally-issued token"
Assert-True ($client3.DefaultRequestHeaders.Authorization.Parameter.Split('.').Count -eq 3) "HMAC-minted value is a three-part JWT"

Write-Host "`n  [New-SpeHttpClient - no credentials at all uses Basic]" -ForegroundColor White

$cache4 = @{}
$client4 = New-SpeHttpClient -Username "u" -Password "p" -Uri ([Uri]"https://spe.dev.local/basic/") -Cache $cache4
Assert-Equal $client4.DefaultRequestHeaders.Authorization.Scheme "Basic" "Fallback to Basic when no SharedSecret or AccessToken"
