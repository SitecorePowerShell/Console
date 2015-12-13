#Requires -Version 3

function Send-RemoteItem {
    <#
        .SYNOPSIS
            Uploads a file to the filesystem on the server or media library through a Sitecore PowerShell Extensions web service.
    
       .EXAMPLE
            The following uploads a file to the root application path.
    
            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
            Get-Item -Path C:\temp\data.xml | Send-RemoteItem @props -RootPath App
    
       .EXAMPLE
            The following uploads the image to the media library with the specified name.
    
            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
            Get-Item -Path C:\image.png | Send-RemoteItem -Session $session -RootPath Media -Destination "Images/image2.png"
    
        .EXAMPLE
            The following uploads the image to the media library updating the item with the specified Id..
    
            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
            Get-Item -Path C:\temp\cover.jpg | Send-RemoteItem -Session $session -Destination "{04DAD0FD-DB66-4070-881F-17264CA257E1}"

        .EXAMPLE
            The following uploads a file to the specified absolute path.

            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
            Send-RemoteItem -Session $session -Path "C:\temp\data.xml" -Destination "C:\inetpub\wwwroot\Console\Website\upload\data1.xml"
    #>
    [CmdletBinding(DefaultParameterSetName='Uri')]
    param(
        [Parameter(Mandatory=$true, ParameterSetName='Session')]
        [ValidateNotNull()]
        [pscustomobject]$Session,
        
        [Parameter(ParameterSetName='Uri')]
        [Uri[]]$ConnectionUri,

        [Parameter(ParameterSetName='Uri')]
        [string]$Username,

        [Parameter(ParameterSetName='Uri')]
        [string]$Password,

        [Parameter(ParameterSetName='Uri')]
        [System.Management.Automation.PSCredential]
        $Credential,

        [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
        [ValidateNotNullOrEmpty()]
        [string]$Path,

        [Parameter(ParameterSetName='Session')]
        [Parameter(ParameterSetName='Uri')]
        [ValidateSet("App", "Data", "Debug", "Index", "Layout", "Log", "Media", "Package", "Serialization", "Temp")]
        [ValidateNotNullOrEmpty()]
        [string]$RootPath,

        [string]$Destination
    )

    process {
        
        $mediaId = [guid]::Empty
        $isMediaItem = $RootPath -eq "Media" -or [guid]::TryParse($Destination, [ref]$mediaId)
        
        if(!$isMediaItem -and (!$RootPath -and ![System.IO.Path]::IsPathRooted($Destination))) {
            Write-Error -Message "RootPath is required when Destination is not fully qualified." -ErrorAction Stop
        }

        $Destination = $Destination.TrimEnd('\','/')

        $output = $Destination

        if($mediaId -eq [guid]::Empty) {
            $extension = [System.IO.Path]::GetExtension($Path)
            if(!$output.EndsWith($extension)) {
                if(!$output.EndsWith("/") -and !$output.EndsWith("\")) {
                    $output += "/"
                }

                $output += [System.IO.Path]::GetFileName($Path)
            }
        }

        if($Session) {
            $Username = $Session.Username
            $Password = $Session.Password
            $Credential = $Session.Credential
            $ConnectionUri = $Session | ForEach-Object { $_.Connection.BaseUri }
        }

        $serviceUrl = "/-/script"

        if($isMediaItem) {
            $serviceUrl += "/media/master/" + $output + "/?"
        } else {
            $serviceUrl += "/file/" + $RootPath + "/?path=" + $output + "&"
        }

        $serviceUrl += "user=" + $Username + "&password=" + $Password

        $data = [System.IO.File]::ReadAllBytes($Path)

        if(!$data -or $data.Length -le 0) {
            Write-Verbose -Message "Upload failed. No content to send to the web service."
            return
        }

        foreach($uri in $ConnectionUri) {
            
            # http://hostname/-/script/type/origin/location
            $url = $uri.AbsoluteUri.TrimEnd("/") + $serviceUrl

            Write-Verbose -Message "Preparing to upload local item to the remote url $($url)"
            $webclient = New-Object System.Net.WebClient
            
            if($Credential) {
                $webclient.Credentials = $Credential
            }

            [byte[]]$response = & {
                try {
                    Write-Verbose -Message "Uploading $($Path)"
                    $webclient.UploadData($url, $data)
                    Write-Verbose -Message "Upload complete."
                } catch [System.Net.WebException] {
                    [System.Net.WebException]$ex = $_.Exception
                    [System.Net.HttpWebResponse]$errorResponse = $ex.Response
                    Write-Verbose -Message "Response status description: $($errorResponse.StatusDescription)"
                }
            }
        }
    }
}