function Test-RemoteConnection {
    <#
        .SYNOPSIS
            Tests connectivity with a remote Sitecore instance and returns server info.

        .DESCRIPTION
            The Test-RemoteConnection command sends a lightweight request to the server
            to verify connectivity and retrieve SPE/Sitecore version information.
            No PowerShell script is executed on the server.

        .PARAMETER Session
            The session object created by New-ScriptSession.

        .PARAMETER Quiet
            The command should return a true or false value indicating connectivity.

        .EXAMPLE
            The following example tests connectivity with the session host and returns a true or false value.

            $session = New-ScriptSession -Username admin -SharedSecret $secret -ConnectionUri https://remotesitecore
            Test-RemoteConnection -Session $session -Quiet

        .EXAMPLE
            The following example tests connectivity with the session host and returns an object with Sitecore details or $null results.

            $session = New-ScriptSession -Username admin -SharedSecret $secret -ConnectionUri https://remotesitecore
            Test-RemoteConnection -Session $session

        .LINK
            New-ScriptSession
    #>

    [CmdletBinding()]
    param(
        [Parameter(ParameterSetName='Session')]
        [ValidateNotNull()]
        [pscustomobject]$Session,

        [switch]$Quiet
    )

    process {
        $sd = Expand-ScriptSession -Session $Session
        $testUrl = "/-/script/script/?action=test"

        $result = $null
        foreach ($uri in $sd.ConnectionUri) {
            $url = $uri.AbsoluteUri.TrimEnd("/") + $testUrl

            $client = New-SpeHttpClient -Username $sd.Username -Password $sd.Password -SharedSecret $sd.SharedSecret `
                -AccessKeyId $sd.AccessKeyId -Credential $sd.Credential -UseDefaultCredentials $sd.UseDefaultCredentials `
                -Uri $uri -Cache $sd.HttpClients -Algorithm $sd.Algorithm

            try {
                $content = New-Object System.Net.Http.StringContent("", [System.Text.Encoding]::UTF8, "text/plain")
                $response = $client.PostAsync($url, $content).Result
                if ($response.IsSuccessStatusCode) {
                    $body = $response.Content.ReadAsStringAsync().Result
                    $result = $body | ConvertFrom-Json
                }
            } catch {
                Write-Verbose "Connection test failed for $uri : $_"
            }
        }

        $isSuccess = $null -ne $result
        if ($Quiet) {
            $isSuccess
        } else {
            $result
        }
    }
}
