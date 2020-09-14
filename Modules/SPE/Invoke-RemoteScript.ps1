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
        [bool]$Raw
    )
    if($response) {
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
        break            
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
        [switch]$Raw
    )

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

    $usingVariables = @(Get-UsingVariables -ScriptBlock $scriptBlock | 
        Group-Object -Property SubExpression | 
        ForEach-Object {
        $_.Group | Select-Object -First 1
    })
    
    $invokeWithArguments = $false        
    if ($usingVariables.count -gt 0) {
        $usingVar = $usingVariables | Group-Object -Property SubExpression | ForEach-Object {$_.Group | Select-Object -First 1}  
        Write-Debug "CommandOrigin: $($MyInvocation.CommandOrigin)"      
        $usingVariableValues = Get-UsingVariableValues -UsingVar $usingVar
        $invokeWithArguments = $true
    }

    if ($invokeWithArguments) {
        if(!$Arguments) { $Arguments = @{} }

        $paramsPrefix = "`$params."
        if($AsJob.IsPresent) {
            $paramsPrefix = "$"
        }
        $command = $ScriptBlock.ToString()
        foreach($usingVarValue in $usingVariableValues) {
            $Arguments[($usingVarValue.NewName.TrimStart('$'))] = $usingVarValue.Value
            $command = $command.Replace($usingVarValue.Name, "$($paramsPrefix)$($usingVarValue.NewName.TrimStart('$'))")
        }

        $newScriptBlock = $command
    } else {
        $newScriptBlock = $scriptBlock.ToString()
    }


    if($Arguments) {
        #This is still needed in order to pass types
        $parameters = ConvertTo-CliXml -InputObject $Arguments
    }

    if($PSCmdlet.ParameterSetName -eq "InProcess") {
        # TODO: This will likely fail for params.
        [scriptblock]::Create($newScriptBlock).Invoke()
    } else {
        if($PSCmdlet.ParameterSetName -eq "Session") {
            $Username = $Session.Username
            $Password = $Session.Password
            $SharedSecret = $Session.SharedSecret
            $SessionId = $Session.SessionId
            $Credential = $Session.Credential
            $UseDefaultCredentials = $Session.UseDefaultCredentials
            $ConnectionUri = $Session | ForEach-Object { $_.Connection.BaseUri }
            $PersistentSession = $Session.PersistentSession
        } else {
            $SessionId = [guid]::NewGuid()
            $PersistentSession = $false
        }
        
        $serviceUrl = "/-/script/script/?"
        $serviceUrl += "sessionId=" + $SessionId + "&rawOutput=" + $Raw.IsPresent + "&persistentSession=" + $PersistentSession
        foreach ($uri in $ConnectionUri) {            
            $url = $uri.AbsoluteUri.TrimEnd("/") + $serviceUrl
            $localParams = $parameters | Out-String
            
            #creating a psuedo file split on a special comment rather than trying to pass a potentially enormous set of data to the handler
            #theoretically this is the equivalent of a binary upload to the endpoint and breaking it into 2 files
            $body = "$($newScriptBlock)<#$($SessionId)#>$($localParams)"
            
            Write-Verbose -Message "Preparing to invoke the script against the service at url $($url)"
            Add-Type -AssemblyName System.Net.Http
            $handler = New-Object System.Net.Http.HttpClientHandler
            $handler.AutomaticDecompression = [System.Net.DecompressionMethods]::GZip -bor [System.Net.DecompressionMethods]::Deflate
            $client = New-Object -TypeName System.Net.Http.Httpclient $handler

            if(![string]::IsNullOrEmpty($SharedSecret)) {
                $token = New-Jwt -Algorithm 'HS256' -Issuer 'SPE Remoting' -Audience ($uri.GetLeftPart([System.UriPartial]::Authority)) -Name $Username -SecretKey $SharedSecret -ValidforSeconds 30
                $client.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token)
            } else {
                $authBytes = [System.Text.Encoding]::GetEncoding("iso-8859-1").GetBytes("$($Username):$($Password)")
                $client.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Basic", [System.Convert]::ToBase64String($authBytes))
            }
                 
            if ($Credential) {
                $handler.Credentials = $Credential
            }

            if ($UseDefaultCredentials) {
                $handler.UseDefaultCredentials = $UseDefaultCredentials
            }
            
            [System.Net.HttpWebResponse]$script:errorResponse = $null 
            $script:encounteredError = $false

            $response = & {
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
                        $taskResult.Content.ReadAsStringAsync().Result
                        Write-Verbose -Message "Script transfer complete."
                    } else {
                        $script:ex = $postResponse.Exception
                        $reason = $postResponse.Exception.Message
                        $innerException = $postResponse.Exception
                        while(($innerException = $innerException.InnerException)) {
                            $reason += " " + $innerException.Message                            
                        }
                        $script:encounteredError = $true
                        Write-Error -Message "Server response: $($reason)" -Category ConnectionError `
                            -CategoryActivity "Post" -CategoryTargetName $uri -CategoryReason "$($postResponse.Status)" -CategoryTargetType $RootPath -ErrorAction SilentlyContinue
                        $Host.UI.WriteErrorLine($reason)
                    }
                }
                catch [System.Net.Http.HttpRequestException] {
                    $script:ex = $_.Exception
                    [System.Net.Http.HttpResponseMessage]$script:errorResponse = $taskResult
                    if ($errorResponse) {
                        if ($errorResponse.StatusCode -eq [System.Net.HttpStatusCode]::Forbidden) {
                            Write-Verbose -Message "Check that the proper credentials are provided and that the service configurations are enabled."
                        }
                        elseif ($errorResponse.StatusCode -eq [System.Net.HttpStatusCode]::NotFound) {
                            Write-Verbose -Message "Check that the service files are properly configured."
                        }
                    }
                    else {
                        Write-Verbose -Message $ex.Message
                    }
                }
            }
            
            if ($errorResponse) {
                $script:encounteredError = $true
                Write-Error -Message "Server response: $($errorResponse.ReasonPhrase)" -Category ConnectionError `
                    -CategoryActivity "Download" -CategoryTargetName $uri -Exception ($script:ex) -CategoryReason "$($errorResponse.StatusCode)" -CategoryTargetType $RootPath 
            }
            
            if(!$encounteredError) {
                Write-Verbose -Message "Parsing response from server."
                Parse-Response -Response $response -HasRedirectedMessages $hasRedirectedMessages -Raw $Raw
            } else {
                Write-Verbose -Message "Stopping from further execution."
            }
        }
    }
}

