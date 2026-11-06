# Integration tests for the LogSanitizer call-site sanitization contract.
#
# Threat model (#1444): attacker-controlled fields (kid from JWT header,
# username from query string, originalPath from zip entry, ex.Message from
# AccountIdentity) flow into structured audit messages of the form
# "[Remoting] action=X key1=val1 key2=val2 ...". An attacker who can
# inject "key=value" pairs into a single field can forge audit fields.
# LogSanitizer.SanitizeValue URL-encodes the field-delimiter chars
# (=, space, \r, \n, \t) at the call site.
#
# These tests drive the actual handler with attacker-flavored payloads and
# grep the live SPE.log to confirm:
#   1. The raw attacker substring (containing space + =) is NOT present.
#   2. The named field carries the URL-encoded form (%20 / %3D / %0A).
#
# Sites covered (3) by output capture: deprecatedAuth/credentialRejected
# (the query-string user path), authRejected (both the bad-name catch
# branch's user= and error= fields - one call site, two tainted inputs),
# and mediaUploaded (file= field from upload form). The remaining sites
# in the original source-regex sweep are unreachable via attacker traffic
# at runtime - bearerAuthFailed logs user=unknown when validation rejects
# before assigning username, bearerAuthSuccess requires a real Sitecore
# user, clientSignatureFailed/clientNotFound emit at Debug level which is
# filtered by the default log4net pipeline, undeterminedFilename emits at
# Warn (also filtered), and userUnauthorized requires custom role
# fixtures. Those keep a tighter source-regex backstop in
# tests/unit/SPE.LogSanitizer.Tests.ps1.

. "$PSScriptRoot\_helpers\Audit.ps1"

Write-Host "`n  [Audit-Field Sanitization (output capture)]" -ForegroundColor White

if (-not (Get-Command -Name docker -ErrorAction SilentlyContinue)) {
    Skip-Test "All audit-sanitization tests" "docker CLI not available"
    return
}

# Sanity check: Get-SpeAuditTail must return *something* or every test below
# fails for the wrong reason. Issue a benign request first so a freshly-
# recycled container has at least one log line, then probe. Report the line
# count only, never echo content.
try {
    $warmupSession = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost
    Invoke-RemoteScript -Session $warmupSession -ScriptBlock { 1 } | Out-Null
    Stop-ScriptSession -Session $warmupSession -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 300
} catch { }

$probeLines = @(Get-SpeAuditTail)
Write-Host ("  Audit log probe: {0} lines available" -f $probeLines.Count)
if ($probeLines.Count -eq 0) {
    Skip-Test "All audit-sanitization tests" "Audit log tail is empty (path mismatch or container unreachable)"
    return
}

# Each test embeds a unique correlator inside the attacker payload so the
# subsequent log scan can locate the line by greping for the slug. The slug
# is a 12-char hex string (no delimiter characters) so it survives
# sanitization unchanged and the surrounding URL-encoded chars then signal
# that the sanitizer ran.
function New-AuditTestSlug { ([guid]::NewGuid().ToString("N").Substring(0, 12)) }

$handler = New-Object System.Net.Http.HttpClientHandler
if ($PSVersionTable.PSVersion.Major -ge 6) {
    $handler.ServerCertificateCustomValidationCallback = [System.Net.Http.HttpClientHandler]::DangerousAcceptAnyServerCertificateValidator
}
$hc = New-Object System.Net.Http.HttpClient($handler)
$hc.Timeout = [TimeSpan]::FromSeconds(15)

# --------------------------------------------------------------------------
#  Site 1: query-string credentials (deprecatedAuth or credentialRejected)
#  AuthProcessor lines 40 + 53. Both branches log
#    user=<LogSanitizer.SanitizeValue(username)>
#  off the same query string. AllowQueryStringCredentials picks the action
#  name; either is acceptable - the assertion is on sanitization, not on
#  the policy decision.
# --------------------------------------------------------------------------
Write-Host "`n  [Site 1: credentials-from-query-string sanitizes user=]" -ForegroundColor White

$slug1 = New-AuditTestSlug
$rawAttack1 = "$slug1 action=loginSuccess"
$encUser = [uri]::EscapeDataString($rawAttack1)
$url1 = "$protocolHost/-/script/script/?user=$encUser&password=irrelevant"
$content1 = New-Object System.Net.Http.StringContent("", [System.Text.Encoding]::UTF8, "text/plain")
$null = $hc.PostAsync($url1, $content1).Result

Start-Sleep -Milliseconds 600

$audit = @(Get-SpeAuditTail)
$line1 = $audit |
    Where-Object { ($_ -match "action=(deprecatedAuth|credentialRejected)") -and ($_ -match [regex]::Escape($slug1)) } |
    Select-Object -First 1
Assert-NotNull $line1 "Site 1: audit line for query-string credentials carrying the test slug found"
if ($line1) {
    Assert-AuditFieldSanitized -Line $line1 -Field "user" `
        -RawAttackSubstring $rawAttack1 `
        -TestName "Site 1 (query-string credentials)"
}

# --------------------------------------------------------------------------
#  Site 2: authRejected (error variant - AccountIdentity exception)
#  AuthProcessor lines 459-461 + 450. Malformed username throws
#  ArgumentException; the catch sanitizes ex.Message into safeMessage AND
#  RejectAuthenticationMethod sanitizes user=. Both fields tested in one
#  request because they share the same call site.
# --------------------------------------------------------------------------
Write-Host "`n  [Site 2: authRejected (error variant) sanitizes user= and error=]" -ForegroundColor White

$slug2 = New-AuditTestSlug
# Username with embedded CRLF + literal '=': AccountIdentity ArgumentException
# throws on this, the message echoes the input, and both ex.Message (error=)
# and the username (user=) carry chars the sanitizer must encode.
$malformedName = "$slug2`r`naction=injected"
$basicAuth2 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes("${malformedName}:x"))

