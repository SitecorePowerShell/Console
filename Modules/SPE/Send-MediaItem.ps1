function Send-MediaItem {
    <#
        .SYNOPSIS
            Uploads an item to the media library in Sitecore PowerShell Extensions via web service calls.
    
       .EXAMPLE
            The following uploads all of the images in a directory to the specified path in the media library in the master db.
            
            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
            Get-ChildItem -Path C:\Images | Send-MediaItem -Session $session -Destination "/sitecore/media library/Images/"
    
        .EXAMPLE
            The following uploads a single image with a new name to the specified path in the media library in the master db.
    
            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
            Send-MediaItem -Session $session -Path C:\Images\banner.jpg -Destination "/sitecore/media library/Images/banner.jpg"
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

        [Parameter(Position=0, Mandatory=$true, ValueFromPipeline=$true, ValueFromPipelineByPropertyName=$true)]
        [Alias("FullName")]
        [ValidateNotNullOrEmpty()]
        [string]$Path,
        
        [Parameter(Position=1, Mandatory=$true)]
        [Alias("RemotePath")]
		[ValidateNotNullOrEmpty()]
        [String]$Destination,

        [Parameter(Position=2)]
        [String]$Database = "master",

        [Parameter(Position=3)]
        [String]$Language = "en"
    )

    process {
        
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

            $output = $Destination
            $extension = [System.IO.Path]::GetExtension($Path)
            if(!$output.EndsWith($extension)) {
                if(!$output.EndsWith("/") -and !$output.EndsWith("\")) {
                    $output += "/"
                }

                $output += [System.IO.Path]::GetFileName($Path)
            }
	
            [byte[]]$bytes = [System.IO.File]::ReadAllBytes($Path)

            if($bytes -and $bytes.Length -gt 0) {

                Write-Verbose -Message "Uploading $($Path)"
                $singleConnection.Proxy.UploadFile($Username, $Password, $output, $bytes, $Database, $Language) | Out-Null

                Write-Verbose -Message "Upload complete."
            } else {
                Write-Verbose -Message "Upload failed. No content to send to the web service."
            }
        }
    }
}