#Requires -Version 3

function Get-FileExtension {
    param(
        [ValidateNotNullOrEmpty()]
        [byte[]]$InputObject,

        [ValidateNotNullOrEmpty()]
        [string]$ContentType
    )
        
    $extension = ""
        
    Write-Verbose "The destination path is missing a file extension. Attempting to figure that out now."

    switch($ContentType) {
        "application/json" { $extension = ".json" }
        "application/pdf" { $extension = ".pdf" }
        "application/x-javascript" { $extension = ".js" }
        "application/x-zip-compressed" {  $extension = ".zip" }
        "image/bmp" { $extension = ".bmp" }
        "image/gif" { $extension = ".gif" }
        "image/jpeg" { $extension = ".jpg" }
        "image/png" {  $extension = ".png" }
        "image/svg+xml" {  $extension = ".svg" }
        "text/css" {  $extension = ".css" }
        "text/html" {  $extension = ".html" }
        "text/plain" {  $extension = ".txt" }
        "text/xml" {  $extension = ".xml" }
    }
        
    $extension
}

function Receive-RemoteItem {
    [CmdletBinding(DefaultParameterSetName='Uri and File')]
    param(
        [Parameter(Mandatory=$true, ParameterSetName='Session and File')]
        [Parameter(Mandatory=$true, ParameterSetName='Session and Database')]
        [ValidateNotNull()]
        [pscustomobject]$Session,
        
        [Parameter(Mandatory=$true, ParameterSetName='Uri and File')]
        [Parameter(Mandatory=$true, ParameterSetName='Uri and Database')]
        [Uri[]]$ConnectionUri,

        [Parameter(ParameterSetName='Uri and File')]
        [Parameter(ParameterSetName='Uri and Database')]
        [string]$Username,

        [Parameter(ParameterSetName='Uri and File')]
        [Parameter(ParameterSetName='Uri and Database')]
        [string]$Password,

        [Parameter(ParameterSetName='Uri and File')]
        [Parameter(ParameterSetName='Uri and Database')]
        [System.Management.Automation.PSCredential]
        $Credential,

        [Parameter(Mandatory=$true, ValueFromPipeline=$true, ParameterSetName='Session and File')]
        [Parameter(Mandatory=$true, ValueFromPipeline=$true, ParameterSetName='Session and Database')]
        [Parameter(Mandatory=$true, ValueFromPipeline=$true, ParameterSetName='Uri and File')]
        [Parameter(Mandatory=$true, ValueFromPipeline=$true, ParameterSetName='Uri and Database')]
        [ValidateNotNullOrEmpty()]
        [string]$Path,

        [Parameter(ParameterSetName='Session and File')]
        [Parameter(ParameterSetName='Uri and File')]
        [ValidateSet("App", "Data", "Debug", "Index", "Layout", "Log", "Media", "Package", "Serialization", "Temp")]
        [ValidateNotNullOrEmpty()]
        [string]$RootPath,

        [Parameter(Mandatory=$true, ParameterSetName='Session and Database')]
        [Parameter(Mandatory=$true, ParameterSetName='Uri and Database')]
        [ValidateNotNullOrEmpty()]
        [string]$Database,

        [Parameter(Mandatory=$true, ParameterSetName='Session and File')]
        [Parameter(Mandatory=$true, ParameterSetName='Session and Database')]
        [Parameter(Mandatory=$true, ParameterSetName='Uri and File')]
        [Parameter(Mandatory=$true, ParameterSetName='Uri and Database')]
        [ValidateNotNullOrEmpty()]
        [string]$Destination,

        [Parameter(ParameterSetName='Session and File')]
        [Parameter(ParameterSetName='Session and Database')]
        [Parameter(ParameterSetName='Uri and File')]
        [Parameter(ParameterSetName='Uri and Database')]
        [switch]$Force,

        #[Parameter(ParameterSetName='Session and File')]
        [Parameter(ParameterSetName='Session and Database')]
        #[Parameter(ParameterSetName='Uri and File')]
        [Parameter(ParameterSetName='Uri and Database')]
        [switch]$Container
    )

    process {

        if(!$RootPath -and ![System.IO.Path]::IsPathRooted($Path)) {
            Write-Error -Message "RootPath is required when Path is not fully qualified." -ErrorAction Stop
        }

        $Path = $Path.TrimEnd('\','/')
        $isMediaItem = ![string]::IsNullOrEmpty($Database)

        if($Session) {
            $Username = $Session.Username
            $Password = $Session.Password
            $Credential = $Session.Credential
            $ConnectionUri = $Session | ForEach-Object { $_.Connection.BaseUri }
        }

        $itemType = "undetermined"

        $serviceUrl = "/-/script"

        if($isMediaItem) {
            $serviceUrl += "/media/" + $Database + "/" + $Path + "/?"
            $itemType = "media"
        } else {
            $serviceUrl += "/file/" + $RootPath + "/?path=" + $Path + "&"
            $itemType = "file"
        }

        $serviceUrl += "user=" + $Username + "&password=" + $Password

        foreach($uri in $ConnectionUri) {
            
            # http://hostname/-/script/type/origin/location
            $url = $uri.AbsoluteUri.TrimEnd("/") + $serviceUrl

            Write-Verbose -Message "Preparing to download remote item from the url $($url)"
            Write-Verbose -Message "Downloading the $($itemType) item $($Path)"
            $webclient = New-Object System.Net.WebClient
            
            if($Credential) {
                $webclient.Credentials = $Credential
            }

            [byte[]]$response = & {
                try {
                    $webclient.DownloadData($url)
                } catch [System.Net.WebException] {
                    [System.Net.WebException]$ex = $_.Exception
                    [System.Net.HttpWebResponse]$errorResponse = $ex.Response
                    Write-Verbose -Message "Response status description: $($errorResponse.StatusDescription)"
                }
            }

            if($response -and $response.Length -gt 0) {
                $contentType = $webclient.ResponseHeaders["Content-Type"]
                $contentDisposition = $webclient.ResponseHeaders["Content-Disposition"]
                $filename = ""
                Write-Verbose -Message "Response content length: $($response.Length) bytes"
                Write-Verbose -Message "Response content type: $($contentType)"
                
                if($contentDisposition.IndexOf("filename=") -gt -1) {
                    $filename = $contentDisposition.Substring($contentDisposition.IndexOf("filename=") + 10).Replace('"', "")
                    Write-Verbose -Message "Response filename: $($filename)"
                }
     
                $directory = [System.IO.Path]::GetDirectoryName($Destination)
                if(!$directory) {
                    $directory = $Destination
                }
                
                if(!(Test-Path $directory -PathType Container)) {
                    Write-Verbose "Creating a new directory $($directory)"
                    New-Item -ItemType Directory -Path $directory | Out-Null
                }

                $output = $Destination
                if($Container) {
                    # Preserve Media Item directory structure
                    if($isMediaItem) {
                        $output = Join-Path -Path $output -ChildPath $Path.Replace('/','\').Replace("\sitecore\media library\", "")
                    }
                }

                $extension = [System.IO.Path]::GetExtension($output)
                if(!$extension) {
                    $extension = [System.IO.Path]::GetExtension($filename)

                    if(!$extension) {
                        Write-Error -Message "The file extension could not be determined."
                    }
                    
                    # If a media item is requested it will use the filename as the last part.
                    $output = $output.TrimEnd([System.IO.Path]::GetFileNameWithoutExtension($Path))
                    $output = Join-Path -Path $output -ChildPath $filename
                    
                } else {
                    Write-Verbose "Overriding the filename $($filename) with $([System.IO.Path]::GetFileName($output))"
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