$req2 = New-Object System.Net.Http.HttpRequestMessage("POST", "$protocolHost/-/script/script/")
$null = $req2.Headers.TryAddWithoutValidation("Authorization", "Basic $basicAuth2")
$req2.Content = New-Object System.Net.Http.StringContent("", [System.Text.Encoding]::UTF8, "text/plain")
try { $null = $hc.SendAsync($req2).Result } catch { }

Start-Sleep -Milliseconds 600

$audit = @(Get-SpeAuditTail)
$line2 = $audit |
    Where-Object { ($_ -match "action=authRejected") -and ($_ -match [regex]::Escape($slug2)) } |
    Select-Object -First 1
Assert-NotNull $line2 "Site 2: audit line for authRejected carrying the test slug found"
if ($line2) {
    # The user= field must carry the slug + sanitized line breaks; the raw
    # `\r\n` bytes must not survive into the audit line.
    Assert-True ($line2 -notmatch "${slug2}`r") `
        "Site 2: raw CR after slug NOT present in audit line"
    Assert-True ($line2 -match "user=[^\s]*$([regex]::Escape($slug2))%(0A|0D)") `
        "Site 2: user= field carries the slug followed by URL-encoded CR/LF"
}

# --------------------------------------------------------------------------
#  Site 3: mediaUploaded sanitizes file= from upload form
#  MediaProcessor line 127. Form fileName is sanitized into the audit line.
#  Filenames cannot contain '=' on most filesystems; testing with a space
#  proves the sanitizer is invoked (space is the simplest delimiter char).
# --------------------------------------------------------------------------
Write-Host "`n  [Site 3: mediaUploaded sanitizes file= from upload form]" -ForegroundColor White

$slug3 = New-AuditTestSlug
$attackerFilename = "$slug3 report.png"

# Create a minimal local file with the attacker-flavored name and ship it
# through Send-RemoteItem - the established working media-upload path.
$tempDir = Join-Path $env:TEMP "SpeAuditSan_$slug3"
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
$tempFile = Join-Path $tempDir $attackerFilename
# 1x1 PNG, smallest legal payload.
[byte[]]$png = 0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A,0x00,0x00,0x00,0x0D,0x49,0x48,0x44,0x52,0x00,0x00,0x00,0x01,0x00,0x00,0x00,0x01,0x08,0x06,0x00,0x00,0x00,0x1F,0x15,0xC4,0x89,0x00,0x00,0x00,0x0D,0x49,0x44,0x41,0x54,0x78,0x9C,0x62,0x00,0x01,0x00,0x00,0x05,0x00,0x01,0x0D,0x0A,0x2D,0xB4,0x00,0x00,0x00,0x00,0x49,0x45,0x4E,0x44,0xAE,0x42,0x60,0x82
[System.IO.File]::WriteAllBytes($tempFile, $png)

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost
try {
    Get-Item -Path $tempFile |
        Send-RemoteItem -Session $session -RootPath Media -Destination "Images/_AuditSan_$slug3" -ErrorAction SilentlyContinue
} finally {
    Stop-ScriptSession -Session $session -ErrorAction SilentlyContinue
    Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}

Start-Sleep -Milliseconds 800

$audit = @(Get-SpeAuditTail)
$line3 = $audit |
    Where-Object { ($_ -match "action=mediaUploaded") -and ($_ -match [regex]::Escape($slug3)) } |
    Select-Object -First 1
Assert-NotNull $line3 "Site 3: mediaUploaded line carrying the test slug found"
if ($line3) {
    # file= is unquoted in the audit line; destination= is double-quoted.
    # Both flow attacker-influenced strings, so both go through SanitizeValue.
    $fileFieldMatch = [regex]::Match($line3, 'file=(\S+)')
    Assert-True $fileFieldMatch.Success "Site 3: file= field is parseable from audit line"
    if ($fileFieldMatch.Success) {
        $fileValue = $fileFieldMatch.Groups[1].Value
        Assert-True ($fileValue -notmatch "$([regex]::Escape($slug3)) report") `
            "Site 3: file= value does NOT contain raw 'slug<space>report'"
        Assert-True ($fileValue -match "$([regex]::Escape($slug3))%20report") `
            "Site 3: file= value carries %20 between slug and 'report'"
    }

    # destination= is double-quoted. Two positive signals confirm SanitizeValue
    # is wired in (a -notmatch ' ' assertion alone passes if Sitecore upstream-
    # normalizes spaces before mco.Destination is built):
    #   1. The Sitecore media library root "/sitecore/media library/" always
    #      contributes a space to the destination, so "media%20library" must
    #      appear in the sanitized value.
    #   2. The attacker filename "<slug> report.png" (sans extension) is also
    #      part of mco.Destination, so "<slug>%20report" must appear too.
    $destFieldMatch = [regex]::Match($line3, 'destination="([^"]*)"')
    Assert-True $destFieldMatch.Success "Site 3: destination= field is parseable from audit line"
    if ($destFieldMatch.Success) {
        $destValue = $destFieldMatch.Groups[1].Value
        Assert-True ($destValue -notmatch ' ') `
            "Site 3: destination= value contains no raw spaces (sanitized)"
        Assert-True ($destValue -match 'media%20library') `
            "Site 3: destination= value carries %20 in 'media library' (positive sanitization evidence)"
        Assert-True ($destValue -match "$([regex]::Escape($slug3))%20report") `
            "Site 3: destination= value carries %20 between attacker slug and 'report'"
        Assert-True ($destValue -match [regex]::Escape($slug3)) `
            "Site 3: destination= value carries the test slug"
    }
}

$hc.Dispose()
