function Get-SpeBackoffDelay {
    <#
        .SYNOPSIS
            Returns a jittered retry delay (seconds) for SPE remoting client retries.

        .DESCRIPTION
            Two modes:
              * ServerHintSeconds > 0 (e.g. from Retry-After): returns the hint
                with +/-20% jitter so concurrent clients that all parsed the same
                hint do not rebound on the same wall-clock tick.
              * ServerHintSeconds = 0: full jitter,
                rand(0.1, min(Cap, Base * 2^min(Attempt,6))) seconds.

            Heuristic values: 20% spread is enough to break synchronization without
            ignoring the server's hint; the 2^6 ceiling avoids overflow on extreme
            attempt counts. Re-tune if observed retry storms still cluster.
    #>
    [CmdletBinding()]
    [OutputType([double])]
    param(
        [Parameter(Mandatory = $true)]
        [int]$Attempt,
        [double]$BaseSeconds = 1.0,
        [double]$CapSeconds = 30.0,
        [int]$ServerHintSeconds = 0
    )
    if ($ServerHintSeconds -gt 0) {
        $minMs = [int][Math]::Max(100, $ServerHintSeconds * 800)
        $maxMs = [int]($ServerHintSeconds * 1200)
        $delay = (Get-Random -Minimum $minMs -Maximum ($maxMs + 1)) / 1000.0
        return [Math]::Min($CapSeconds, [Math]::Max(0.1, $delay))
    }
    $exp = [Math]::Min($Attempt, 6)
    $ceiling = [Math]::Min($CapSeconds, $BaseSeconds * [Math]::Pow(2, $exp))
    if ($ceiling -lt 0.1) { $ceiling = 0.1 }
    $ceilingMs = [int]($ceiling * 1000)
    return (Get-Random -Minimum 100 -Maximum ($ceilingMs + 1)) / 1000.0
}

function Test-SpeSafeTransportError {
    <#
        .SYNOPSIS
            Classifies a remoting failure as unambiguously safe to retry.

        .DESCRIPTION
            Returns $true only when the request can be proven not to have reached
            the server, so a retry cannot double-execute a non-idempotent script.
            Covers: HTTP 502 Bad Gateway (Traefik upstream never accepted),
            SocketException ConnectionRefused/HostNotFound/TryAgain/NoData.

            Ambiguous failures (504, mid-stream connection reset, post-send
            timeout, generic IOException) intentionally return $false. Those
            require an opt-in switch from the caller and are deferred.
    #>
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [System.Exception]$Exception,
        [int]$StatusCode = 0
    )
    if ($StatusCode -eq 502) { return $true }
    if (-not $Exception) { return $false }

    $cur = $Exception
    while ($cur) {
        if ($cur -is [System.Net.Sockets.SocketException]) {
            $code = [int]$cur.SocketErrorCode
            # 10061 ConnectionRefused, 11001 HostNotFound, 11002 TryAgain, 11004 NoData
            return ($code -eq 10061 -or $code -eq 11001 -or $code -eq 11002 -or $code -eq 11004)
        }
        $cur = $cur.InnerException
    }
    return $false
}

function Expand-ScriptSession {
    param([pscustomobject]$Session)
    [pscustomobject]@{
        Username              = $Session.Username
        Password              = $Session.Password
        SharedSecret          = $Session.SharedSecret
        AccessKeyId           = $Session.AccessKeyId
        AccessToken           = $Session.AccessToken
        SessionId             = $Session.SessionId
        Credential            = $Session.Credential
        UseDefaultCredentials = [bool]$Session.UseDefaultCredentials
        ConnectionUri         = @($Session.Connection | ForEach-Object { $_.BaseUri })
        Algorithm             = if ($Session.Algorithm) { $Session.Algorithm } else { "HS256" }
        PersistentSession     = $Session.PersistentSession
    }
}

