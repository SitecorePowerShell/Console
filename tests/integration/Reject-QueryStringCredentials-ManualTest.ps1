# Reject-QueryStringCredentials-ManualTest.ps1
#
# Manual verification for #1454 Spe.Remoting.AllowQueryStringCredentials.
#
# Sends raw HTTP requests against the remoting inline-script endpoint to
# confirm the gate behavior:
#
#   default                     ?user=&password= against a default install
#                               (AllowQueryStringCredentials=true). Expect
#                               the gate to let the request through (auth
#                               will still fail downstream because creds
#                               are bogus, that is expected) and an
#                               action=deprecatedAuth warning to appear in
#                               the SPE log. Pre-9.0 behavior preserved.
#
#   -RejectMode                 Same query-string request after the operator
#                               has flipped Spe.Remoting.AllowQueryStringCredentials
#                               to false and recycled the app pool. Expect
#                               401, and an action=credentialRejected
#                               reason=queryStringCredentials line in the
#                               SPE log. The 401 ReasonPhrase intentionally
#                               matches downstream RejectAuthenticationMethod
#                               so the gate's existence is not visible to
#                               unauthenticated callers.
#
#   -BearerMode                 Authorization: Bearer JWT, no query-string
#                               creds. Regression check: works in both modes.
#
# Prerequisites:
#   - Container is up (task up) and CM is responsive at https://spe.dev.local.
#   - task deploy has shipped the #1454 build of Spe.dll.
#   - One of the existing test API keys is loaded for the Bearer probe. We
#     use the AuditLevel test fixture's "Unrestricted-Standard" key by default.
#
# To flip the setting between runs, drop a config patch include like:
#   <setting name="Spe.Remoting.AllowQueryStringCredentials"><patch:attribute name="value">false</patch:attribute></setting>
# Then `task deploy` (or recycle the app pool / restart the CM container) so
# WebServiceSettings re-reads at static-init.

[CmdletBinding(DefaultParameterSetName = 'Default')]
param(
    [string]$BaseUrl     = "https://spe.dev.local",
    [string]$AccessKeyId = "spe_audit_unrestricted_standard",
    [string]$Secret      = "AuditTest-Unrestricted-Standard-Secret!LongEnough",

    [Parameter(ParameterSetName = 'RejectMode')]
    [switch]$RejectMode,

    [Parameter(ParameterSetName = 'BearerMode')]
    [switch]$BearerMode
)

Import-Module SPE -Force

$uri = [Uri]$BaseUrl
$sessionId = [guid]::NewGuid().ToString()
$probeScript = "'spe-1454-probe'"
$body = "$probeScript<#$sessionId#>"

$handler = New-Object System.Net.Http.HttpClientHandler
$handler.AutomaticDecompression = [System.Net.DecompressionMethods]::GZip -bor [System.Net.DecompressionMethods]::Deflate
$client = New-Object System.Net.Http.HttpClient $handler

$mode = $PSCmdlet.ParameterSetName
switch ($mode) {
    'BearerMode' {
        Write-Host "[mode] Bearer: Authorization header, no query-string creds (regression check)" -ForegroundColor Cyan
        $token = New-Jwt `
            -Algorithm   HS256 `
            -Issuer      'SPE Remoting' `
            -Audience    ($uri.GetLeftPart([System.UriPartial]::Authority)) `
            -KeyId       $AccessKeyId `
            -SecretKey   $Secret `
            -ValidForSeconds 30
        $client.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token)
        $endpoint = "$($uri.AbsoluteUri.TrimEnd('/'))/-/script/script/?sessionId=$sessionId&rawOutput=False&outputFormat=Json&persistentSession=False&errorFormat=structured"
        $expectStatus = 200
    }
    'RejectMode' {
        Write-Host "[mode] Reject: query-string creds, gate opt-out (Spe.Remoting.AllowQueryStringCredentials must be false)" -ForegroundColor Cyan
        $endpoint = "$($uri.AbsoluteUri.TrimEnd('/'))/-/script/script/?sessionId=$sessionId&rawOutput=False&outputFormat=Json&persistentSession=False&errorFormat=structured&user=admin&password=spe-1454-bogus"
        $expectStatus = '401 (ReasonPhrase indistinguishable from downstream auth-fail by design); SPE log shows action=credentialRejected reason=queryStringCredentials'
    }
    default {
        Write-Host "[mode] Default: query-string creds, default config (Spe.Remoting.AllowQueryStringCredentials=true)" -ForegroundColor Cyan
        $endpoint = "$($uri.AbsoluteUri.TrimEnd('/'))/-/script/script/?sessionId=$sessionId&rawOutput=False&outputFormat=Json&persistentSession=False&errorFormat=structured&user=admin&password=spe-1454-bogus"
        $expectStatus = '401 (downstream auth-fail because creds are bogus); SPE log shows action=deprecatedAuth reason=queryStringCredentials'
    }
}

