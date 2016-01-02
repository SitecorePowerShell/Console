function Test-RemoteConnection {
    [CmdletBinding()]
    param(
        [Parameter(ParameterSetName='Session')]
        [ValidateNotNull()]
        [pscustomobject]$Session,

        [Parameter()]
        [int]$Timeout = 60,

        [switch]$Quiet
    )

    process {
        foreach($connection in $Session.Connection) {
            $uri = "$($connection.BaseUri.AbsoluteUri.TrimEnd('/'))/sitecore/service/keepalive.aspx?ts=$([DateTime]::Now.Ticks)&reason=default"
            $webRequest = [System.Net.WebRequest]::Create($uri)
            if($Session.Credential) {
                $webRequest.Credential = $Session.Credential
            }
            $webRequest.Timeout = 1000 * $Timeout

            try {
                $webResponse = [System.Net.HttpWebResponse]($webRequest.GetResponse())
                if($Quiet) {
                    $webResponse.StatusCode -eq [System.Net.HttpStatusCode]::OK
                } else {
                    $webResponse
                }
            } catch [System.Net.WebException] {
                if($Quiet) {
                    $false
                } else {
                    throw $_
                }
            }
        }
    }
}