# AppDomain-scoped HttpClient pool. Lives on a static .NET type so it survives
# Remove-Module / Import-Module cycles and is shared by every runspace in the
# AppDomain (PowerShell module $script: scope is per-runspace, which would
# defeat sharing across runspace-pool workers).
#
# Why include an identity fingerprint in the key: HttpClientHandler binds
# Credential / UseDefaultCredentials at construction. Two callers with
# different identities cannot share one handler safely. Keying by URI alone
# would let session B reuse a connection wired to session A's NetworkCredential.
#
# DNS staleness is bounded by max age: every cached client is replaced after
# SpeHttpClientPool.MaxAgeMinutes, so a moved CM is picked up within that
# window. .NET Framework 4.8 / Windows PowerShell 5.1 lacks
# SocketsHttpHandler.PooledConnectionLifetime, so we expire entries ourselves.
#
# Why we do not Dispose evicted/aged-out clients: another runspace can be
# mid-PostAsync against the cached client. Disposing under it would surface
# as ObjectDisposedException on the in-flight request. Letting GC collect
# the handler when the last caller drops its reference is correct; the cost
# is a short-lived memory holdover, not a socket leak.
if (-not ('Spe.Internal.SpeHttpClientPool' -as [type])) {
    try {
        Add-Type -ReferencedAssemblies @('System.Net.Http') -TypeDefinition @'
namespace Spe.Internal {
    using System;
    using System.Collections.Concurrent;
    using System.Net.Http;

    public sealed class SpeCachedClient {
        public HttpClient Client;
        public DateTime CreatedUtc;
    }

    public static class SpeHttpClientPool {
        private static readonly ConcurrentDictionary<string, SpeCachedClient> Cache =
            new ConcurrentDictionary<string, SpeCachedClient>();

        public static int MaxAgeMinutes = 10;
        public static int MaxEntries    = 64;

        public static int Count { get { return Cache.Count; } }

        public static HttpClient GetOrAdd(string key, Func<HttpClient> factory) {
            TimeSpan maxAge = TimeSpan.FromMinutes(MaxAgeMinutes);
            DateTime now = DateTime.UtcNow;

            SpeCachedClient existing;
            if (Cache.TryGetValue(key, out existing) && now - existing.CreatedUtc <= maxAge) {
                return existing.Client;
            }

            HttpClient built = factory();
            SpeCachedClient candidate = new SpeCachedClient { Client = built, CreatedUtc = now };

            // Atomic add-or-replace-if-stale. The update factory runs under the
            // dictionary's per-bucket lock, so the freshness check on `prev`
            // cannot race with another writer. If `prev` is still fresh, our
            // newly-built client is discarded (GC reclaims it).
            SpeCachedClient result = Cache.AddOrUpdate(
                key,
                candidate,
                (k, prev) => (now - prev.CreatedUtc > maxAge) ? candidate : prev);

            if (object.ReferenceEquals(result, candidate)) {
                EvictIfFull();
            }
            return result.Client;
        }

        private static void EvictIfFull() {
            int over = Cache.Count - MaxEntries;
            if (over <= 0) return;
            // Snapshot, sort by CreatedUtc, remove oldest. Best-effort under
            // concurrent insertion. We deliberately do not Dispose evicted
            // clients; live callers may still hold references.
            var arr = Cache.ToArray();
            Array.Sort(arr, (a, b) => a.Value.CreatedUtc.CompareTo(b.Value.CreatedUtc));
            int evicted = 0;
            for (int i = 0; i < arr.Length && evicted < over; i++) {
                SpeCachedClient removed;
                if (Cache.TryRemove(arr[i].Key, out removed)) {
                    evicted++;
                }
            }
        }

        public static void Clear() {
            Cache.Clear();
        }
    }
}
'@
    } catch {
        # Race during simultaneous first-load from multiple runspaces: another
        # thread already added the type. Re-check; only swallow the error if
        # the type really is present now, otherwise rethrow so a real
        # compilation failure surfaces instead of silently degrading later.
        if (-not ('Spe.Internal.SpeHttpClientPool' -as [type])) { throw }
    }
}

