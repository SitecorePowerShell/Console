#Requires -Version 3

function Receive-RemoteItem {
    <#
        .SYNOPSIS
            Downloads a file or media item through a Sitecore PowerShell Extensions web service.

       .PARAMETER Path
            The source path of the item to download on the server side.
            
       .PARAMETER Destination
            The destination path of the item on the client side.
            
       .PARAMETER RootPath
            The predefined directory in which the item will be downloaded from. This is simply a keyword that maps to a predefined location on the server side. 
            
            When using this you can simply provide the file or media item name in the Path parameter.    
            
       .EXAMPLE
            The following downloads an item from the media library in the master db and overwrite any existing version.
    
            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
            Receive-RemoteItem -Session $session -Path "/Default Website/cover" -Destination "C:\Images\" -Database master -Force
    
       .EXAMPLE
            The following downloads an item from the media library using the Id in the master db and uses the specified name.
    
            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
            Receive-RemoteItem -Session $session -Path "{04DAD0FD-DB66-4070-881F-17264CA257E1}" -Destination "C:\Images\cover1.jpg" -Database master -Force
    
        .EXAMPLE
            The following downloads all the items from the media library in the specified path.
    
            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
            Invoke-RemoteScript -Session $session -ScriptBlock { 
                Get-ChildItem -Path "master:/sitecore/media library/" -Recurse | 
                    Where-Object { $_.Size -gt 0 } | Select-Object -Expand ItemPath 
            } | Receive-RemoteItem -Destination "C:\Images\" -Database master
            Stop-ScriptSession -Session $session

        .EXAMPLE
            The following downloads a file from the application root path.

            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
            Receive-RemoteItem -Session $session -Path "default.js" -RootPath App -Destination "C:\Files\"
            Stop-ScriptSession -Session $session
              
        .EXAMPLE
            The following compresses the log files into an archive and downloads from the absolute path.

            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
            Invoke-RemoteScript -Session $session -ScriptBlock {
                Import-Function -Name Compress-Archive
                Get-ChildItem -Path "$($SitecoreLogFolder)" | Where-Object { !$_.PSIsContainer } | 
                    Compress-Archive -DestinationPath "$($SitecoreDataFolder)archived.zip" -Recurse | Select-Object -Expand FullName
            } | Receive-RemoteItem -Session $session -Destination "C:\Files\"
    
        .LINK
            Send-RemoteItem

        .LINK
            New-ScriptSession

        .LINK
            Stop-ScriptSession


    #>
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

        [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
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

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]$Destination,

        [Parameter()]
        [switch]$Force,

        #[Parameter(ParameterSetName='Session and File')]
        [Parameter(ParameterSetName='Session and Database')]
        #[Parameter(ParameterSetName='Uri and File')]
        [Parameter(ParameterSetName='Uri and Database')]
        [switch]$Container
    )

    process {

        $isMediaItem = ![string]::IsNullOrEmpty($Database) -or $RootPath -ieq "media"
        
        if($isMediaItem -and !$Database){
            $Database = "master"
        }
        
        if(!$isMediaItem -and (!$RootPath -and ![System.IO.Path]::IsPathRooted($Path))) {
            Write-Error -Message "RootPath is required when Path is not fully qualified." -ErrorAction Stop
        }

        $Path = $Path.TrimEnd('\','/')

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

            [System.Net.HttpWebResponse]$script:errorResponse = $null
            [byte[]]$response = & {
                try {
                    $script:errorResponse = $null
                    $script:ex = $null
                    $webclient.DownloadData($url)
                } catch [System.Net.WebException] {
                    [System.Net.WebException]$script:ex = $_.Exception
                    [System.Net.HttpWebResponse]$script:errorResponse = $script:ex.Response
                    Write-Verbose -Message "Response exception message: $($ex.Message)"
                    Write-Verbose -Message "Response status description: $($errorResponse.StatusDescription)"
                    if($errorResponse.StatusCode -eq [System.Net.HttpStatusCode]::Forbidden) {
                        Write-Verbose -Message "Check that the proper credentials are provided and that the service configurations are enabled."
                    } elseif ($errorResponse.StatusCode -eq [System.Net.HttpStatusCode]::NotFound){
                        Write-Verbose -Message "Check that the service files exist and are properly configured."
                    }
                }
            }

            if($errorResponse){
                Write-Error -Message "Server responded with error: $($errorResponse.StatusDescription)" -Category ConnectionError `
                    -CategoryActivity "Download" -CategoryTargetName $uri -Exception ($script:ex) -CategoryReason "$($errorResponse.StatusCode)" -CategoryTargetType $RootPath 
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