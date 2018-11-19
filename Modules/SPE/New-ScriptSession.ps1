function New-ScriptSession {
    <#
        .SYNOPSIS
            Creates a new script session in Sitecore PowerShell Extensions via web service calls.
    
        .EXAMPLE
            The following remotely connects to an instance of Sitecore initializes a session.
            
            New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
    
            Username      : admin
            Password      : b
            ConnectionUri : http://concentrasitecore/
            SessionId     : 528b9865-a69e-4875-919f-12209646c934
            Credential    : 
        
        .PARAMETER Username
            Specifies the Sitecore identity used for connecting to a remote instance.

        .PARAMETER Password
            Specifies the Sitecore password associated with the identity. 
        
        .PARAMETER Timeout
            Specifies the duration of the wait, in seconds.

        .PARAMETER ConnectionUri
            Specifies the remote instance url. HTTPS is highly recommended.

        .PARAMETER Credential
            Specifies a user account that has permission to perform this action. The default is the current user. This is an alternative to using the UseDefaultCredential parameter.

        .PARAMETER UseDefaultCredential
            Indicates that this command uses the default credential. This command sets the UseDefaultCredential property in the resulting proxy object to True. This is an alternative to using the Credential parameter.
        
        .LINK
            Invoke-RemoteScript

        .LINK
            Wait-RemoteScriptSession

        .LINK
            Stop-ScriptSession

    #>
    [CmdletBinding(DefaultParameterSetName="All")]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$Username = $null,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$Password = $null,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [Uri[]]$ConnectionUri = $null,

        [Parameter(Mandatory = $false, HelpMessage = "The timeout in seconds.")]
        [int]$Timeout,

        [Parameter(Mandatory = $false, ParameterSetName = "Credential")]
        [System.Management.Automation.PSCredential]
        $Credential,

        [Parameter(Mandatory = $false, ParameterSetName = "DefaultCredentials")]
        [switch]$UseDefaultCredentials
    )
    
    begin {
        $sessionId = [guid]::NewGuid()
        $session = @{
            "Username" = [string]$Username
            "Password" = [string]$Password
            "SessionId" = [string]$sessionId
            "Credential" = [System.Management.Automation.PSCredential]$Credential
            "UseDefaultCredentials" = [bool]$UseDefaultCredentials
            "Connection" = @()
            "PersistentSession" = $false
        }
    }

    process {

        foreach($uri in $ConnectionUri) {

            $connection = [PSCustomObject]@{
                Uri = [Uri]$uri
                BaseUri = [Uri]$uri
            }

            $session["Connection"] += @($connection)
        }
    }
    
    end {
        [PSCustomObject]$session
    }
}