function Get-SpeClientCacheKey {
    param(
        [Uri]$Uri,
        [string]$Username,
        [string]$SharedSecret,
        [string]$AccessKeyId,
        [string]$AccessToken,
        [System.Management.Automation.PSCredential]$Credential,
        [bool]$UseDefaultCredentials
    )
    # Hash sensitive material: plaintext secrets must not sit in a cache key
    # that could leak via Get-Variable, debug logs, or memory dumps. SHA256
    # truncated to 16 bytes; collision risk is negligible for SPE caller counts.
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $material =
            if ($UseDefaultCredentials) { 'default|' }
            elseif ($Credential)        { "cred|$($Credential.UserName)" }
            elseif ($AccessToken)       { "oauth|$AccessToken" }
            elseif ($SharedSecret)      { "hmac|$AccessKeyId|$Username|$SharedSecret" }
            else                        { "basic|$Username" }
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($material)
        $hash  = [System.Convert]::ToBase64String($sha.ComputeHash($bytes), 0, 16)
        return "$($Uri.AbsoluteUri)|$hash"
    } finally { $sha.Dispose() }
}

function New-SpeAuthHeader {
    # Returns a fresh AuthenticationHeaderValue. Callers must stamp it on the
    # per-request HttpRequestMessage rather than HttpClient.DefaultRequestHeaders
    # (mutating defaults on a shared client is not thread-safe and would race
    # with other callers' requests in flight).
    param(
        [string]$Username,
        [string]$Password,
        [string]$SharedSecret,
        [string]$AccessKeyId,
        [string]$AccessToken,
        [Uri]$Uri,
        [string]$Algorithm = "HS256"
    )
    if (![string]::IsNullOrEmpty($AccessToken)) {
        return New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $AccessToken)
    }
    if (![string]::IsNullOrEmpty($SharedSecret)) {
        # JWT tokens are short-lived (30 s). Rebuild on every call rather than
        # caching: New-Jwt costs ~700 us, which is <2% of end-to-end HTTP
        # latency, and a per-call token avoids the bookkeeping a cache would
        # need to honor server-side clock skew.
        $jwtParams = @{
            Algorithm = $Algorithm
            Issuer = 'SPE Remoting'
            Audience = $Uri.GetLeftPart([System.UriPartial]::Authority)
            SecretKey = $SharedSecret
            ValidForSeconds = 30
        }
        if (![string]::IsNullOrEmpty($AccessKeyId)) { $jwtParams['KeyId'] = $AccessKeyId }
        if (![string]::IsNullOrEmpty($Username))    { $jwtParams['Name']  = $Username    }
        $token = New-Jwt @jwtParams
        return New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token)
    }
    $authBytes = [System.Text.Encoding]::GetEncoding("iso-8859-1").GetBytes("${Username}:${Password}")
    return New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Basic", [System.Convert]::ToBase64String($authBytes))
}

