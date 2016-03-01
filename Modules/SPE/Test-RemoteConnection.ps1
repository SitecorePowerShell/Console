function Test-RemoteConnection {
        <#
        .SYNOPSIS
            Requests the keepalive.aspx page from the server to test connectivity.

        .DESCRIPTON
            The Test-RemoteConnection command submits a web request to the specified host to both warmup and test connectivity.
    
        .PARAMETER Timeout
            The time to way before aborting in seconds.

        .PARAMETER Quiet
            The command should return a true or false value indicating connectivity.
        
        .EXAMPLE
            The following example tests connectivity with the session host and returns a true or false value.
            
            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
            Test-RemoteConnection -Session $session -Quiet
    
        .EXAMPLE
            The following example tests connectivity with the session host and returns the System.Net.HttpWebResponse object or System.Net.WebException error message.

            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
            Test-RemoteConnection -Session $session

    	.LINK
            New-ScriptSession
    #>    
    
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