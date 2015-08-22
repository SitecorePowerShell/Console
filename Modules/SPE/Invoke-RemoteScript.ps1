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

    if($PSCmdlet.ParameterSetName -eq "InProcess") {
        $ScriptBlock.Invoke()
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
    
            if($Arguments) {
                $parameters = ConvertTo-CliXml -InputObject $Arguments
            }
            
            $response = $singleConnection.Proxy.ExecuteScriptBlock2($Username, $Password, $ScriptBlock.ToString(), $parameters, $SessionId)
            if($response) {
                ConvertFrom-CliXml -InputObject $response
            }
        }
    }
}