function New-SpeHttpClient {
    # Returns @{ Client; AuthHeader }. The Client is shared (AppDomain-scoped
    # cache); the AuthHeader is per-call and must be applied to a per-request
    # HttpRequestMessage by the caller, never to client.DefaultRequestHeaders.
    param(
        [string]$Username,
        [string]$Password,
        [string]$SharedSecret,
        [string]$AccessKeyId,
        [string]$AccessToken,
        [System.Management.Automation.PSCredential]$Credential,
        [bool]$UseDefaultCredentials,
        [Uri]$Uri,
        [string]$Algorithm = "HS256"
    )

    $cacheKey = Get-SpeClientCacheKey -Uri $Uri -Username $Username `
        -SharedSecret $SharedSecret -AccessKeyId $AccessKeyId -AccessToken $AccessToken `
        -Credential $Credential -UseDefaultCredentials $UseDefaultCredentials

    # Capture identity into the factory's closure so the C# pool only invokes
    # it on a true miss. Closure params bind at scriptblock-creation time;
    # subsequent callers with different identities use a different cache key
    # and a different factory, so there's no cross-identity leak.
    $cred = $Credential
    $useDefault = $UseDefaultCredentials
    $factory = [System.Func[System.Net.Http.HttpClient]] {
        $handler = New-Object System.Net.Http.HttpClientHandler
        $handler.AutomaticDecompression = [System.Net.DecompressionMethods]::GZip -bor [System.Net.DecompressionMethods]::Deflate
        if ($cred) { $handler.Credentials = $cred }
        if ($useDefault) { $handler.UseDefaultCredentials = $true }
        New-Object System.Net.Http.HttpClient $handler
    }

    $client = [Spe.Internal.SpeHttpClientPool]::GetOrAdd($cacheKey, $factory)

    $authHeader = New-SpeAuthHeader -Username $Username -Password $Password `
        -SharedSecret $SharedSecret -AccessKeyId $AccessKeyId -AccessToken $AccessToken `
        -Uri $Uri -Algorithm $Algorithm

    [pscustomobject]@{
        Client     = $client
        AuthHeader = $authHeader
    }
}

function Invoke-SpeHttpRequest {
    # Convenience wrapper: builds an HttpRequestMessage with the per-call
    # auth header, sends it, and returns the response. Each request gets a
    # fresh message because HttpRequestMessage is single-use, and per-request
    # headers are the only thread-safe way to carry auth on a shared client.
    param(
        [Parameter(Mandatory = $true)] [pscustomobject]$ClientInfo,
        [Parameter(Mandatory = $true)] [System.Net.Http.HttpMethod]$Method,
        [Parameter(Mandatory = $true)] [string]$Url,
        [System.Net.Http.HttpContent]$Content,
        [System.Threading.CancellationToken]$CancellationToken = [System.Threading.CancellationToken]::None
    )
    $req = New-Object System.Net.Http.HttpRequestMessage($Method, $Url)
    $req.Headers.Authorization = $ClientInfo.AuthHeader
    if ($Content) { $req.Content = $Content }
    return $ClientInfo.Client.SendAsync($req, $CancellationToken)
}

# ----------------------------------------------------------------------
# Shared -Verbose helpers used by every HttpClient-based remoting cmdlet
# (Invoke-RemoteScript, Test-RemoteConnection, Stop-ScriptSession,
# Invoke-RemoteWait, Send/Receive-RemoteItem, Wait-Remote*). Live here
# rather than inside one cmdlet file so the verbose surface stays
# consistent across the suite. None are exported.
# ----------------------------------------------------------------------

# Pulls an HTTP header value (first match) or returns $null. Works against
# both HttpResponseHeaders and HttpContentHeaders since both expose the
# same Contains / GetValues shape.
function Get-HttpHeaderValue {
    param($Headers, [string]$Name)
    if (-not $Headers -or -not $Headers.Contains($Name)) { return $null }
    # Index instead of Select-Object -First: on Windows PowerShell 5.1 the
    # latter leaks StopUpstreamCommandsException into callers' -ErrorVariable.
    return @($Headers.GetValues($Name))[0]
}

# Formats the X-Spe-Request-Id correlation header as a " (rid=xxxxx)"
# suffix, or empty string when absent. Lets every error-message builder
# tag its message with the audit-log rid without repeating the lookup.
function Format-RidSuffix {
    param($Headers)
    $rid = Get-HttpHeaderValue $Headers "X-Spe-Request-Id"
    if ($rid) { return " (rid=$rid)" }
    return ""
}