function Invoke-RemoteScriptAsync {
    param(
        [Parameter()]
        [pscustomobject]$Session,

        [Parameter()]
        [scriptblock]$ScriptBlock,

        [Parameter()]
        [Alias("ArgumentList")]
        [hashtable]$Arguments,

        [Parameter()]
        [switch]$Raw
    )

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

    $usingVariables = @(Get-UsingVariables -ScriptBlock $scriptBlock | 
        Group-Object -Property SubExpression | 
        ForEach {
        $_.Group | Select -First 1
    })
    
    $invokeWithArguments = $false        
    if ($usingVariables.count -gt 0) {
        $usingVar = $usingVariables | Group-Object -Property SubExpression | ForEach {$_.Group | Select -First 1}  
        Write-Debug "CommandOrigin: $($MyInvocation.CommandOrigin)"      
        $usingVariableValues = Get-UsingVariableValues -UsingVar $usingVar
        $invokeWithArguments = $true
    }

    if ($invokeWithArguments) {
        if(!$Arguments) { $Arguments = @{} }

        $paramsPrefix = "`$params."
        if($AsJob.IsPresent) {
            $paramsPrefix = "$"
        }
        $command = $ScriptBlock.ToString()
        foreach($usingVarValue in $usingVariableValues) {
            $Arguments[($usingVarValue.NewName.TrimStart('$'))] = $usingVarValue.Value
            $command = $command.Replace($usingVarValue.Name, "$($paramsPrefix)$($usingVarValue.NewName.TrimStart('$'))")
        }

        $newScriptBlock = $command
    } else {
        $newScriptBlock = $scriptBlock.ToString()
    }

    if($Arguments) {
        #This is still needed in order to pass types
        $parameters = ConvertTo-CliXml -InputObject $Arguments
    }

    $newScriptBlock = $scriptBlock.ToString()
    $Username = $Session.Username
    $Password = $Session.Password
    $SessionId = $Session.SessionId
    $Credential = $Session.Credential
    $UseDefaultCredentials = $Session.UseDefaultCredentials
    $ConnectionUri = $Session | ForEach-Object { $_.Connection.BaseUri }
    $PersistentSession = $Session.PersistentSession

    $serviceUrl = "/-/script/script/?"
    $serviceUrl += "sessionId=" + $SessionId + "&rawOutput=" + $Raw.IsPresent + "&persistentSession=" + $PersistentSession

    $handler = New-Object System.Net.Http.HttpClientHandler
    $handler.AutomaticDecompression = [System.Net.DecompressionMethods]::GZip -bor [System.Net.DecompressionMethods]::Deflate
    $client = New-Object -TypeName System.Net.Http.Httpclient $handler
    $authBytes = [System.Text.Encoding]::GetEncoding("iso-8859-1").GetBytes("$($Username):$($Password)")
    $client.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Basic", [System.Convert]::ToBase64String($authBytes))
       
    if ($Credential) {
        $handler.Credentials = $Credential
    }

    if($UseDefaultCredentials) {
        $handler.UseDefaultCredentials = $UseDefaultCredentials
    }
   
    $localParams = $parameters | Out-String

    $messageBytes = [System.Text.Encoding]::UTF8.GetBytes("$($newScriptBlock.ToString())<#$($SessionId)#>$($localParams)")

    $ms = New-Object System.IO.MemoryStream
    $gzip = New-Object System.IO.Compression.GZipStream($ms, [System.IO.Compression.CompressionMode]::Compress, $true)
    $gzip.Write($messageBytes, 0, $messageBytes.Length)
    $gzip.Close()
    $ms.Position = 0
    $content = New-Object System.Net.Http.ByteArrayContent(@(,$ms.ToArray()))
    $ms.Close()
    $content.Headers.ContentType = New-Object System.Net.Http.Headers.MediaTypeHeaderValue("text/plain")
    $content.Headers.ContentEncoding.Add("gzip")

    foreach($uri in $ConnectionUri) {
        $url = $uri.AbsoluteUri.TrimEnd("/") + $serviceUrl

        $taskPost = $client.PostAsync($url, $content)

        $localProps = @{
            Raw = $Raw.IsPresent
        }
        $continuation = New-RunspacedDelegate ([Func[System.Threading.Tasks.Task[System.Net.Http.HttpResponseMessage],object, PSObject]] { 
            param($t,$props)

            $contentTask = $t.Result.Content.ReadAsStringAsync()
            $response = $contentTask.GetAwaiter().GetResult()
            Parse-Response -Response $response -HasRedirectedMessages $false -Raw $props.Raw
        })
        Invoke-GenericMethod -InputObject $taskPost -MethodName ContinueWith -GenericType PSObject -ArgumentList $continuation,$localProps
    }    
}