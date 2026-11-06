# RemoteErrorVerbosity-ManualTest.ps1
#
# Manual verification for #1440 Spe.Remoting.DetailedErrors.
#
# Triggers a probe via raw HTTP POST against the remoting inline-script
# endpoint and shows the response envelope so the operator can confirm
# the gate behavior. Three probe modes:
#
#   default                     'throw' (terminating). Status 424.
#                               Exercises the handler-level catch sanitized
#                               path. errorId=Spe.Remoting.ScriptExecutionFailed.
#
#   -NonTerminating             Get-Item against a missing path. Status 200,
#                               script "succeeded" but the error stream
#                               carries one record. Exercises the
#                               SanitizedStructuredErrorScript constant -
#                               the in-script PS error formatter.
#
#   -Pass                       Probe that returns clean output, no errors.
#                               Status 200, errors=[]. Regression check that
#                               the gate doesn't break the success path.
#
# Prerequisites:
#   - Container is up (task up) and CM is responsive at https://spe.dev.local.
#   - task deploy has shipped the #1440 build of Spe.dll.
#   - One of the existing test API keys is loaded. We use the AuditLevel
#     test fixture's "Unrestricted-Standard" key by default (see
#     tests/integration/AuditLevel-ManualTest.ps1) - swap if your fixture
#     differs.
#
# Sanitized envelope (DetailedErrors=false, default):
#   - Each error has correlationId, errorCategory, fullyQualifiedErrorId,
#     message (a fixed template that quotes correlationId).
#   - Each error does NOT have scriptStackTrace, invocationInfo,
#     exceptionType, exceptionMessage, positionMessage, categoryReason,
#     categoryTargetName, categoryTargetType.
#   - The script prints the correlationId. Grep the CM log for it to
#     confirm the full PowerShell error record is logged server-side.
#
# Verbose envelope (DetailedErrors=true):
#   - All of the forbidden-when-sanitized fields return.
#
# To flip the setting, drop a config patch include like:
#   <setting name="Spe.Remoting.DetailedErrors"><patch:attribute name="value">true</patch:attribute></setting>
# Then `task deploy` (or recycle the app pool / restart the CM container)
# so WebServiceSettings re-reads at static-init.

[CmdletBinding(DefaultParameterSetName = 'Throw')]
param(
    [string]$BaseUrl     = "https://spe.dev.local",
    [string]$AccessKeyId = "spe_audit_unrestricted_standard",
    [string]$Secret      = "AuditTest-Unrestricted-Standard-Secret!LongEnough",

    [Parameter(ParameterSetName = 'NonTerminating')]
    [switch]$NonTerminating,

    [Parameter(ParameterSetName = 'Pass')]
    [switch]$Pass
)

Import-Module SPE -Force

# Pick the probe based on the chosen mode. Each one trips a different code
# path so the operator can verify all three sanitized-vs-verbose sites.
$mode = $PSCmdlet.ParameterSetName
switch ($mode) {
    'NonTerminating' {
        # Non-terminating PS error - script "succeeds", error stream has one
        # record formatted by the (Sanitized)StructuredErrorScript constant.
        $probeScript = "Get-Item -Path 'master:/sitecore/content/missing-spe-1440' -ErrorAction Continue"
        $expectErrors = $true
        Write-Host "[mode] NonTerminating: in-script PS error formatter" -ForegroundColor Cyan
    }
    'Pass' {
        # Success path. errors=[] confirms the gate doesn't disturb the
        # happy path.
        $probeScript = "'spe-1440-pass'"
        $expectErrors = $false
        Write-Host "[mode] Pass: success-path regression check" -ForegroundColor Cyan
    }
    default {
        # Terminating throw. Script aborts, handler-level catch path
        # produces the sanitized envelope with a synthetic errorId.
        $probeScript = "throw 'spe-1440-probe'"
        $expectErrors = $true
        Write-Host "[mode] Throw: handler-level catch sanitized path" -ForegroundColor Cyan
    }
}

# Bypass Invoke-RemoteScript so we can read the raw JSON envelope directly.
# Uses only the SPE module's public surface: New-Jwt mints the bearer token,
# vanilla HttpClient sends the request. (New-SpeHttpClient would do this in
# one call but it's intentionally not in FunctionsToExport.)
$uri = [Uri]$BaseUrl
$token = New-Jwt `
    -Algorithm   HS256 `
    -Issuer      'SPE Remoting' `
    -Audience    ($uri.GetLeftPart([System.UriPartial]::Authority)) `
    -KeyId       $AccessKeyId `
    -SecretKey   $Secret `
    -ValidForSeconds 30