Write-Host "POST $endpoint" -ForegroundColor Cyan

$content = New-Object System.Net.Http.StringContent($body, [System.Text.Encoding]::UTF8, "text/plain")
$response = $client.PostAsync($endpoint, $content).GetAwaiter().GetResult()
$rawBody = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()

Write-Host "`nStatus      : $([int]$response.StatusCode) $($response.StatusCode)" -ForegroundColor Cyan
Write-Host "Description : $($response.ReasonPhrase)" -ForegroundColor Cyan
Write-Host "Body        :" -ForegroundColor Cyan
if ($rawBody) { Write-Host $rawBody -ForegroundColor Yellow } else { Write-Host '(empty)' -ForegroundColor DarkGray }

Write-Host "`n--- Expected ---" -ForegroundColor Cyan
Write-Host "  $expectStatus" -ForegroundColor DarkGray

Write-Host "`n--- Result ---" -ForegroundColor Cyan
switch ($mode) {
    'BearerMode' {
        if ([int]$response.StatusCode -eq 200) {
            Write-Host "  PASS: header-based auth works regardless of the gate" -ForegroundColor Green
        } else {
            Write-Host "  FAIL: bearer probe returned $([int]$response.StatusCode); should be 200" -ForegroundColor Red
        }
    }
    'RejectMode' {
        # ReasonPhrase intentionally matches downstream RejectAuthenticationMethod so the
        # gate is invisible to unauthenticated callers. The signal is the SPE log line
        # action=credentialRejected reason=queryStringCredentials, which the operator
        # must check separately.
        if ([int]$response.StatusCode -eq 401) {
            Write-Host "  PASS (response side): 401 returned (ReasonPhrase: $($response.ReasonPhrase))" -ForegroundColor Green
            Write-Host "  Confirm in SPE log: action=credentialRejected reason=queryStringCredentials (gate). If you see action=authRejected or action=deprecatedAuth instead, the setting did not flip." -ForegroundColor DarkGray
        } else {
            Write-Host "  FAIL: expected 401, got $([int]$response.StatusCode) $($response.ReasonPhrase)" -ForegroundColor Red
        }
    }
    default {
        # Both gate-reject and downstream-auth-fail produce 401 with the same ReasonPhrase
        # by design, so this assertion can only flag obvious failures (non-401). Operator
        # must read the SPE log to confirm action=deprecatedAuth (gate let it through, PASS)
        # vs action=credentialRejected (gate rejected, FAIL because default should be true).
        if ([int]$response.StatusCode -eq 401) {
            Write-Host "  PASS (response side): 401 returned (downstream auth-fail OR gate-reject - response is identical by design)" -ForegroundColor Green
            Write-Host "  Confirm in SPE log: action=deprecatedAuth reason=queryStringCredentials (gate let it through, default behavior preserved). If you see action=credentialRejected instead, AllowQueryStringCredentials is not at its default of true." -ForegroundColor DarkGray
        } else {
            Write-Host "  FAIL: expected 401, got $([int]$response.StatusCode) $($response.ReasonPhrase)" -ForegroundColor Red
        }
    }
}
