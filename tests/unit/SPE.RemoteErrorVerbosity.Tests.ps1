# Source-regression guards for Spe.Remoting.DetailedErrors (#1440).
#
# Threat model: an authenticated remoting caller learns more about the server
# than they need from error responses - .NET stack traces, file paths,
# internal type names, blocked-command names, exception-message text from
# server-side IO. Default-off "minimal" payload exposes only the request
# correlation id (rid) plus the PowerShell ErrorCategory and
# FullyQualifiedErrorId, both of which the caller already produced via their
# own script. Operators flip the setting on for dev/troubleshooting.
#
# These tests are source-regression guards: the gate sites live deep inside
# RemoteScriptCall's HttpHandler pipeline and can't be exercised from a unit
# test without a live Sitecore container. End-to-end coverage belongs in
# tests/integration once the container can run with the flag flipped.

Write-Host "`n  [Spe.Remoting.DetailedErrors source guards]" -ForegroundColor White

$repoRoot = Resolve-Path "$PSScriptRoot\..\.."
$handler  = Join-Path $repoRoot "src\Spe\sitecore modules\PowerShell\Services\RemoteScriptCall.ashx.cs"
$settings = Join-Path $repoRoot "src\Spe\Core\Settings\Authorization\WebServiceSettings.cs"
$config   = Join-Path $repoRoot "src\Spe\App_Config\Include\Spe\Spe.config"

if (-not (Test-Path $handler) -or -not (Test-Path $settings) -or -not (Test-Path $config)) {
    Skip-Test "All DetailedErrors source guards" "Source files not found"
    return
}

$handlerSrc  = Get-Content $handler  -Raw
$settingsSrc = Get-Content $settings -Raw
$configSrc   = Get-Content $config   -Raw

# Spe.config: setting declared with default false
Assert-True ($configSrc -match 'name="Spe\.Remoting\.DetailedErrors"\s+value="false"') `
    "Spe.config declares Spe.Remoting.DetailedErrors=false"

# WebServiceSettings: bool property + reader from the new setting
Assert-True ($settingsSrc -match 'public static bool DetailedErrors') `
    "WebServiceSettings exposes static bool DetailedErrors"
Assert-True ($settingsSrc -match 'GetBoolSetting\("Spe\.Remoting\.DetailedErrors",\s*false\)') `
    "WebServiceSettings reads Spe.Remoting.DetailedErrors with default false"

# Sanitized structured-error script exists alongside the verbose one
Assert-True ($handlerSrc -match 'SanitizedStructuredErrorScript') `
    "Handler declares SanitizedStructuredErrorScript constant"

# Locate the SanitizedStructuredErrorScript literal body and verify high-leak
# fields are absent. Match a chain of "..." string concatenations terminated
# by a semicolon, which is how the sibling StructuredErrorScript is written.
$sanitizedMatch = [regex]::Match(
    $handlerSrc,
    'SanitizedStructuredErrorScript\s*=\s*(?<body>("(?:[^"\\]|\\.)*"\s*\+?\s*)+);',
    [System.Text.RegularExpressions.RegexOptions]::Singleline)

Assert-True $sanitizedMatch.Success "SanitizedStructuredErrorScript literal is parseable"

if ($sanitizedMatch.Success) {
    $body = $sanitizedMatch.Groups['body'].Value

    Assert-True ($body -notmatch 'scriptStackTrace') "Sanitized script omits scriptStackTrace"
    Assert-True ($body -notmatch 'invocationInfo')   "Sanitized script omits invocationInfo"
    Assert-True ($body -notmatch 'exceptionType')    "Sanitized script omits exceptionType"
    Assert-True ($body -notmatch 'exceptionMessage') "Sanitized script omits exceptionMessage"
    Assert-True ($body -notmatch 'positionMessage')  "Sanitized script omits positionMessage"

    Assert-True ($body -match 'correlationId')                       "Sanitized script emits correlationId"
    Assert-True ($body -match 'errorCategory')                       "Sanitized script emits errorCategory"
    Assert-True ($body -match 'fullyQualifiedErrorId|errorId')       "Sanitized script emits errorId / FullyQualifiedErrorId"
}

# Line 1922 site picks the variant by flag (and falls back to FlatErrorScript
# for non-structured callers - that branch is unchanged).
Assert-True ($handlerSrc -match 'WebServiceSettings\.DetailedErrors\s*\?\s*StructuredErrorScript\s*:\s*SanitizedStructuredErrorScript') `
    "ProcessScript picks StructuredErrorScript vs SanitizedStructuredErrorScript by WebServiceSettings.DetailedErrors"

# SetErrorResponse: structured branch consults the flag before exposing
# exception fields. Hard-anchor on the method signature to scope the search.
$setErrorResponseSegment = [regex]::Match(
    $handlerSrc,
    'SetErrorResponse\(HttpContext context.*?^\s{8}\}',
    [System.Text.RegularExpressions.RegexOptions]::Singleline -bor `
    [System.Text.RegularExpressions.RegexOptions]::Multiline).Value

Assert-True ([bool]$setErrorResponseSegment) "Located SetErrorResponse method"
if ($setErrorResponseSegment) {
    Assert-True ($setErrorResponseSegment -match 'WebServiceSettings\.DetailedErrors') `
        "SetErrorResponse consults WebServiceSettings.DetailedErrors before exposing exception details"
}

# Handler-level catch (the structured branch around the script-execution
# Try/Catch): the new sanitized form must include a correlationId field.
Assert-True ($handlerSrc -match '\["correlationId"\]') `
    "Handler structured-error branches produce a correlationId field"

# L7: policy-block headers + 403 body gated on the flag
$policyBlockedSegment = [regex]::Match(
    $handlerSrc,
    'X-SPE-Restriction"\]\s*=\s*"policy-blocked".*?return;',
    [System.Text.RegularExpressions.RegexOptions]::Singleline).Value

Assert-True ([bool]$policyBlockedSegment) "Located policy-blocked branch"
if ($policyBlockedSegment) {
    Assert-True ($policyBlockedSegment -match 'WebServiceSettings\.DetailedErrors') `
        "Policy-block branch gates X-SPE-BlockedCommand / X-SPE-Policy on WebServiceSettings.DetailedErrors"
}

# L8: TransmitFile IOException no longer leaks ex.Message unconditionally.
# Bound the scan to ~500 chars after the catch keyword - the catch body has
# string interpolations like ${rid={GetRequestId()}} that contain literal }
# chars, so a brace-balanced match isn't worth writing in regex. Proximity
# is enough.
$ioCatchMatch = [regex]::Match(
    $handlerSrc,
    'catch\s*\(IOException[\s\S]{0,1000}?WebServiceSettings\.DetailedErrors',
    [System.Text.RegularExpressions.RegexOptions]::Singleline)

Assert-True $ioCatchMatch.Success `
    "IOException catch consults WebServiceSettings.DetailedErrors before assigning ex.Message"