$handler = New-Object System.Net.Http.HttpClientHandler
$handler.AutomaticDecompression = [System.Net.DecompressionMethods]::GZip -bor [System.Net.DecompressionMethods]::Deflate
$client = New-Object System.Net.Http.HttpClient $handler
$client.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token)

# Inline-script wire format: /-/script/script/?sessionId=...&outputFormat=Json&errorFormat=structured
# Body uses the SPE delimiter "<#sessionId#>" between the script block and
# the optional parameters block. We send no parameters.
$sessionId = [guid]::NewGuid().ToString()
$endpoint = "$($uri.AbsoluteUri.TrimEnd('/'))/-/script/script/?sessionId=$sessionId&rawOutput=False&outputFormat=Json&persistentSession=False&errorFormat=structured"
$body = "$probeScript<#$sessionId#>"

Write-Host "POST $endpoint" -ForegroundColor Cyan

$content = New-Object System.Net.Http.StringContent($body, [System.Text.Encoding]::UTF8, "text/plain")
$response = $client.PostAsync($endpoint, $content).GetAwaiter().GetResult()
$rawBody = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()

Write-Host "`nStatus : $($response.StatusCode)" -ForegroundColor Cyan
Write-Host "Body   :" -ForegroundColor Cyan
Write-Host $rawBody -ForegroundColor Yellow

$parsed = $rawBody | ConvertFrom-Json

# Pass mode: assert the success-path shape and exit early.
if (-not $expectErrors) {
    $errCount = if ($parsed.errors) { $parsed.errors.Count } else { 0 }
    Write-Host "`n--- Pass-mode check ---" -ForegroundColor Cyan
    if ($errCount -eq 0 -and $parsed.output -and $parsed.output.Count -gt 0) {
        Write-Host "  PASS: errors=[] and output carries the script result" -ForegroundColor Green
    } elseif ($errCount -eq 0) {
        Write-Host "  PASS-ish: errors=[] but output is empty (still a success envelope)" -ForegroundColor Yellow
    } else {
        Write-Host "  FAIL: expected zero errors, got $errCount" -ForegroundColor Red
    }
    return
}

if (-not $parsed.errors -or $parsed.errors.Count -eq 0) {
    Write-Host "`nNo errors in response - probe did not produce one as expected for mode '$mode'." -ForegroundColor Red
    return
}

$err = $parsed.errors[0]
$expectedKeys = @('correlationId', 'errorCategory', 'fullyQualifiedErrorId', 'message')
$forbiddenSanitized = @('scriptStackTrace', 'invocationInfo', 'exceptionType', 'exceptionMessage', 'positionMessage', 'categoryReason', 'categoryTargetName', 'categoryTargetType')

Write-Host "`n--- Field check ---" -ForegroundColor Cyan
foreach ($k in $expectedKeys) {
    $present = [bool]$err.PSObject.Properties[$k]
    $color = if ($present) { 'Green' } else { 'Red' }
    Write-Host ("  {0,-25} {1}" -f $k, $(if ($present) { 'present' } else { 'MISSING' })) -ForegroundColor $color
}
Write-Host ""
foreach ($k in $forbiddenSanitized) {
    $present = [bool]$err.PSObject.Properties[$k]
    $color = if (-not $present) { 'Green' } else { 'Yellow' }
    $note = if ($present) { 'present (verbose mode? OK if DetailedErrors=true)' } else { 'absent (sanitized form, OK)' }
    Write-Host ("  {0,-25} {1}" -f $k, $note) -ForegroundColor $color
}

if ($err.correlationId) {
    Write-Host "`ncorrelationId : $($err.correlationId)" -ForegroundColor Cyan
    Write-Host "  Grep the CM log for this id to confirm the full PowerShell error is recorded server-side. Example:" -ForegroundColor DarkGray
    Write-Host "  docker exec spe-cm powershell -NoProfile -Command `"Get-ChildItem C:\inetpub\wwwroot\sc\App_Data\logs\SPE.log* | Sort-Object LastWriteTime -Desc | Select-Object -First 1 | Get-Content | Select-String '$($err.correlationId)'`"" -ForegroundColor DarkGray
}
