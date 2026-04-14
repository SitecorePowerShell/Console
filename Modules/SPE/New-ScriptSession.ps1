function New-ScriptSession {
    <#
        .SYNOPSIS
            Creates a new script session in Sitecore PowerShell Extensions via web service calls.

        .EXAMPLE
            The following remotely connects to an instance of Sitecore using the legacy shared secret.

            New-ScriptSession -Username admin -SharedSecret $secret -ConnectionUri https://remotesitecore

        .EXAMPLE
            The following remotely connects using an API Key (Access Key Id).

            New-ScriptSession -AccessKeyId "spe_a3f7b2c891d4e6f0a1b2c3d4" -SharedSecret $secret -ConnectionUri https://remotesitecore

        .PARAMETER Username
            Specifies the Sitecore identity used for connecting to a remote instance.
            Required for Password and SharedSecret parameter sets. Not used with AccessKey.

        .PARAMETER Password
            Specifies the Sitecore password associated with the identity.

        .PARAMETER SharedSecret
            Specifies the shared secret used for HMAC authentication.

        .PARAMETER AccessKeyId
            Specifies the Access Key Id of a Remoting API Key item. When provided, the
            JWT includes a kid header for direct key lookup. Username is not required
            because identity is determined by the Impersonate User field on the API Key item.

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
        [Parameter(Mandatory = $true, ParameterSetName = "All")]
        [Parameter(Mandatory = $true, ParameterSetName = "Password")]
        [Parameter(Mandatory = $true, ParameterSetName = "SharedSecret")]
        [ValidateNotNullOrEmpty()]
        [string]$Username = $null,

        [Parameter(Mandatory = $true, ParameterSetName = "Password")]
        [ValidateNotNullOrEmpty()]
        [string]$Password = $null,

        [Parameter(Mandatory = $true, ParameterSetName = "SharedSecret")]
        [Parameter(Mandatory = $true, ParameterSetName = "AccessKey")]
        [ValidateNotNullOrEmpty()]
        [string]$SharedSecret = $null,

        [Parameter(Mandatory = $true, ParameterSetName = "AccessKey")]
        [ValidateNotNullOrEmpty()]
        [string]$AccessKeyId = $null,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [Uri[]]$ConnectionUri = $null,

        [Parameter(Mandatory = $false, HelpMessage = "The timeout in seconds.")]
        [int]$Timeout,

        [Parameter(Mandatory = $false, ParameterSetName = "Password")]
        [Parameter(Mandatory = $false, ParameterSetName = "SharedSecret")]
        [Parameter(Mandatory = $false, ParameterSetName = "Credential")]
        [System.Management.Automation.PSCredential]
        $Credential,

        [Parameter(Mandatory = $false, ParameterSetName = "Password")]
        [Parameter(Mandatory = $false, ParameterSetName = "SharedSecret")]
        [Parameter(Mandatory = $false, ParameterSetName = "DefaultCredentials")]
        [switch]$UseDefaultCredentials,

        [Parameter(Mandatory = $false, ParameterSetName = "SharedSecret")]
        [Parameter(Mandatory = $false, ParameterSetName = "AccessKey")]
        [ValidateSet("HS256", "HS384", "HS512")]
        [string]$Algorithm = "HS256"
    )

    begin {
        $sessionId = [guid]::NewGuid()
        $session = @{
            "Username" = [string]$Username
            "Password" = [string]$Password
            "SharedSecret" = [string]$SharedSecret
            "AccessKeyId" = [string]$AccessKeyId
            "SessionId" = [string]$sessionId
            "Credential" = [System.Management.Automation.PSCredential]$Credential
            "UseDefaultCredentials" = [bool]$UseDefaultCredentials
            "Algorithm" = [string]$Algorithm
            "Connection" = @()
            "PersistentSession" = $false
            "_HttpClients" = @{}
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