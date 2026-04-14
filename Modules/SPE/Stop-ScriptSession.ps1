function Stop-ScriptSession {
    <#
        .SYNOPSIS
            Stop Sitecore PowerShell Extensions script session after a script execution.
            This command should always be executed to clean up after a session created using New-ScriptSession was used to Invoke-RemoteScript.
            If no script was executed on the server (i.e. the $session object was only used to download or upload files/items the cleanup is not necessary.

        .EXAMPLE
            The following example remotely executes a script in Sitecore using a reusable session and disposes of it afterwards

            $session = New-ScriptSession -Username admin -SharedSecret $secret -ConnectionUri https://remotesitecore
            Invoke-RemoteScript -Session $session -ScriptBlock { Get-User -id admin }
            Stop-ScriptSession -Session $session

            Name                     Domain       IsAdministrator IsAuthenticated
            ----                     ------       --------------- ---------------
            sitecore\admin           sitecore     True            False

        .EXAMPLE
            The following example runs a script as a ScriptSession job on the server (using Start-ScriptSession internally).
            The arguments are passed to the server with the help of the $Using convention.
            The results are finally returned and the job is removed after which the session is closed..

            $session = New-ScriptSession -Username admin -SharedSecret $secret -ConnectionUri https://remotesitecore
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

    	.LINK
            Wait-RemoteScriptSession

    	.LINK
            New-ScriptSession

        .LINK
            Invoke-RemoteScript
    #>

    [CmdletBinding()]
    param(

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

        [Parameter(HelpMessage="Timeout in seconds")]
        $Timeout = 30
    )

    if ($PSCmdlet.ParameterSetName -eq "Session") {
        $sd = Expand-ScriptSession -Session $Session
        $Username             = $sd.Username
        $Password             = $sd.Password
        $SharedSecret         = $sd.SharedSecret
        $AccessKeyId          = $sd.AccessKeyId
        $SessionId            = $sd.SessionId
        $Credential           = $sd.Credential
        $UseDefaultCredentials = $sd.UseDefaultCredentials
        $ConnectionUri        = $sd.ConnectionUri
        $Algorithm            = $sd.Algorithm
        $clientCache          = $sd.HttpClients
    } else {
        $SharedSecret = $null
        $UseDefaultCredentials = $false
        $Algorithm = "HS256"
        $clientCache = @{}
    }

    $cleanupUrl = "/-/script/script/?sessionId=$SessionId&action=cleanup"

    foreach ($uri in $ConnectionUri) {
        $url = $uri.AbsoluteUri.TrimEnd("/") + $cleanupUrl

        Write-Verbose -Message "Sending cleanup request to $url"
        $client = New-SpeHttpClient -Username $Username -Password $Password -SharedSecret $SharedSecret `
            -AccessKeyId $AccessKeyId -Credential $Credential -UseDefaultCredentials $UseDefaultCredentials `
            -Uri $uri -Cache $clientCache -Algorithm $Algorithm

        try {
            $content = New-Object System.Net.Http.StringContent("", [System.Text.Encoding]::UTF8, "text/plain")
            $response = $client.PostAsync($url, $content).Result
            Write-Verbose -Message "Cleanup response: $([int]$response.StatusCode)"
        } catch {
            Write-Warning "Failed to clean up session $SessionId on $uri : $_"
        }
    }
}
