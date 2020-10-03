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
            The following example tests connectivity with the session host and returns an object with Sitecore details or $null results.

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

        [switch]$Quiet
    )

    process {
        $result = Invoke-RemoteScript -ScriptBlock {
            [PSCustomObject]@{
                "SPEVersion" = $PSVersionTable.SPEVersion
                "SitecoreVersion" = [SitecoreVersion]::Current.ToString()
                "CurrentTime" = [datetime]::UtcNow
            }
        } -Session $Session

        $isSuccess = $result -ne $null
        if($Quiet) {
            $isSuccess
        } else {
            $result
        }
    }
}