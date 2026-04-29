# Unit tests for LogSanitizer + PowerShellLog.ToJson duplicate-key hardening (#1444).
#
# Threat model: attacker-controlled JWT claims (iss, aud, sub) and request
# parameters (username, kid, archive entry filenames) flow into structured
# log messages of the form "[Category] action=X key1=val1 key2=val2 ...".
# Without sanitization, an attacker can inject "key=value" pairs into a
# single field by submitting a value containing spaces and equals signs.
# A SIEM that parses those messages as key/value pairs would see forged
# fields and could be tricked into recording a successful auth event.
#
# Two layers of defense:
#   1. LogSanitizer.SanitizeValue - URL-encodes the field-delimiter chars
#      (=, space, \r, \n, \t) at the call site.
#   2. PowerShellLog.ToJson - on duplicate keys, keeps the first occurrence
#      so even an unsanitized injection cannot overwrite the legitimate
#      action= or user= field.
#
# Layer 2 is the safety net for any future call site that forgets to
# sanitize; it must remain in place as a regression guard.

Write-Host "`n  [LogSanitizer + PowerShellLog.ToJson]" -ForegroundColor White

$spePath = "$PSScriptRoot\..\..\src\Spe\bin\Debug\Spe.dll"

if (-not (Test-Path $spePath)) {
    Skip-Test "All LogSanitizer/ToJson tests" "Build artifacts not found - run 'task build' first"
    return
}

try { Add-Type -Path $spePath -ErrorAction SilentlyContinue } catch { }

# Spe.dll references Newtonsoft.Json 13 but no Newtonsoft.Json.dll is
# checked in; the package is normally satisfied by the Sitecore CM bin
# folder at runtime. For unit tests, pre-load it from the NuGet cache so
# PowerShellLog.ToJson can return a JObject without an assembly-load failure.
$newtonsoftDll = "$env:USERPROFILE\.nuget\packages\newtonsoft.json\13.0.1\lib\net45\Newtonsoft.Json.dll"
$haveNewtonsoft = Test-Path $newtonsoftDll
if ($haveNewtonsoft) {
    try { Add-Type -Path $newtonsoftDll -ErrorAction SilentlyContinue } catch { }
}

# PowerShellLog is public, LogSanitizer is internal - reach the assembly
# through the public type, then look up both via reflection.
$asm = [Spe.Core.Diagnostics.PowerShellLog].Assembly
$sanitizerType = $asm.GetType("Spe.Core.Diagnostics.LogSanitizer")
$logType       = $asm.GetType("Spe.Core.Diagnostics.PowerShellLog")

if (-not $sanitizerType -or -not $logType) {
    Skip-Test "All LogSanitizer/ToJson tests" "Spe.Core.Diagnostics.LogSanitizer or PowerShellLog type not found"
    return
}

