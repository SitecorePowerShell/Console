Add-Type -AssemblyName System.Net.Http

# GZip-compresses the request body and wraps it in a ByteArrayContent with the
# correct headers. Pulled out of Invoke-RemoteScript's send loop.
function Compress-SpeRequestBody {
    param([string]$Body)
    $messageBytes = [System.Text.Encoding]::UTF8.GetBytes($Body)
    $ms = New-Object System.IO.MemoryStream
    $gzip = New-Object System.IO.Compression.GZipStream($ms, [System.IO.Compression.CompressionMode]::Compress, $true)
    $gzip.Write($messageBytes, 0, $messageBytes.Length)
    $gzip.Close()
    $ms.Position = 0
    $content = New-Object System.Net.Http.ByteArrayContent(@(, $ms.ToArray()))
    $ms.Close()
    $content.Headers.ContentType = New-Object System.Net.Http.Headers.MediaTypeHeaderValue("text/plain")
    $content.Headers.ContentEncoding.Add("gzip")
    return $content
}

# Pulls an HTTP header value (first match) or returns $null.
function Get-HttpHeaderValue {
    param($Headers, [string]$Name)
    if (-not $Headers -or -not $Headers.Contains($Name)) { return $null }
    return ($Headers.GetValues($Name) | Select-Object -First 1)
}