# Names the auth method this call uses, for -Verbose output only. Mirrors
# the precedence used inside New-SpeHttpClient. Identity is included; the
# secret itself is not.
function Get-AuthDescription {
    param([string]$Username, $Password, [string]$SharedSecret, [string]$AccessKeyId, [string]$AccessToken, $Credential, [bool]$UseDefaultCredentials)
    if ($AccessToken)                    { return "OAuth(Bearer)" }
    if ($SharedSecret -and $AccessKeyId) { return "SharedSecret($AccessKeyId)" }
    if ($SharedSecret)                   { return "SharedSecret(HMAC,$Username)" }
    if ($UseDefaultCredentials)          { return "Windows(default)" }
    if ($Credential)                     { return "Credential($($Credential.UserName))" }
    if ($Username)                       { return "Basic($Username)" }
    return "Anonymous"
}

# Format the request-body line so callers can verify the gzip threshold
# fired the way they expect and see the wire bytes leaving the cmdlet.
function Format-BodyShape {
    param([System.Net.Http.ByteArrayContent]$Content, [int]$RawBytes)
    $wireBytes = $Content.Headers.ContentLength
    if ($null -eq $wireBytes -or $wireBytes -le 0) {
        $wireBytes = $Content.ReadAsByteArrayAsync().Result.Length
    }
    $isGzip = $Content.Headers.ContentEncoding -and
              $Content.Headers.ContentEncoding.Count -gt 0 -and
              @($Content.Headers.ContentEncoding)[0] -eq 'gzip'
    if ($isGzip) { return "Body: $RawBytes bytes raw -> $wireBytes bytes gzip" }
    return "Body: $RawBytes bytes plaintext (gzip skipped)"
}

# Format the response line: status, wall-clock time, server-side time
# (X-Spe-Duration-Ms), audit-log rid. Wall - server is roughly the
# network + queue overhead the call paid on top of script execution.
function Format-ResponseLine {
    param([System.Net.Http.HttpResponseMessage]$Response, [double]$WallMs)
    $status   = [int]$Response.StatusCode
    $reason   = $Response.ReasonPhrase
    $duration = Get-HttpHeaderValue $Response.Headers "X-Spe-Duration-Ms"
    $rid      = Get-HttpHeaderValue $Response.Headers "X-Spe-Request-Id"
    $line = "Response: $status $reason in {0:N0}ms" -f $WallMs
    if ($duration) { $line += " server={0}ms" -f $duration }
    if ($rid)      { $line += " rid=$rid" }
    return $line
}

# Shared verbose logger for the WebException catch block used by Send/Receive-RemoteItem.
# Note: HttpClient throws HttpRequestException, not WebException, so this is only reached
# by the legacy HttpWebRequest path. Preserved as-is for back-compat with downstream code
# that may still funnel WebException through here.
function Write-RemoteHttpResponseVerbose {
    param(
        [System.Net.HttpWebResponse]$Response,
        [Exception]$Exception
    )
    Write-Verbose -Message "Response exception message: $($Exception.Message)"
    Write-Verbose -Message "Response status description: $($Response.StatusDescription)"
    switch ($Response.StatusCode) {
        ([System.Net.HttpStatusCode]::Forbidden) {
            Write-Verbose -Message "Check that the proper credentials are provided and that the service configurations are enabled."
        }
        ([System.Net.HttpStatusCode]::NotFound) {
            Write-Verbose -Message "Check that the service files exist and are properly configured."
        }
    }
}

