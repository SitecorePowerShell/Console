#Requires -Version 3

function Send-RemoteItem {
    <#
        .SYNOPSIS
            Downloads a file or media item through a Sitecore PowerShell Extensions web service.
    
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

        .EXAMPLE
            The following downloads a file from the application root path.

            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
            Receive-RemoteItem -Session $session -Path "default.js" -RootPath App -Destination "C:\Files\"
              
        .EXAMPLE
            The following compresses the log files into an archive and downloads from the absolute path.

            $session = New-ScriptSession -Username admin -Password b -ConnectionUri http://remotesitecore
            Invoke-RemoteScript -Session $session -ScriptBlock {
                Import-Function -Name Compress-Archive
                Get-ChildItem -Path "$($SitecoreLogFolder)" | Where-Object { !$_.PSIsContainer } | 
                    Compress-Archive -DestinationPath "$($SitecoreDataFolder)archived.zip" -Recurse | Select-Object -Expand FullName
            } | Receive-RemoteItem -Session $session -Destination "C:\Files\"
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
        [string]$Database = "master",

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

        $isMediaItem = $RootPath -eq "Media"        
        
        if(!$isMediaItem -and (!$RootPath -and ![System.IO.Path]::IsPathRooted($Destination))) {
            Write-Error -Message "RootPath is required when Destination is not fully qualified." -ErrorAction Stop
        }

        $Destination = $Destination.TrimEnd('\','/')

        $output = $Destination
        $extension = [System.IO.Path]::GetExtension($Path)
        if(!$output.EndsWith($extension)) {
            if(!$output.EndsWith("/") -and !$output.EndsWith("\")) {
                $output += "/"
            }

            $output += [System.IO.Path]::GetFileName($Path)
        }

        if($Session) {
            $Username = $Session.Username
            $Password = $Session.Password
            $Credential = $Session.Credential
            $ConnectionUri = $Session | ForEach-Object { $_.Connection.BaseUri }
        }

        $serviceUrl = "/-/script"

        if($isMediaItem) {
            $serviceUrl += "/media/" + $Database + "/" + $output + "/?"
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