# Dispatches a non-2xx HttpResponseMessage to the right error path.
# Returns a result hashtable:
#   @{ Retryable = bool; WaitSeconds = int; EncounteredError = bool; Handled = bool }
# When Handled = $true, the caller should clear $errorResponse and either retry
# (if Retryable) or stop (if EncounteredError).
function Resolve-HttpErrorResponse {
    param(
        [System.Net.Http.HttpResponseMessage]$ErrorResponse,
        [int]$MaxRetries,
        [int]$RetryAttempt,
        [int]$Ceiling429,
        [int]$Ceiling503,
        [string]$OutputFormat,
        [Uri]$Uri,
        [string]$RootPath
    )
    $result = @{ Retryable = $false; WaitSeconds = 0; EncounteredError = $false; Handled = $false }
    if (-not $ErrorResponse) { return $result }

    $status = [int]$ErrorResponse.StatusCode

    # 429 and 503 share the retry-or-report shape.
    if ($status -eq 429 -or $status -eq 503) {
        $retryAfter = Get-HttpHeaderValue $ErrorResponse.Headers "Retry-After"
        $defaultWait = if ($status -eq 503) { 2 } else { 1 }
        $ceiling = if ($status -eq 503) { $Ceiling503 } else { $Ceiling429 }

        if ($MaxRetries -gt 0 -and $RetryAttempt -lt $MaxRetries) {
            $waitParsed = 0
            [void][int]::TryParse([string]$retryAfter, [ref]$waitParsed)
            if ($waitParsed -lt 1) { $waitParsed = $defaultWait }
            $result.Retryable = $true
            $result.WaitSeconds = [Math]::Min($waitParsed, $ceiling)
            $result.Handled = $true
            return $result
        }

        if ($status -eq 429) {
            $rateLimit = Get-HttpHeaderValue $ErrorResponse.Headers "X-RateLimit-Limit"
            $msg = "Rate limit exceeded."
            if ($rateLimit) { $msg += " Limit: $rateLimit requests per window." }
            if ($retryAfter) { $msg += " Retry after $retryAfter seconds." }
            Write-Error -Message $msg -Category LimitsExceeded `
                -CategoryActivity "Invoke" -CategoryTargetName $Uri -CategoryReason "TooManyRequests"
        } else {
            $msg = "Service unavailable (503). The server may be starting up."
            if ($retryAfter) { $msg += " Retry after $retryAfter seconds." }
            Write-Error -Message $msg -Category ConnectionError `
                -CategoryActivity "Invoke" -CategoryTargetName $Uri -CategoryReason "ServiceUnavailable"
        }
        $result.EncounteredError = $true
        $result.Handled = $true
        return $result
    }

    # 401: auth failure. Read X-SPE-AuthFailureReason for a specific message.
    if ($status -eq 401) {
        $reasonCode = Get-HttpHeaderValue $ErrorResponse.Headers "X-SPE-AuthFailureReason"
        $msg = switch ($reasonCode) {
            "expired"  { "Authentication failed: the API Key has expired. Rotate the key or update its Expires field." }
            "disabled" { "Authentication failed: the API Key is disabled. Enable it on the API Key item to allow access." }
            "invalid"  { "Authentication failed: credentials are invalid (unknown key or bad signature)." }
            default    { "Authentication failed. Check that the proper credentials are provided." }
        }
        Write-Error -Message $msg -Category AuthenticationError `
            -CategoryActivity "Invoke" -CategoryTargetName $Uri -CategoryReason "Unauthorized"
        $result.EncounteredError = $true
        $result.Handled = $true
        return $result
    }

    # 403: policy/command block. Structured via X-SPE-Restriction.
    if ($status -eq [int][System.Net.HttpStatusCode]::Forbidden) {
        $restriction = Get-HttpHeaderValue $ErrorResponse.Headers "X-SPE-Restriction"
        $blockedCmd = Get-HttpHeaderValue $ErrorResponse.Headers "X-SPE-BlockedCommand"
        $policyName = Get-HttpHeaderValue $ErrorResponse.Headers "X-SPE-Policy"

        if ($restriction -eq "command-blocked") {
            Write-Error -Message "Script contains a blocked command: $blockedCmd" -Category SecurityError `
                -CategoryActivity "Invoke" -CategoryTargetName $Uri -CategoryReason "CommandBlocked"
            $result.EncounteredError = $true
            $result.Handled = $true
            return $result
        }
        if ($restriction -eq "policy-blocked") {
            Write-Error -Message "Script blocked by remoting policy '$policyName': $blockedCmd" -Category SecurityError `
                -CategoryActivity "Invoke" -CategoryTargetName $Uri -CategoryReason "PolicyRestriction"
            $result.EncounteredError = $true
            $result.Handled = $true
            return $result
        }

        # Generic 403: try to surface a structured JSON error body, else verbose hint.
        if ($OutputFormat -eq 'Json' -and (Invoke-JsonErrorBody $ErrorResponse)) {
            $result.EncounteredError = $true
            $result.Handled = $true
            return $result
        }
        Write-Verbose -Message "Check that the proper credentials are provided and that the service configurations are enabled."
        return $result  # Unhandled; caller surfaces the generic Write-Error fallback.
    }

    # Any other status: try JSON body, else a verbose hint for 404.
    if ($OutputFormat -eq 'Json' -and (Invoke-JsonErrorBody $ErrorResponse)) {
        $result.EncounteredError = $true
        $result.Handled = $true
        return $result
    }
    if ($status -eq [int][System.Net.HttpStatusCode]::NotFound) {
        Write-Verbose -Message "Check that the service files are properly configured."
    }
    return $result  # Unhandled; caller surfaces generic Write-Error.
}

# Attempts to parse the error response body as a structured-errors JSON payload
# (only meaningful when OutputFormat=Json). Returns $true on a successful parse
# so the caller knows the error was surfaced; $false otherwise.
function Invoke-JsonErrorBody {
    param([System.Net.Http.HttpResponseMessage]$ErrorResponse)
    try {
        $errorBody = $ErrorResponse.Content.ReadAsStringAsync().Result
        if (-not $errorBody) { return $false }
        Parse-Response -Response $errorBody -HasRedirectedMessages $false -Raw $false -OutputFormat 'Json'
        return $true
    } catch {
        return $false
    }
}

# Map Deserialized.System.Management.Automation.*Record typename to its matching
# Write-* cmdlet. Used by Parse-Response to route server-captured stream records
# back into the caller's corresponding streams.
$script:DeserializedRecordTypePrefix = "Deserialized.System.Management.Automation."
$script:DeserializedRecordWriters = @{
    "VerboseRecord"     = { param($msg) Write-Verbose $msg }
    "InformationRecord" = { param($msg) Write-Information $msg }
    "DebugRecord"       = { param($msg) Write-Debug $msg }
    "WarningRecord"     = { param($msg) Write-Warning $msg }
    "ErrorRecord"       = { param($msg) Write-Error $msg }
}

function Emit-DeserializedRecord {
    param($Record)
    if (-not ($Record -is [PSObject])) {
        $Record
        return
    }
    foreach ($typeName in $Record.PSObject.TypeNames) {
        if (-not $typeName.StartsWith($script:DeserializedRecordTypePrefix)) { continue }
        $shortName = $typeName.Substring($script:DeserializedRecordTypePrefix.Length)
        $writer = $script:DeserializedRecordWriters[$shortName]
        if ($writer) {
            & $writer $Record.ToString()
            return
        }
    }
    $Record
}

function Write-JsonErrors {
    param($Errors)
    foreach ($errObj in $Errors) {
        # Legacy flat string from older server or non-structured format.
        if (-not ($errObj -is [PSCustomObject]) -or -not $errObj.PSObject.Properties['errorCategory']) {
            Write-Error -Message ([string]$errObj)
            continue
        }

        # Structured error from enhanced server (errorFormat=structured).
        $category = [System.Management.Automation.ErrorCategory]::NotSpecified
        try { $category = [System.Management.Automation.ErrorCategory]$errObj.errorCategory } catch { }

        $exceptionMsg = if ($errObj.exceptionMessage) { $errObj.exceptionMessage } else { $errObj.message }
        $errRecord = New-Object System.Management.Automation.ErrorRecord(
            (New-Object System.Exception($exceptionMsg)),
            $errObj.fullyQualifiedErrorId,
            $category,
            $errObj.categoryTargetName
        )
        if ($errObj.scriptStackTrace) {
            $errRecord | Add-Member -NotePropertyName 'RemoteScriptStackTrace' -NotePropertyValue $errObj.scriptStackTrace
        }
        if ($errObj.invocationInfo) {
            $errRecord | Add-Member -NotePropertyName 'RemoteInvocationInfo' -NotePropertyValue $errObj.invocationInfo
        }
        Write-Error -ErrorRecord $errRecord
    }
}

function Parse-Response {
    param(
        [string]$Response,
        [bool]$HasRedirectedMessages,
        [bool]$Raw,
        [string]$OutputFormat = 'CliXml'
    )

    # Guard: empty response (the "login failed" branch in the pre-refactor code
    # was unreachable - "login failed" is truthy, so it never hit the falsy-else).
    if (-not $Response) {
        Write-Verbose "No response returned by the service. If results were expected confirm that the service is enabled and the account has access. A common cause for this is an application pool recycling."
        return
    }

    # JSON path: short-circuit on success; fall through to CliXml if the body
    # isn't actually JSON (older server ignoring the OutputFormat hint).
    if ($OutputFormat -eq 'Json') {
        try {
            Write-Verbose -Message "Parsing JSON response from server."
            $parsed = $Response | ConvertFrom-Json
            if ($parsed.errors -and $parsed.errors.Count -gt 0) {
                Write-JsonErrors $parsed.errors
            }
            if ($parsed.output) { $parsed.output }
            return
        } catch {
            Write-Warning "JSON parsing failed - the server may not support OutputFormat 'Json'. Falling back to CliXml deserialization. Upgrade the server to use JSON output."
            # fall through
        }
    }

    # Split the response body into output + messages.
    # Raw mode: the body may contain a `<#messages#>` delimiter; output portion
    # is emitted directly, messages portion (if present) is deserialized below.
    # Non-Raw mode: the whole body is a CliXml messages stream.
    $responseMessages = ""
    $hasMessages = $HasRedirectedMessages
    if ($Raw) {
        if ($Response.Contains("<#messages#>")) {
            $parts = $Response -split "<#messages#>"
            $parts[0]
            if ($parts.Length -gt 1) {
                $responseMessages = $parts[1]
                $hasMessages = $true
            }
        } elseif (![string]::IsNullOrEmpty($Response)) {
            $Response
        }
    } else {
        $responseMessages = $Response
    }

    if ([string]::IsNullOrEmpty($responseMessages)) { return }

    # hasMessages path: body carries serialized stream records that need to be
    # routed back to the caller's verbose/debug/info/warning/error streams.
    if (-not $hasMessages) {
        Write-Verbose -Message "Deserializing the response message from the server."
        ConvertFrom-CliXml -InputObject $responseMessages
        return
    }

    Write-Verbose -Message "Redirecting output to the appropriate stream."
    foreach ($record in ConvertFrom-CliXml -InputObject $responseMessages) {
        Emit-DeserializedRecord $record
    }
}

function Invoke-RemoteScript {
    <#
        .SYNOPSIS
            Run scripts in Sitecore PowerShell Extensions via web service calls.

        .DESCRIPTION
            When using commands such as Write-Verbose, be sure the preference settings are configured properly.

            Change each of these to "Continue" in order to see the message appear in the console.

            Example values:

            ConfirmPreference              High
            DebugPreference                SilentlyContinue
            ErrorActionPreference          Continue
            InformationPreference          SilentlyContinue
            ProgressPreference             Continue
            VerbosePreference              SilentlyContinue
            WarningPreference              Continue
            WhatIfPreference               False

        .EXAMPLE
            The following example remotely executes a script in Sitecore using a reusable session.

            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
            Invoke-RemoteScript -Session $session -ScriptBlock { Get-User -id admin }
            Stop-ScriptSession -Session $session

            Name                     Domain       IsAdministrator IsAuthenticated
            ----                     ------       --------------- ---------------
            sitecore\admin           sitecore     True            False

        .EXAMPLE
            The following remotely executes a script in Sitecore with the $Using variable.

            $date = [datetime]::Now
            $script = {
                $Using:date
            }

            Invoke-RemoteScript -ConnectionUri "http://remotesitecore" -Username "admin" -Password "b" -ScriptBlock $script
            Stop-ScriptSession -Session $session

            6/25/2015 11:09:17 AM

        .EXAMPLE
            The following example runs a script as a ScriptSession job on the server (using Start-ScriptSession internally).
            The arguments are passed to the server with the help of the $Using convention.
            The results are finally returned and the job is removed.

            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
            $identity = "admin"
            $date = [datetime]::Now
            $jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
                [Sitecore.Security.Accounts.User]$user = Get-User -Identity $using:identity
                $user.Name
                $using:date
            } -AsJob
            Start-Sleep -Seconds 2

            Invoke-RemoteScript -Session $session -ScriptBlock {
                $ss = Get-ScriptSession -Id $using:JobId
                $ss | Receive-ScriptSession

                if($ss.LastErrors) {
                    $ss.LastErrors
                }
            }
            Stop-ScriptSession -Session $session

        .EXAMPLE
            The following remotely executes a script in Sitecore with arguments.

            $script = {
                [Sitecore.Security.Accounts.User]$user = Get-User -Identity admin
                $user
                $params.date.ToString()
            }

            $args = @{
                "date" = [datetime]::Now
            }

            Invoke-RemoteScript -ConnectionUri "http://remotesitecore" -Username "admin" -Password "b" -ScriptBlock $script -ArgumentList $args
            Stop-ScriptSession -Session $session

            Name                     Domain       IsAdministrator IsAuthenticated
            ----                     ------       --------------- ---------------
            sitecore\admin           sitecore     True            False
            6/25/2015 11:09:17 AM

    	.LINK
    		Wait-RemoteScriptSession

    	.LINK
    		New-ScriptSession

    	.LINK
    	    Stop-ScriptSession
    #>
   [CmdletBinding(SupportsShouldProcess = $true, DefaultParameterSetName="InProcess")]
    param(

        [Parameter(ParameterSetName='InProcess')]
        [Parameter(ParameterSetName='Session')]
        [Parameter(ParameterSetName='Uri')]
        [scriptblock]$ScriptBlock,

        [Parameter(ParameterSetName='Session')]
        [ValidateNotNull()]
        [pscustomobject]$Session,

        [Parameter(ParameterSetName='Uri')]
        [Uri[]]$ConnectionUri,

        [Parameter(ParameterSetName='Uri')]
        [string]$SessionId,

        [Parameter(ParameterSetName='Uri')]
        [string]$Username,

        [Parameter(ParameterSetName='Uri')]
        [string]$Password,

        [Parameter(ParameterSetName='Uri')]
        [string]$SharedSecret,

        [Parameter(ParameterSetName='Uri')]
        [System.Management.Automation.PSCredential]
        $Credential,

        [Parameter()]
        [Alias("ArgumentList")]
        [hashtable]$Arguments,

        [Parameter(ParameterSetName='Session')]
        [switch]$AsJob,

        [Parameter()]
        [switch]$Raw,

        [Parameter()]
        [ValidateSet('CliXml', 'Json', 'Raw')]
        [string]$OutputFormat = 'CliXml',

        [Parameter()]
        [switch]$StructuredErrors,

        [Parameter()]
        [ValidateRange(0, 10)]
        [int]$MaxRetries = 0
    )

    # Hardcoded ceilings on Retry-After (seconds). Protect against a hostile server
    # parking the client indefinitely.
    $retryCeiling429 = 60
    $retryCeiling503 = 10

    # Map -Raw switch to OutputFormat for backwards compat
    if($Raw.IsPresent -and $OutputFormat -eq 'CliXml') {
        $OutputFormat = 'Raw'
    }

    if($PSCmdlet.MyInvocation.BoundParameters["WhatIf"].IsPresent) {
        $functionScriptBlock = {
            $WhatIfPreference = $true
        }
        $ScriptBlock = [scriptblock]::Create($functionScriptBlock.ToString() + $ScriptBlock.ToString());
    }
    $hasRedirectedMessages = $false
    $captureStreams = $false
    if($PSCmdlet.MyInvocation.BoundParameters["Debug"].IsPresent -or $PSCmdlet.MyInvocation.BoundParameters["Verbose"].IsPresent) {
        # Signal the server to inject the Write-* stream-capture bootstrap
        # after its policy scan. Keeps the user's script text clean so
        # restrictive remoting policies do not see the bootstrap's Write-*
        # calls. Server-side injection lives in RemoteScriptCall.ashx.cs.
        $hasRedirectedMessages = $true
        $captureStreams = $true
    }

    if($AsJob.IsPresent) {
        $nestedScript = $ScriptBlock.ToString()
        $ScriptBlock = [scriptblock]::Create("Start-ScriptSession -ScriptBlock { $($nestedScript) } -ArgumentList `$params | Select-Object -ExpandProperty ID")
    }

    $paramsPrefix = if ($AsJob.IsPresent) { '$' } else { '$params.' }
    $resolved = Resolve-UsingVariables -ScriptBlock $ScriptBlock -Arguments $Arguments -ParamsPrefix $paramsPrefix
    $newScriptBlock = $resolved.ScriptText
    $Arguments = $resolved.Arguments


    if($Arguments) {
        #This is still needed in order to pass types
        $parameters = ConvertTo-CliXml -InputObject $Arguments
    }

    if($PSCmdlet.ParameterSetName -eq "InProcess") {
        # TODO: This will likely fail for params.
        [scriptblock]::Create($newScriptBlock).Invoke()
    } else {
        if($PSCmdlet.ParameterSetName -eq "Session") {
            $sd = Expand-ScriptSession -Session $Session
            $Username             = $sd.Username
            $Password             = $sd.Password
            $SharedSecret         = $sd.SharedSecret
            $AccessKeyId          = $sd.AccessKeyId
            $SessionId            = $sd.SessionId
            $Credential           = $sd.Credential
            $UseDefaultCredentials = $sd.UseDefaultCredentials
            $ConnectionUri        = $sd.ConnectionUri
            $PersistentSession    = $sd.PersistentSession
            $Algorithm            = $sd.Algorithm
            $clientCache          = $sd.HttpClients
        } else {
            $SessionId = [guid]::NewGuid()
            $PersistentSession = $false
            $Algorithm = "HS256"
            $clientCache = @{}
        }

        $serviceUrl = "/-/script/script/?"
        $isRawFormat = $OutputFormat -eq 'Raw'
        $serviceUrl += "sessionId=" + $SessionId + "&rawOutput=" + $isRawFormat + "&outputFormat=" + $OutputFormat + "&persistentSession=" + $PersistentSession
        if($StructuredErrors.IsPresent) {
            $serviceUrl += "&errorFormat=structured"
        }
        if($captureStreams) {
            $serviceUrl += "&captureStreams=true"
        }
        foreach ($uri in $ConnectionUri) {
            $url = $uri.AbsoluteUri.TrimEnd("/") + $serviceUrl
            $localParams = if ($parameters) { $parameters } else { '' }

            #creating a psuedo file split on a special comment rather than trying to pass a potentially enormous set of data to the handler
            #theoretically this is the equivalent of a binary upload to the endpoint and breaking it into 2 files
            $body = "$($newScriptBlock)<#$($SessionId)#>$($localParams)"

            Write-Verbose -Message "Preparing to invoke the script against the service at url $($url)"
            $client = New-SpeHttpClient -Username $Username -Password $Password -SharedSecret $SharedSecret `
                -AccessKeyId $AccessKeyId -Credential $Credential -UseDefaultCredentials $UseDefaultCredentials `
                -Uri $uri -Cache $clientCache -Algorithm $Algorithm

            $errorResponse = $null
            $encounteredError = $false
            $response = $null
            $taskResult = $null
            $retryAttempt = 0

            do {
                $retryable = $false
                $retryWaitSeconds = 0
                $errorResponse = $null
                $encounteredError = $false
                $response = $null
                $taskResult = $null

                try {
                    Write-Verbose -Message "Transferring script to server"
                    $content = Compress-SpeRequestBody -Body $body

                    $postResponse = $client.PostAsync($url, $content)
                    $taskResult = $postResponse.Result
                    if ($taskResult) {
                        $taskResult.EnsureSuccessStatusCode() > $null
                        $response = $taskResult.Content.ReadAsStringAsync().Result
                        Write-Verbose -Message "Script transfer complete."
                    } else {
                        $ex = $postResponse.Exception
                        $reason = $postResponse.Exception.Message
                        $innerException = $postResponse.Exception
                        while (($innerException = $innerException.InnerException)) {
                            $reason += " " + $innerException.Message
                        }
                        $encounteredError = $true
                        Write-Error -Message "Server response: $($reason)" -Category ConnectionError `
                            -CategoryActivity "Post" -CategoryTargetName $uri -CategoryReason "$($postResponse.Status)" -CategoryTargetType $RootPath -ErrorAction SilentlyContinue
                        $Host.UI.WriteErrorLine($reason)
                    }
                }
                catch [System.Net.Http.HttpRequestException] {
                    $ex = $_.Exception
                    [System.Net.Http.HttpResponseMessage]$errorResponse = $taskResult
                    if (-not $errorResponse) {
                        Write-Verbose -Message $ex.Message
                    } else {
                        $outcome = Resolve-HttpErrorResponse -ErrorResponse $errorResponse `
                            -MaxRetries $MaxRetries -RetryAttempt $retryAttempt `
                            -Ceiling429 $retryCeiling429 -Ceiling503 $retryCeiling503 `
                            -OutputFormat $OutputFormat -Uri $uri -RootPath $RootPath
                        if ($outcome.Handled) {
                            $retryable = $outcome.Retryable
                            $retryWaitSeconds = $outcome.WaitSeconds
                            $encounteredError = $outcome.EncounteredError
                            $errorResponse = $null
                        }
                        # If not Handled, leave $errorResponse set so the generic
                        # post-loop Write-Error fallback below surfaces it.
                    }
                }

                if ($retryable -and $retryAttempt -lt $MaxRetries) {
                    $retryAttempt++
                    Write-Verbose -Message "Retrying after $retryWaitSeconds second(s) (attempt $retryAttempt of $MaxRetries, status $([int]$taskResult.StatusCode))."
                    Start-Sleep -Seconds $retryWaitSeconds
                    continue
                }
                break
            } while ($true)

            # Surface rate-limit headers from a successful response so callers running
            # with -Verbose can see remaining budget without triggering a 429.
            if (!$encounteredError -and $taskResult -and $taskResult.Headers) {
                $hasLimit = $taskResult.Headers.Contains("X-RateLimit-Limit")
                $hasRemaining = $taskResult.Headers.Contains("X-RateLimit-Remaining")
                if ($hasLimit -or $hasRemaining) {
                    $rlLimit = if ($hasLimit) { $taskResult.Headers.GetValues("X-RateLimit-Limit") | Select-Object -First 1 } else { "" }
                    $rlRemaining = if ($hasRemaining) { $taskResult.Headers.GetValues("X-RateLimit-Remaining") | Select-Object -First 1 } else { "" }
                    $rlReset = if ($taskResult.Headers.Contains("X-RateLimit-Reset")) { $taskResult.Headers.GetValues("X-RateLimit-Reset") | Select-Object -First 1 } else { "" }
                    $rlLine = "Rate limit: X-RateLimit-Limit=$rlLimit X-RateLimit-Remaining=$rlRemaining"
                    if ($rlReset) { $rlLine += " X-RateLimit-Reset=$rlReset" }
                    Write-Verbose -Message $rlLine
                }
            }

            if ($errorResponse) {
                $encounteredError = $true
                Write-Error -Message "Server response: $($errorResponse.ReasonPhrase)" -Category ConnectionError `
                    -CategoryActivity "Download" -CategoryTargetName $uri -Exception $ex -CategoryReason "$($errorResponse.StatusCode)" -CategoryTargetType $RootPath
            }

            if(!$encounteredError) {
                Write-Verbose -Message "Parsing response from server."
                Parse-Response -Response $response -HasRedirectedMessages $hasRedirectedMessages -Raw ($OutputFormat -eq 'Raw') -OutputFormat $OutputFormat
            } else {
                Write-Verbose -Message "Stopping from further execution."
            }
        }
    }
}

