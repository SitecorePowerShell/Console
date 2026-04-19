#Requires -Version 3

Add-Type -AssemblyName System.Net.Http

# Derives the output file path from the destination + Content-Disposition filename.
# Pulled out of Receive-RemoteItem to flatten the per-download branch.
function Resolve-ReceivedFilePath {
    param(
        [System.Net.Http.HttpResponseMessage]$ResponseMessage,
        [string]$Destination,
        [string]$Path,
        [bool]$Container,
        [bool]$IsMediaItem
    )

    $contentDisposition = $ResponseMessage.Content.Headers.GetValues("Content-Disposition")[0]
    $filename = ""
    if ($contentDisposition.IndexOf("filename=") -gt -1) {
        $filename = $contentDisposition.Substring($contentDisposition.IndexOf("filename=") + 10).Replace('"', "")
        Write-Verbose -Message "Response filename: $filename"
    }

    $directory = [System.IO.Path]::GetDirectoryName($Destination)
    if (-not $directory) { $directory = $Destination }
    if (-not (Test-Path $directory -PathType Container)) {
        Write-Verbose "Creating a new directory $directory"
        New-Item -ItemType Directory -Path $directory | Out-Null
    }

    $output = $Destination
    if ($Container -and $IsMediaItem) {
        # Preserve Media Item directory structure.
        $output = Join-Path -Path $output -ChildPath $Path.Replace('/','\').Replace("\sitecore\media library\", "")
    }

    # Destination already has an extension -> use it directly.
    if ([System.IO.Path]::GetExtension($output)) {
        Write-Verbose "Overriding the filename $filename with $([System.IO.Path]::GetFileName($output))"
        return $output
    }

    if (-not [System.IO.Path]::GetExtension($filename)) {
        Write-Error -Message "The file extension could not be determined."
    }

    # Media items addressed by name suffix the filename-without-extension onto
    # $output; strip that before joining the real filename.
    $filenameWithoutExtension = [System.IO.Path]::GetFileNameWithoutExtension($Path)
    if ($output.EndsWith($filenameWithoutExtension)) {
        $output = $output.Substring(0, $output.Length - $filenameWithoutExtension.Length)
    }
    return (Join-Path -Path $output -ChildPath $filename)
}

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

        $isMediaItem = ![string]::IsNullOrEmpty($Database) -or ($RootPath -eq "media" -and ![System.IO.Path]::HasExtension($Path))

        if($isMediaItem -and !$Database){
            $Database = "master"
        }

        if(!$isMediaItem -and (!$RootPath -and ![System.IO.Path]::IsPathRooted($Path))) {
            Write-Error -Message "RootPath is required when Path is not fully qualified." -ErrorAction Stop
        }

        $Path = $Path.TrimEnd('\','/')

        if($Session) {
            $sd = Expand-ScriptSession -Session $Session
            $Username             = $sd.Username
            $Password             = $sd.Password
            $SharedSecret         = $sd.SharedSecret
            $AccessKeyId          = $sd.AccessKeyId
            $Credential           = $sd.Credential
            $UseDefaultCredentials = $sd.UseDefaultCredentials
            $ConnectionUri        = $sd.ConnectionUri
            $Algorithm            = $sd.Algorithm
            $clientCache          = $sd.HttpClients
        } else {
            $Algorithm = "HS256"
            $clientCache = @{}
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

        foreach($uri in $ConnectionUri) {

            # http://hostname/-/script/type/origin/location
            $url = $uri.AbsoluteUri.TrimEnd("/") + $serviceUrl

            Write-Verbose -Message "Preparing to download remote item from the url $($url)"
            Write-Verbose -Message "Downloading the $($itemType) item $($Path)"
            $client = New-SpeHttpClient -Username $Username -Password $Password -SharedSecret $SharedSecret `
                -AccessKeyId $AccessKeyId -Credential $Credential -UseDefaultCredentials $UseDefaultCredentials `
                -Uri $uri -Cache $clientCache -Algorithm $Algorithm

            $errorResponse = $null
            $ex = $null
            [System.Net.Http.HttpResponseMessage]$responseMessage = $null
            $response = $null

            try {
                $responseMessage = $client.GetAsync($url).Result
                [byte[]]$response = $responseMessage.Content.ReadAsByteArrayAsync().Result
            } catch [System.Net.WebException] {
                [System.Net.WebException]$ex = $_.Exception
                [System.Net.HttpWebResponse]$errorResponse = $ex.Response
                Write-RemoteHttpResponseVerbose -Response $errorResponse -Exception $ex
            }

            if ($errorResponse) {
                Write-Error -Message "Server response: $($errorResponse.StatusDescription)" -Category ConnectionError `
                    -CategoryActivity "Download" -CategoryTargetName $uri -Exception $ex -CategoryReason "$($errorResponse.StatusCode)" -CategoryTargetType $RootPath
            }

            # Guard: empty response body.
            $hasBody = ($response -and $response.Length -gt 0) -or $responseMessage.Content.Headers.Count -gt 0
            if (-not $hasBody) {
                Write-Verbose -Message "Download failed. No content returned from the web service."
                continue
            }

            if (-not $responseMessage.IsSuccessStatusCode) {
                Write-Error "Download failed. $($responseMessage.ReasonPhrase)"
                return
            }

            $contentType = $responseMessage.Content.Headers.GetValues("Content-Type")[0]
            Write-Verbose -Message "Response content length: $($response.Length) bytes"
            Write-Verbose -Message "Response content type: $($contentType)"

            $output = Resolve-ReceivedFilePath -ResponseMessage $responseMessage `
                -Destination $Destination -Path $Path -Container $Container.IsPresent -IsMediaItem $isMediaItem

            if ((Test-Path $output -PathType Leaf) -and -not $Force.IsPresent) {
                Write-Verbose "Skipping the save of $($output) because it already exists."
                Write-Verbose "Download complete."
                continue
            }

            Write-Verbose "Creating a new file $output"
            New-Item -Path $output -ItemType File -Force | Out-Null
            if ($response) {
                [System.IO.File]::WriteAllBytes((Convert-Path -Path $output), $response)
            }
            Write-Verbose "Download complete."
        }
    }
}