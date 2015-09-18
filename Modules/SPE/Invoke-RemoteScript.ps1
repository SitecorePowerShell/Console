function Invoke-RemoteScript {
    <#
        .SYNOPSIS
            Run scripts in Sitecore PowerShell Extensions via web service calls.
    
        .EXAMPLE
            The following example remotely executes a script in Sitecore using a reusable session.
    
            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
            Invoke-RemoteScript -Session $session -ScriptBlock { Get-User -id admin }
    
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
    
            6/25/2015 11:09:17 AM
                    
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
    
            Name                     Domain       IsAdministrator IsAuthenticated
            ----                     ------       --------------- ---------------
            sitecore\admin           sitecore     True            False          
            6/25/2015 11:09:17 AM
    #>
    
    [CmdletBinding(DefaultParameterSetName="InProcess")]
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
        [hashtable]$Arguments
    )

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
        Write-Verbose ("Found {0} `$Using: variables!" -f $usingVariableValues.count)
        $invokeWithArguments = $true
    }
  
    if ($invokeWithArguments) {
        if(!$Arguments) { $Arguments = @{} }

        $command = "Invoke-Command -ScriptBlock ({$(Convert-UsingScript $scriptBlock)}) -ArgumentList "
        $variableNames = @()
        foreach($usingVarValue in $usingVariableValues) {
            $Arguments[($usingVarValue.NewName.TrimStart('$'))] = $usingVarValue.Value
            $variableNames += "`$params.$($usingVarValue.NewName.TrimStart('$'))"
        }
        $command += ($variableNames -join ",")

        $newScriptBlock = $command
    } else {
        $newScriptBlock = $scriptBlock.ToString()
    }

    if($Arguments) {
        $parameters = ConvertTo-CliXml -InputObject $Arguments
    }

    if($PSCmdlet.ParameterSetName -eq "InProcess") {
        # TODO: This will likely fail for params.
        $newScriptBlock.Invoke()
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
            if(!$singleConnection.Uri.AbsoluteUri.EndsWith(".asmx")) {
                $singleConnection.Uri = [Uri]"$($singleConnection.Uri.AbsoluteUri.TrimEnd('/'))/sitecore%20modules/PowerShell/Services/RemoteAutomation.asmx"
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
                ConvertFrom-CliXml -InputObject $response
            }
        }
    }
}