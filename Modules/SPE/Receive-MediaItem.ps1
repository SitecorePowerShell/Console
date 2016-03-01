function Receive-MediaItem {
    <#
        .SYNOPSIS
            Downloads an item from the media library in Sitecore PowerShell Extensions via web service calls.
    
       .EXAMPLE
            The following downloads an item from the media library in the master db and dynamically detects the file extension.
            Existing files will be deleted automatically.
    
            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
            Receive-MediaItem -Session $session -Path "/sitecore/media library/Images/Icons/accuracy" -Destination C:\Images\ -Force
    
       .EXAMPLE
            The following downloads an item from the media library in the master db and uses the specified name.
    
            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
            Receive-MediaItem -Session $session -Path "/sitecore/media library/Images/Icons/accuracy" -Destination C:\Images\accuracy2.jpg -Force
    
        .EXAMPLE
            The following downloads all the items from the media library in the specified path.
    
            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
            Invoke-RemoteScript -Session $session -ScriptBlock { 
                Get-ChildItem -Path "master:/sitecore/media library/Images/Icons/" | Select-Object -Expand ItemPath 
            } | Receive-MediaItem -Session $session -Destination C:\Temp\Images\
            Stop-ScriptSession -Session $session
    #>
    [CmdletBinding()]
    param(
        [Parameter(ParameterSetName='Session')]
        [ValidateNotNull()]
        [pscustomobject]$Session,

        [Parameter(ParameterSetName='Uri')]
        [string[]]$ConnectionUri,

        [Parameter(ParameterSetName='Uri')]
        [string]$SessionId,

        [Parameter(ParameterSetName='Uri')]
        [string]$Username,

        [Parameter(ParameterSetName='Uri')]
        [string]$Password,

        [Parameter(ParameterSetName='Uri')]
        [System.Management.Automation.PSCredential]
        $Credential,

        [Parameter(Position=0, Mandatory=$true, ValueFromPipeline=$true)]
        [ValidateNotNullOrEmpty()]
        [string]$Path,
        
        [Parameter(Position=1, Mandatory=$true)]
        [Alias("RemotePath")]
		[ValidateNotNullOrEmpty()]
        [string]$Destination,

        [Parameter(Position=2)]
        [string]$Database = "master",

        [Parameter(Position=3)]
        [string]$Language = "en",

        [Parameter()]
        [switch]$Force
    )
    
    begin {
        function Get-ImageExtension {
            param(
                [ValidateNotNullOrEmpty()]
                [byte[]]$ImageData
            )
        
            $extension = ".jpg"
        
            Write-Verbose "The destination path is missing a file extension. Attempting to figure that out now."
            $memoryStream = New-Object System.IO.MemoryStream
            $memoryStream.Write($ImageData, 0, $ImageData.Length)
            $image = [System.Drawing.Image]::FromStream($memoryStream)
            switch($image.RawFormat.Guid) {
                "b96b3cab-0728-11d3-9d7b-0000f81ef32e" {
                    $extension = ".bmp"
                    break
                }
                "b96b3cb0-0728-11d3-9d7b-0000f81ef32e" {
                    $extension = ".gif"
                    break
                }
                "b96b3cae-0728-11d3-9d7b-0000f81ef32e" {
                    $extension = ".jpg"
                    break
                }
                "b96b3caa-0728-11d3-9d7b-0000f81ef32e" {
                    $extension = ".bmp"
                    break
                }
                "b96b3caf-0728-11d3-9d7b-0000f81ef32e" {
                    $extension = ".png"
                    break
                }
            }
            $memoryStream.Dispose()
            $image.Dispose()
        
            $extension
        }
    }

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

            Write-Verbose -Message "Downloading $($Path)"
            [byte[]]$response = $singleConnection.Proxy.DownloadFile($Username, $Password, $Path, $Database, $Language)
	
            if($response -and $response.Length -gt 0) {
                
                $directory = [System.IO.Path]::GetDirectoryName($Destination)
                if(!$directory) {
                    $directory = $Destination
                }
                
                if(!(Test-Path $directory -PathType Container)) {
                    Write-Verbose "Creating a new directory $($directory)"
                    New-Item -ItemType Directory -Path $directory | Out-Null
                }

                $output = $Destination

                $extension = [System.IO.Path]::GetExtension($output)
                if(!$extension) {
                    $extension = Get-ImageExtension -ImageData $response

                    $name = [System.IO.Path]::GetFileName($Path.TrimEnd('\','/'))
                    $output = Join-Path -Path $output -ChildPath ($name + $extension)
                }
                
                if(-not(Test-Path $output -PathType Leaf) -or $Force.IsPresent) {
                    Write-Verbose "Creating a new file $($output)"
                    New-Item -Path $output -ItemType File -Force | Out-Null
                    [System.IO.File]::WriteAllBytes((Convert-Path -Path $output), $response)
                } else {
                    Write-Verbose "Skipping the save of $($output) because it already exists."
                }
                
                Write-Verbose "Download complete."
            } else {
                Write-Verbose -Message "Download failed. No content returned from the web service."
            }
        }
    }
}