Add-Type -AssemblyName System.Net.Http

$writeStreamOverridesScript = {
    if($PSVersionTable.PSVersion.Major -ge 5) {
        function Write-Information {
            param([string]$Message)
            $InformationPreference = "Continue"
            Microsoft.PowerShell.Utility\Write-Information -Message $Message 6>&1
        }
    }
    function Write-Debug {
        param([string]$Message)
        $DebugPreference = "Continue"
        Microsoft.PowerShell.Utility\Write-Debug -Message $Message 5>&1
    }
    function Write-Verbose {
        param([string]$Message)
        $VerbosePreference = "Continue"
        Microsoft.PowerShell.Utility\Write-Verbose -Message $Message 4>&1
    }
    function Write-Warning {
        param([string]$Message)
        $WarningPreference = "Continue"
        Microsoft.PowerShell.Utility\Write-Warning -Message $Message 3>&1
    }
    function Write-Error {
        param([string]$Message)
        $WarningPreference = "Continue"
        Microsoft.PowerShell.Utility\Write-Error -Message $Message 2>&1
    }
}

function Parse-Response {
    param(
        [string]$Response,
        [bool]$HasRedirectedMessages,
        [bool]$Raw,
        [string]$OutputFormat = 'CliXml'
    )
    if($response) {
        if($OutputFormat -eq 'Json') {
            Write-Verbose -Message "Parsing JSON response from server."
            try {
                $parsed = $response | ConvertFrom-Json
                if($parsed.errors -and $parsed.errors.Count -gt 0) {
                    foreach($errObj in $parsed.errors) {
                        if($errObj -is [PSCustomObject] -and $errObj.PSObject.Properties['errorCategory']) {
                            # Structured error from enhanced server (errorFormat=structured)
                            $category = [System.Management.Automation.ErrorCategory]::NotSpecified
                            try {
                                $category = [System.Management.Automation.ErrorCategory]$errObj.errorCategory
                            } catch { }

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
                        else {
                            # Legacy flat string from older server or non-structured format
                            Write-Error -Message ([string]$errObj)
                        }
                    }
                }
                if($parsed.output) {
                    $parsed.output
                }
                return
            } catch {
                Write-Warning "JSON parsing failed - the server may not support OutputFormat 'Json'. Falling back to CliXml deserialization. Upgrade the server to use JSON output."
                # Fall through to CliXml parsing below
            }
        }

        $parsedResponse = $response
        $responseMessages = ""
        if($Raw) {
            if($parsedResponse.Contains("<#messages#>")) {
                $parsedResponse = $parsedResponse -split "<#messages#>"
            }
            if($parsedResponse -is [string[]]) {
                $parsedResponse[0]
                if($parsedResponse.Length -gt 1) {
                    $responseMessages = $parsedResponse[1]
                    $hasRedirectedMessages = $true
                }
            } elseif(![string]::IsNullOrEmpty($parsedResponse)) {
                $parsedResponse
            }
        } elseif($parsedResponse) {
            $responseMessages = $parsedResponse
        }

        if(![string]::IsNullOrEmpty($responseMessages)) {
            if ($hasRedirectedMessages) {
                Write-Verbose -Message "Redirecting output to the appropriate stream."
                foreach ($record in ConvertFrom-CliXml -InputObject $responseMessages) {
                    if ($record -is [PSObject] -and $record.PSObject.TypeNames -contains "Deserialized.System.Management.Automation.VerboseRecord") {
                        Write-Verbose $record.ToString()
                    }
                    elseif ($record -is [PSObject] -and $record.PSObject.TypeNames -contains "Deserialized.System.Management.Automation.InformationRecord") {
                        Write-Information $record.ToString()
                    }
                    elseif ($record -is [PSObject] -and $record.PSObject.TypeNames -contains "Deserialized.System.Management.Automation.DebugRecord") {
                        Write-Debug $record.ToString()
                    }
                    elseif ($record -is [PSObject] -and $record.PSObject.TypeNames -contains "Deserialized.System.Management.Automation.WarningRecord") {
                        Write-Warning $record.ToString()
                    }
                    elseif ($record -is [PSObject] -and $record.PSObject.TypeNames -contains "Deserialized.System.Management.Automation.ErrorRecord") {
                        Write-Error $record.ToString()
                    }
                    else {
                        $record
                    }
                }
            }
            else {
                Write-Verbose -Message "Deserializing the response message from the server."
                ConvertFrom-CliXml -InputObject $responseMessages
            }
        }
    } elseif ($response -eq "login failed") {
        Write-Verbose "Login with the specified account failed."
        return
    } else {
        Write-Verbose "No response returned by the service. If results were expected confirm that the service is enabled and the account has access. A common cause for this is an application pool recycling."
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
        [switch]$StructuredErrors
    )

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
    if($PSCmdlet.MyInvocation.BoundParameters["Debug"].IsPresent -or $PSCmdlet.MyInvocation.BoundParameters["Verbose"].IsPresent) {
        $hasRedirectedMessages = $true
        $ScriptBlock = [scriptblock]::Create($writeStreamOverridesScript.ToString() + $ScriptBlock.ToString());
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
            $SessionId            = $sd.SessionId
            $Credential           = $sd.Credential
            $UseDefaultCredentials = $sd.UseDefaultCredentials
            $ConnectionUri        = $sd.ConnectionUri
            $PersistentSession    = $sd.PersistentSession
            $clientCache          = $sd.HttpClients
        } else {
            $SessionId = [guid]::NewGuid()
            $PersistentSession = $false
            $clientCache = @{}
        }

        $serviceUrl = "/-/script/script/?"
        $isRawFormat = $OutputFormat -eq 'Raw'
        $serviceUrl += "sessionId=" + $SessionId + "&rawOutput=" + $isRawFormat + "&outputFormat=" + $OutputFormat + "&persistentSession=" + $PersistentSession
        if($StructuredErrors.IsPresent) {
            $serviceUrl += "&errorFormat=structured"
        }
        foreach ($uri in $ConnectionUri) {
            $url = $uri.AbsoluteUri.TrimEnd("/") + $serviceUrl
            $localParams = if ($parameters) { $parameters } else { '' }

            #creating a psuedo file split on a special comment rather than trying to pass a potentially enormous set of data to the handler
            #theoretically this is the equivalent of a binary upload to the endpoint and breaking it into 2 files
            $body = "$($newScriptBlock)<#$($SessionId)#>$($localParams)"

            Write-Verbose -Message "Preparing to invoke the script against the service at url $($url)"
            $client = New-SpeHttpClient -Username $Username -Password $Password -SharedSecret $SharedSecret `
                -Credential $Credential -UseDefaultCredentials $UseDefaultCredentials -Uri $uri -Cache $clientCache
            
            $errorResponse = $null
            $encounteredError = $false
            $response = $null

            try {
                Write-Verbose -Message "Transferring script to server"
                $messageBytes = [System.Text.Encoding]::UTF8.GetBytes($body)
                $ms = New-Object System.IO.MemoryStream
                $gzip = New-Object System.IO.Compression.GZipStream($ms, [System.IO.Compression.CompressionMode]::Compress, $true)
                $gzip.Write($messageBytes, 0, $messageBytes.Length)
                $gzip.Close()
                $ms.Position = 0
                $content = New-Object System.Net.Http.ByteArrayContent(@(, $ms.ToArray()))
                $ms.Close()
                $content.Headers.ContentType = New-Object System.Net.Http.Headers.MediaTypeHeaderValue("text/plain")
                $content.Headers.ContentEncoding.Add("gzip")

                $postResponse = $client.PostAsync($url, $content)
                $taskResult = $postResponse.Result
                if($taskResult) {
                    $taskResult.EnsureSuccessStatusCode() > $null
                    $response = $taskResult.Content.ReadAsStringAsync().Result
                    Write-Verbose -Message "Script transfer complete."
                } else {
                    $ex = $postResponse.Exception
                    $reason = $postResponse.Exception.Message
                    $innerException = $postResponse.Exception
                    while(($innerException = $innerException.InnerException)) {
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
                if ($errorResponse) {
                    # Try to parse structured error body for JSON format
                    if ($OutputFormat -eq 'Json') {
                        try {
                            $errorBody = $errorResponse.Content.ReadAsStringAsync().Result
                            if ($errorBody) {
                                Parse-Response -Response $errorBody -HasRedirectedMessages $false -Raw $false -OutputFormat 'Json'
                                $encounteredError = $true
                                $errorResponse = $null
                            }
                        } catch { }
                    }

                    if ($errorResponse) {
                        if ($errorResponse.StatusCode -eq [System.Net.HttpStatusCode]::Forbidden) {
                            Write-Verbose -Message "Check that the proper credentials are provided and that the service configurations are enabled."
                        }
                        elseif ($errorResponse.StatusCode -eq [System.Net.HttpStatusCode]::NotFound) {
                            Write-Verbose -Message "Check that the service files are properly configured."
                        }
                    }
                }
                else {
                    Write-Verbose -Message $ex.Message
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

