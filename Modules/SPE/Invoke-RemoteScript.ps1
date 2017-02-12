function Invoke-RemoteScript {
    <#
        .SYNOPSIS
            Run scripts in Sitecore PowerShell Extensions via web service calls.
    
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
    	        Stop-ScriptSession -Session $session


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
        [System.Management.Automation.PSCredential]
        $Credential,
        
        [Parameter()]
        [Alias("ArgumentList")]
        [hashtable]$Arguments,

        [Parameter(ParameterSetName='Session')]
        [switch]$AsJob
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
        $functionScriptBlock = {
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
        $ScriptBlock = [scriptblock]::Create($functionScriptBlock.ToString() + $ScriptBlock.ToString());
    }

    if($AsJob.IsPresent) {
        $nestedScript = $ScriptBlock.ToString()
        $ScriptBlock = [scriptblock]::Create("Start-ScriptSession -ScriptBlock { $($nestedScript) } -ArgumentList `$params | Select-Object -ExpandProperty ID")
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
        $parameters = ConvertTo-CliXml -InputObject $Arguments
    }

    if($PSCmdlet.ParameterSetName -eq "InProcess") {
        # TODO: This will likely fail for params.
        [scriptblock]::Create($newScriptBlock).Invoke()
    } else {
        if($PSCmdlet.ParameterSetName -eq "Session") {
            $Username = $Session.Username
            $Password = $Session.Password
            $SessionId = $Session.SessionId
            $Credential = $Session.Credential
            $Connection = $Session.Connection
        } else {
            $Connection = $ConnectionUri | ForEach-Object { [PSCustomObject]@{ Uri = [Uri]$_; Proxy = $null } }
        }
        
        foreach($singleConnection in $Connection) {
            if($singleConnection.Uri.AbsoluteUri -notmatch ".*\.asmx(\?wsdl)?") {
                $singleConnection.Uri = [Uri]"$($singleConnection.Uri.AbsoluteUri.TrimEnd('/'))/sitecore%20modules/PowerShell/Services/RemoteAutomation.asmx?wsdl"
            }
    
            if(!$singleConnection.Proxy) {
                $proxyProps = @{
                    Uri = $singleConnection.Uri
                }
    
                if($Credential) {
                    $proxyProps["Credential"] = $Credential
                }
    
                $singleConnection.Proxy = New-WebServiceProxy @proxyProps
                if($Credential) {
                    $singleConnection.Proxy.Credentials = $Credential
                }
            }
            if(-not $singleConnection.Proxy) { return $null }

            $response = $singleConnection.Proxy.ExecuteScriptBlock2($Username, $Password, $newScriptBlock, $parameters, $SessionId)
            if($response) {
                if($hasRedirectedMessages) {
                    foreach($record in ConvertFrom-CliXml -InputObject $response) {
                        if($record -is [PSObject] -and $record.PSObject.TypeNames -contains "Deserialized.System.Management.Automation.VerboseRecord") {
                            Write-Verbose $record.ToString()
                        } elseif($record -is [PSObject] -and $record.PSObject.TypeNames -contains "Deserialized.System.Management.Automation.InformationRecord") {
                            Write-Information $record.ToString()
                        } elseif($record -is [PSObject] -and $record.PSObject.TypeNames -contains "Deserialized.System.Management.Automation.DebugRecord") {
                            Write-Debug $record.ToString()
                        } elseif($record -is [PSObject] -and $record.PSObject.TypeNames -contains "Deserialized.System.Management.Automation.WarningRecord") {
                            Write-Warning $record.ToString()
                        } elseif($record -is [PSObject] -and $record.PSObject.TypeNames -contains "Deserialized.System.Management.Automation.ErrorRecord") {
                            Write-Error $record.ToString()
                        } else {
                            $record
                        }
                    }
                } else {
                    ConvertFrom-CliXml -InputObject $response
                }
            } elseif ($response -eq "login failed") {
                Write-Verbose "Login with the specified account failed."
                break            
            } else {
                Write-Verbose "No response returned by the service. If results were expected confirm that the service is enabled and the account has access."
            }
        }
    }
}