$sanitizeValue = $sanitizerType.GetMethod("SanitizeValue",
    [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Static)
$redactUrl = $sanitizerType.GetMethod("RedactUrl",
    [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Static)
$toJson = $logType.GetMethod("ToJson",
    [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Static)

if (-not $sanitizeValue -or -not $redactUrl -or -not $toJson) {
    Skip-Test "All LogSanitizer/ToJson tests" "Reflection lookup failed - LogSanitizer or PowerShellLog.ToJson missing"
    return
}

function Sanitize {
    param([string]$Value)
    return $sanitizeValue.Invoke($null, @($Value))
}

function Redact {
    param([Uri]$Url)
    return $redactUrl.Invoke($null, @($Url))
}

function ToJson {
    param([string]$Message)
    return $toJson.Invoke($null, @($Message))
}

# ============================================================
# LogSanitizer.SanitizeValue
# ============================================================
Write-Host "`n  [SanitizeValue: encodes field-delimiter chars]" -ForegroundColor White

Assert-Equal (Sanitize $null)  "(empty)" "null  -> (empty) sentinel"
Assert-Equal (Sanitize "")     "(empty)" "empty -> (empty) sentinel"

# Fast path: no dangerous chars, returns unchanged
Assert-Equal (Sanitize "user@example.com") "user@example.com" "clean value passes through unchanged"
Assert-Equal (Sanitize "kid-abc123")        "kid-abc123"        "alphanumeric-with-dash unchanged"

# Slow path: each delimiter char is URL-encoded
Assert-Equal (Sanitize "a=b")     "a%3Db"     "= is encoded to %3D"
Assert-Equal (Sanitize "a b")     "a%20b"     "space is encoded to %20"
Assert-Equal (Sanitize "a`nb")    "a%0Ab"     "LF is encoded to %0A"
Assert-Equal (Sanitize "a`rb")    "a%0Db"     "CR is encoded to %0D"
Assert-Equal (Sanitize "a`tb")    "a%09b"     "tab is encoded to %09"

# The exact attack from #1444: attacker submits an iss claim that injects
# a fake action= key. After sanitization, the entire payload becomes one
# opaque value with no ToJson-visible delimiters.
$injection = "legitimate action=loginSuccess user=admin status=authenticated"
$sanitized = Sanitize $injection
Assert-Equal $sanitized "legitimate%20action%3DloginSuccess%20user%3Dadmin%20status%3Dauthenticated" `
    "issuer-claim injection: every space and = is encoded"

# ============================================================
# LogSanitizer.RedactUrl
# ============================================================
Write-Host "`n  [RedactUrl: redacts sensitive query params]" -ForegroundColor White

Assert-Equal (Redact $null) "(null)" "null Uri -> (null) sentinel"

$plain = [Uri]"https://spe.dev.local/api/remoting"
Assert-Equal (Redact $plain) "https://spe.dev.local/api/remoting" "no query -> unchanged"

$benign = [Uri]"https://spe.dev.local/api/remoting?apiVersion=2"
Assert-Equal (Redact $benign) "https://spe.dev.local/api/remoting?apiVersion=2" "non-sensitive query -> unchanged"

# Each sensitive key name is replaced with ***REDACTED***
$pwd = [Uri]"https://spe.dev.local/api/remoting?user=admin&password=hunter2"
$redacted = Redact $pwd
Assert-True ($redacted -match "password=") "password key still present"
Assert-True ($redacted -notmatch "hunter2") "password value redacted"
Assert-True ($redacted -match "REDACTED") "REDACTED marker present"
Assert-True ($redacted -match "user=admin") "non-sensitive 'user' param NOT redacted (only password/username/credential/secret/token)"

# 'username' IS in the redact list per LogSanitizer.IsSensitiveParam
$un = [Uri]"https://spe.dev.local/api/remoting?username=admin"
$redactedUn = Redact $un
Assert-True ($redactedUn -notmatch "admin") "username param value redacted"

$tok = [Uri]"https://spe.dev.local/api/remoting?token=eyJhbGc"
Assert-True ((Redact $tok) -notmatch "eyJhbGc") "token param value redacted"

$sec = [Uri]"https://spe.dev.local/api/remoting?secret=s3cr3t&apiVersion=2"
$redactedSec = Redact $sec
Assert-True ($redactedSec -notmatch "s3cr3t") "secret param value redacted"
Assert-True ($redactedSec -match "apiVersion=2")  "non-sensitive params preserved alongside redaction"

# ============================================================
# PowerShellLog.ToJson - duplicate-key hardening (regression guard)
# ============================================================
# PowerShellLog has a Sitecore-dependent static constructor (LogManager
# +Configuration.Factory +Settings) so invoking ToJson via reflection here
# triggers a TypeInitializationException outside a Sitecore runtime.
# Probe with a no-op call first; skip cleanly if the type initializer
# fails. Source-regression guards below cover the duplicate-key contract.
$canInvokeToJson = $false
if ($haveNewtonsoft) {
    try {
        $null = $toJson.Invoke($null, @(""))
        $canInvokeToJson = $true
    } catch {
        $canInvokeToJson = $false
    }
}

if (-not $canInvokeToJson) {
    Skip-Test "ToJson runtime-invocation tests" "PowerShellLog static initializer needs Sitecore runtime; covered by source-regression guards below"
} else {

Write-Host "`n  [ToJson: duplicate keys keep first occurrence]" -ForegroundColor White

# Sanitized happy path: legitimate key/value pairs round-trip into JSON
$j = ToJson "[Auth] action=tokenValidation issuer=legit audience=spe"
Assert-Equal $j["type"].ToString()     "Auth"             "category extracted from [Auth]"
Assert-Equal $j["action"].ToString()   "tokenValidation"  "first action= captured"
Assert-Equal $j["issuer"].ToString()   "legit"            "issuer captured"
Assert-Equal $j["audience"].ToString() "spe"              "audience captured"

# REGRESSION GUARD: this is the #1444 attack scenario applied to the JSON
# layer. An attacker-controlled iss claim that contains "action=loginSuccess"
# must NOT overwrite the legitimate action= field. ToJson keeps the first
# occurrence of each key, so the injected pair is dropped.
$attack = "[Auth] action=tokenValidation issuer=evil action=loginSuccess user=admin"
$j = ToJson $attack
Assert-Equal $j["action"].ToString() "tokenValidation" `
    "REGRESSION GUARD: injected action= cannot overwrite the first occurrence"
Assert-Equal $j["issuer"].ToString() "evil" `
    "issuer still captures the attacker value (sanitization is the call-site defense; ToJson is the safety net)"
Assert-Equal $j["user"].ToString() "admin" `
    "extra injected fields land as new keys (NOT what we want, but the attacker cannot forge action/type)"

# Same attack pattern, this time with a sanitized issuer. After sanitization
# the entire malicious payload becomes one opaque %20-encoded blob that the
# KeyValuePattern regex captures as a single value.
$sanitizedAttack = "[Auth] action=tokenValidation issuer=$(Sanitize 'evil action=loginSuccess user=admin')"
$j = ToJson $sanitizedAttack
Assert-Equal $j["action"].ToString() "tokenValidation" "sanitized: legitimate action preserved"
Assert-Null  $j["user"] "sanitized: injected user= field is NOT extracted (encoded value has no spaces)"

# Non-categorized message: no [Category] prefix means the whole thing
# becomes a single 'message' field, with no key=value extraction.
$j = ToJson "this is a plain log line"
Assert-Equal $j["message"].ToString() "this is a plain log line" "uncategorized -> single 'message' field"
Assert-Null  $j["action"] "uncategorized -> no key=value parsing"

}  # end of ToJson runtime-invocation block

# ============================================================
# Source-regression guards (always run, do not need a Sitecore runtime)
# ============================================================
# These read the C# source directly so the duplicate-key contract and the
# call-site sanitization sweep have a tripwire even when the runtime test
# above is skipped (no Sitecore available, no Newtonsoft.Json available).
Write-Host "`n  [Source guards: ToJson contract + RemoteScriptCall sanitization sweep]" -ForegroundColor White

$repoRoot           = "$PSScriptRoot\..\.."
$powerShellLogPath  = "$repoRoot\src\Spe\Core\Diagnostics\PowerShellLog.cs"
$remoteScriptCall   = "$repoRoot\src\Spe\sitecore modules\PowerShell\Services\RemoteScriptCall.ashx.cs"

if (-not (Test-Path $powerShellLogPath) -or -not (Test-Path $remoteScriptCall)) {
    Skip-Test "Source-regression guards" "Source files not found - run from a checked-out tree"
    return
}

$logSrc      = Get-Content -Raw $powerShellLogPath
$callsiteSrc = Get-Content -Raw $remoteScriptCall

# Duplicate-key hardening: ToJson must check for an existing key before
# assigning, otherwise injected key=value pairs overwrite legitimate ones.
Assert-True ($logSrc -match 'if\s*\(\s*json\[\s*key\s*\]\s*==\s*null\s*\)') `
    "REGRESSION GUARD: ToJson preserves first occurrence of each key (if (json[key] == null) ...)"

# The 10 #1444 call sites must wrap their tainted value in LogSanitizer.
# Pattern: the action= identifier followed somewhere within the same
# statement (up to the closing semicolon) by LogSanitizer.SanitizeValue(.
# Cross-line lazy match handles positional-arg formatting where the
# sanitizer call sits on the line below the format string.
function Assert-Sanitized {
    param([string]$Action, [string]$Reason)
    $pattern = "action=$Action[\s\S]*?;"
    if ($callsiteSrc -match $pattern) {
        $statement = $matches[0]
        # Accept either an inline LogSanitizer call or a pre-sanitized local
        # (e.g. safeMessage). The latter pattern keeps a single sanitize at
        # the top of the catch block and re-uses the result.
        Assert-True (($statement -match 'LogSanitizer\.SanitizeValue\(') -or
                     ($statement -match '\bsafeMessage\b')) `
            "REGRESSION GUARD: action=$Action sanitizes $Reason"
    } else {
        Assert-True $false "REGRESSION GUARD: action=$Action sanitizes $Reason (call site not found)"
    }
}

# AccountIdentity ArgumentException must NOT propagate uncaught - if it
# does, Sitecore's default Application error handler logs the raw name.
# Surface this as a regression guard via two source-level checks:
#   1. The catch block exists for ArgumentException right after the
#      AccountIdentity construction in AuthenticateRequest.
#   2. RejectAuthenticationMethod sanitizes ex.Message BEFORE assigning
#      to StatusDescription (HttpResponse throws HttpException on CR/LF
#      in the reason phrase, and the message has literal \n\n).
Assert-True ($callsiteSrc -match 'identity = new AccountIdentity\(authUserName\);[\s\S]{0,400}?catch \(ArgumentException') `
    "REGRESSION GUARD: AccountIdentity construction wrapped in try/catch (#1444 manual-test finding)"
Assert-True ($callsiteSrc -match 'StatusDescription \+= \$" \{safeMessage\}"') `
    "REGRESSION GUARD: StatusDescription receives sanitized message (closes response-splitting via ex.Message)"

Assert-Sanitized "deprecatedAuth"          "username from query string"
Assert-Sanitized "clientSignatureFailed"   "kid from JWT header"
Assert-Sanitized "clientNotFound kid"      "kid from JWT header"
Assert-Sanitized "bearerAuthSuccess"       "username after impersonation switch"
Assert-Sanitized "bearerAuthFailed"        "username on failed bearer auth"
Assert-Sanitized "authRejected service"    "username on rejected auth"
Assert-Sanitized "authRejected error"      "exception message on auth failure"
Assert-Sanitized "userUnauthorized"        "authUserName on authorization failure"
Assert-Sanitized "undeterminedFilename"    "media-upload originalPath (zip-entry name)"
Assert-Sanitized "mediaUploaded"           "media-upload fileName"
