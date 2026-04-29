#Requires -Version 3

Add-Type -AssemblyName System.Net.Http

function Send-RemoteItem {
    <#
        .SYNOPSIS
            Uploads a file to the filesystem on the server or media library through a Sitecore PowerShell Extensions web service.

       .PARAMETER Path
            The source path of the item to upload on the client side.

       .PARAMETER Destination
            The destination path of the item on the server side.

       .PARAMETER RootPath
            The predefined directory in which the item will be uploaded to. This is simply a keyword that maps to a predefined location on the server side.

            When using this you can simply provide the file or media item name in the Destination parameter.

       .PARAMETER SkipUnpack
            The compressed archive should not be unpacked during the upload process. This is a dynamic parameter that appears when RootPath is 'Media'.

       .PARAMETER SkipExisting
            Any existing items should not be overwritten.

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

        .EXAMPLE
            The following uploads a compressed archive to the the media library and skips unpacking.

            Compress-Archive -Path C:\temp\kittens -DestinationPath C:\temp\kittens.zip
            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
            Get-Item -Path C:\temp\kittens.zip | Send-RemoteItem @props -RootPath Media -Destination "Images/" -SkipUnpack

        .EXAMPLE
            For OAuth bearer authentication, build the session via New-ScriptSession -AccessToken
            and reuse it. The Uri-only parameter set on this cmdlet does not accept -AccessToken;
            session-scoped auth is the supported entry point for externally-issued tokens.

            $session = New-ScriptSession -ConnectionUri https://remotesitecore -AccessToken $jwt
            Send-RemoteItem -Session $session -Path "C:\temp\data.xml" -Destination "C:\temp\data1.xml"

        .LINK
            Receive-RemoteItem

        .LINK
            New-ScriptSession

        .LINK
            Stop-ScriptSession

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

        [Parameter(ParameterSetName='Uri')]
        [string]$SharedSecret,

        [Parameter(ParameterSetName='Uri')]
        [switch]$UseDefaultCredentials,

        [Parameter(Mandatory=$true, ValueFromPipeline=$true, ValueFromPipelineByPropertyName = $true)]
        [ValidateNotNullOrEmpty()]
        [Alias("FullName")]
        [string]$Path,

        [Parameter(ParameterSetName='Session')]
        [Parameter(ParameterSetName='Uri')]
        [ValidateSet("App", "Data", "Debug", "Index", "Layout", "Log", "Media", "Package", "Serialization", "Temp")]
        [ValidateNotNullOrEmpty()]
        [string]$RootPath,

        [string]$Destination
    )

    dynamicparam {
         if ($RootPath -eq "Media") {
              $paramDictionary = New-Object System.Management.Automation.RuntimeDefinedParameterDictionary

              $skipUnpackAttribute = New-Object System.Management.Automation.ParameterAttribute
              $skipUnpackAttributeCollection = New-Object System.Collections.ObjectModel.Collection[System.Attribute]
              $skipUnpackAttributeCollection.Add($skipUnpackAttribute)
              $skipUnpackParam = New-Object System.Management.Automation.RuntimeDefinedParameter('SkipUnpack', [switch], $skipUnpackAttributeCollection)
              $paramDictionary.Add('SkipUnpack', $skipUnpackParam)

              $skipExistingAttribute = New-Object System.Management.Automation.ParameterAttribute
              $skipExistingAttributeCollection = New-Object System.Collections.ObjectModel.Collection[System.Attribute]
              $skipExistingAttributeCollection.Add($skipExistingAttribute)
              $skipExistingParam = New-Object System.Management.Automation.RuntimeDefinedParameter('SkipExisting', [switch], $skipExistingAttributeCollection)
              $paramDictionary.Add('SkipExisting', $skipExistingParam)
              return $paramDictionary
        }
    }

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
            $sd = Expand-ScriptSession -Session $Session
            $Username             = $sd.Username
            $Password             = $sd.Password
            $SharedSecret         = $sd.SharedSecret
            $AccessKeyId          = $sd.AccessKeyId
            $AccessToken          = $sd.AccessToken
            $Credential           = $sd.Credential
            $UseDefaultCredentials = $sd.UseDefaultCredentials
            $ConnectionUri        = $sd.ConnectionUri
            $Algorithm            = $sd.Algorithm
            $clientCache          = $sd.HttpClients
        } else {
            $AccessToken = ""
            $Algorithm = "HS256"
            $clientCache = @{}
        }

        $serviceUrl = "/-/script"

        if($isMediaItem) {
            $serviceUrl += "/media/master/" + $output + "/?"
        } else {
            # When the caller passes a rooted Destination without -RootPath, route
            # the request through origin=custom so the server-side allowlist
            # (Spe.Remoting.AllowedFileRoots) decides whether to honor it.
            $rootSegment = if ([string]::IsNullOrEmpty($RootPath)) { "custom" } else { $RootPath }
            $serviceUrl += "/file/" + $rootSegment + "/?path=" + $output + "&"
        }

        if($PSBoundParameters.SkipUnpack.IsPresent) {
            $serviceUrl += "&skipunpack=true"
        }
        if($PSBoundParameters.SkipExisting.IsPresent) {
            $serviceUrl += "&skipexisting=true"
        }

        foreach($uri in $ConnectionUri) {

            # http://hostname/-/script/type/origin/location
            $url = $uri.AbsoluteUri.TrimEnd("/") + $serviceUrl

            Write-Verbose -Message "Preparing to upload local item to the remote url $($url)"
            $client = New-SpeHttpClient -Username $Username -Password $Password -SharedSecret $SharedSecret `
                -AccessKeyId $AccessKeyId -AccessToken $AccessToken -Credential $Credential -UseDefaultCredentials $UseDefaultCredentials `
                -Uri $uri -Cache $clientCache -Algorithm $Algorithm

            try {
                Write-Verbose -Message "Uploading $($Path)"
                [System.Net.HttpWebResponse]$errorResponse = $null

                $fileStream = ([System.IO.FileInfo] (Get-Item -Path $Path)).OpenRead()
                $content = New-Object System.Net.Http.StreamContent($fileStream)
                $responseMessage = $client.PostAsync($url, $content).Result
                $fileStream.Close()
                $fileStream.Dispose()

                if (-not $responseMessage.IsSuccessStatusCode) {
                    Write-Error "Upload failed. $($responseMessage.ReasonPhrase)"
                    return
                }

                Write-Verbose -Message "Upload complete."
            } catch [System.Net.WebException] {
                [System.Net.WebException]$script:ex = $_.Exception
                [System.Net.HttpWebResponse]$script:errorResponse = $ex.Response
                Write-RemoteHttpResponseVerbose -Response $errorResponse -Exception $ex
            }

            if ($errorResponse) {
                Write-Error -Message "Server response: $($errorResponse.StatusDescription)" -Category ConnectionError `
                    -CategoryActivity "Upload" -CategoryTargetName $uri -Exception ($script:ex) -CategoryReason "$($errorResponse.StatusCode)" -CategoryTargetType $RootPath
            }
        }
    }
}
