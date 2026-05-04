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
        HttpClients           = $Session._HttpClients
    }
}

function New-SpeHttpClient {
    param(
        [string]$Username,
        [string]$Password,
        [string]$SharedSecret,
        [string]$AccessKeyId,
        [string]$AccessToken,
        [System.Management.Automation.PSCredential]$Credential,
        [bool]$UseDefaultCredentials,
        [Uri]$Uri,
        [hashtable]$Cache,
        [string]$Algorithm = "HS256"
    )
    $cacheKey = $Uri.AbsoluteUri
    if ($null -ne $Cache -and $Cache.ContainsKey($cacheKey)) {
        $client = $Cache[$cacheKey]
    } else {
        $handler = New-Object System.Net.Http.HttpClientHandler
        $handler.AutomaticDecompression = [System.Net.DecompressionMethods]::GZip -bor [System.Net.DecompressionMethods]::Deflate
        if ($Credential) { $handler.Credentials = $Credential }
        if ($UseDefaultCredentials) { $handler.UseDefaultCredentials = $true }
        $client = New-Object System.Net.Http.HttpClient $handler
        if ($null -ne $Cache) { $Cache[$cacheKey] = $client }
    }
    # Auth header is refreshed on every call; JWT tokens expire after 30 seconds.
    # Perf note: every call rebuilds the JWT (ConvertTo-Json x2, Base64 x2, HMACSHA256).
    # Caching the token for its 30s lifetime cuts ~700 us per call (see
    # tests/benchmarks/Measure-RemotingPerformance.ps1). Not cached today because
    # the savings are <2% of end-to-end HTTP latency and no high-frequency caller
    # has asked for it. Revisit if a workload shows up that makes it measurable.
    if (![string]::IsNullOrEmpty($AccessToken)) {
        # Externally-issued bearer token (OAuth / OIDC). No client-side minting.
        $client.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $AccessToken)
    } elseif (![string]::IsNullOrEmpty($SharedSecret)) {
        $jwtParams = @{
            Algorithm = $Algorithm
            Issuer = 'SPE Remoting'
            Audience = $Uri.GetLeftPart([System.UriPartial]::Authority)
            SecretKey = $SharedSecret
            ValidForSeconds = 30
        }
        if (![string]::IsNullOrEmpty($AccessKeyId)) {
            $jwtParams['KeyId'] = $AccessKeyId
        }
        if (![string]::IsNullOrEmpty($Username)) {
            $jwtParams['Name'] = $Username
        }
        $token = New-Jwt @jwtParams
        $client.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token)
    } else {
        $authBytes = [System.Text.Encoding]::GetEncoding("iso-8859-1").GetBytes("${Username}:${Password}")
        $client.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Basic", [System.Convert]::ToBase64String($authBytes))
    }
    $client
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
        ForEach-Object { $_.Group | Select-Object -First 1 })

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