function Resolve-UsingVariables {
    <#
        .SYNOPSIS
            Detects $Using: variable references in a scriptblock, resolves their values from
            the caller's scope, and returns substituted script text plus a populated Arguments
            hashtable ready to pass to the remote endpoint.
    #>
    param(
        [scriptblock]$ScriptBlock,
        [hashtable]$Arguments,
        [string]$ParamsPrefix = '$params.'
    )

    $usingVariables = @(Get-UsingVariables -ScriptBlock $ScriptBlock |
        Group-Object -Property SubExpression |
        ForEach-Object { $_.Group[0] })

    if ($usingVariables.Count -eq 0) {
        return [pscustomobject]@{ ScriptText = $ScriptBlock.ToString(); Arguments = $Arguments }
    }

    if (!$Arguments) { $Arguments = @{} }

    $usingVarValues = Get-UsingVariableValues -UsingVar $usingVariables

    # Build a lookup from variable user-path to the replacement parameter name
    $replacementMap = @{}
    foreach ($usingVarValue in $usingVarValues) {
        $Arguments[$usingVarValue.NewName.TrimStart('$')] = $usingVarValue.Value
        $userPath = $usingVarValue.NewVarName -replace '^__using_', ''
        $replacementMap[$userPath] = "$($ParamsPrefix)$($usingVarValue.NewName.TrimStart('$'))"
    }

    # Replace using AST extents (reverse offset order) to avoid shifting positions
    $command = $ScriptBlock.ToString()
    # AST offsets are absolute; ToString() returns the body without braces for literal scriptblocks.
    # For [scriptblock]::Create() scriptblocks there are no braces, so no +1 adjustment needed.
    $baseOffset = $ScriptBlock.Ast.Extent.StartOffset
    if ($ScriptBlock.Ast.Extent.Text.StartsWith('{')) { $baseOffset++ }
    $allNodes = $ScriptBlock.Ast.FindAll(
        { $args[0] -is [System.Management.Automation.Language.UsingExpressionAst] }, $true) |
        Sort-Object { $_.Extent.StartOffset } -Descending
    foreach ($node in $allNodes) {
        $userPath = $node.SubExpression.VariablePath.UserPath
        if ($replacementMap.ContainsKey($userPath)) {
            $start = $node.Extent.StartOffset - $baseOffset
            $end   = $node.Extent.EndOffset - $baseOffset
            $command = $command.Substring(0, $start) + $replacementMap[$userPath] + $command.Substring($end)
        }
    }

    [pscustomobject]@{ ScriptText = $command; Arguments = $Arguments }
}

function Get-UsingVariables {
    param ([scriptblock]$ScriptBlock)
    if($ScriptBlock.ToString().IndexOf("`$using:", [System.StringComparison]::OrdinalIgnoreCase) -eq -1) { return }
    $ScriptBlock.Ast.FindAll({$args[0] -is [System.Management.Automation.Language.UsingExpressionAst]},$true)
}

function Get-UsingVariableValues {
    param ([System.Management.Automation.Language.UsingExpressionAst[]]$usingVar)
    ForEach ($var in $usingVar) {
        try {
            $value = $null
            $isRunspace = ($MyInvocation.CommandOrigin -eq [System.Management.Automation.CommandOrigin]::Runspace -or $MyInvocation.CommandOrigin -eq [System.Management.Automation.CommandOrigin]::Internal)
            $userpath = $Var.SubExpression.VariablePath.UserPath
            if ($isRunspace -and (Test-Path -Path "variable:\$($userpath)")) {
                Write-Verbose "Checking the Runspace for the variable $($userpath)."
                $value = Get-Variable -Name $userpath
            }

            if($value -eq $null -or [string]::IsNullOrEmpty($value.Value)) {
                Write-Verbose "Checking the SessionState for the variable $($userpath)."
                $value = ($PSCmdlet.SessionState.PSVariable.Get($userpath))
                if ([string]::IsNullOrEmpty($value)) {
                    throw 'No value!'
                }
            }
            [pscustomobject]@{
                Name = $var.SubExpression.Extent.Text
                Value = $value.Value
                NewName = ('$__using_{0}' -f $var.SubExpression.VariablePath.UserPath)
                NewVarName = ('__using_{0}' -f $var.SubExpression.VariablePath.UserPath)
            }
        } catch {
            throw "The value of the using variable '$($var.SubExpression.Extent.Text)' cannot be retrieved because it has not been set in the local session."
        }
    }
}

# No OnRemove handler: the HttpClient pool is AppDomain-scoped and shared
# across runspaces, so a Remove-Module in one runspace must not dispose
# clients that other runspaces are still using. GC reclaims handlers when
# the last caller drops its reference.
