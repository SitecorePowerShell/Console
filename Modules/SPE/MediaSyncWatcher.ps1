function ShouldSkip {
	param(
		[string]$Path,
		[array]$SkippedPaths
	)
	
	$SkippedPaths | ForEach-Object {
		if ($Path -like $_) {
			return $true
		}
	}
	return $false
}

function GetRelativePath {
	param(
		[string]$FullPath,
		[string]$ThemeFolder
	)

	[System.Uri]$fileUri = [System.IO.Path]::GetDirectoryName($FullPath)
	[System.Uri]$themeUri = "$ThemeFolder\"
	[string]$relativePath = $themeUri.MakeRelativeUri($fileUri)
	return $relativePath
}

function GetConfigNodeValue {
	param(
		[string]$Path,
		[string]$XPath
	)
	$config = [xml](Get-Content $Path)
	return $config.SelectSingleNode($XPath).InnerText
}

function GetConfigNodeValues {
	param(
		[string]$Path,
		[string]$XPath
	)
	$nodes = Select-Xml -Path $Path -XPath $XPath
	foreach ($node in $nodes) {
		$value = $node.Node.Value
		if ([string]::IsNullOrWhiteSpace($value)) {
			$value = $node.Node.InnerText
		}
		$value
	}
}

function GenerateIdentifier {
	param(
		[string]$MediaPath,
		[string]$ThemeFolder
	)
    return [string]::Join("_", $MediaPath.Split([System.IO.Path]::GetInvalidFileNameChars()))
}

function Start-FolderMediaSync {
	param(
		[Parameter(Mandatory = $true,Position = 0)]
		[ValidateScript({ Test-Path -Path $_ })]
		[string]$Path,
		[Parameter(Mandatory = $true,Position = 1)]
		[ValidateScript({ Test-Path -Path $_ })]
		[string]$ThemeFolder
	)

	$hostXpath = "mediaSync/folder[@path='$ThemeFolder']/site/host"
	$protocolHost = GetConfigNodeValue $Path $hostXpath

	$userNameXpath = "mediaSync/folder[@path='$ThemeFolder']/site/credentials/login"
	$userName = GetConfigNodeValue $Path $userNameXpath

	$passwordXpath = "mediaSync/folder[@path='$ThemeFolder']/site/credentials/password"
	$password = GetConfigNodeValue $Path $passwordXpath

    $mediaLibaryPathXpath = "mediaSync/folder[@path='$ThemeFolder']/media/mediaFolder"
	$MediaLibaryPath = GetConfigNodeValue $Path $mediaLibaryPathXpath

	$skippedPathsXpath = "mediaSync/folder[@path='$ThemeFolder']/media/skippedPaths/mask"
	$skippedPaths = GetConfigNodeValues $Path $skippedPathsXpath

	$filter = '*.*'

	$session = New-ScriptSession -Username $userName -Password $password -ConnectionUri $protocolHost
	$MessageData = New-Object PSObject -Property @{ MediaLibaryPath = $MediaLibaryPath.Trim("/"); logfile = $logfile; session = $session; ThemeFolder = $ThemeFolder.Trim("\") }

	$fsw = New-Object IO.FileSystemWatcher $ThemeFolder,$filter -Property @{ IncludeSubdirectories = $true; NotifyFilter = [IO.NotifyFilters]'FileName, LastWrite' }

	Write-Host "Watching '$ThemeFolder'" -ForegroundColor Green

	$idf = GenerateIdentifier $ThemeFolder
	$identifier = "FileCreated_$idf"
	Register-ObjectEvent $fsw Created -SourceIdentifier $identifier -MessageData $MessageData -Action {
		$name = $Event.SourceEventArgs.Name
		$changeType = $Event.SourceEventArgs.ChangeType
		$timeStamp = $Event.TimeGenerated
		$extension = [System.IO.Path]::GetExtension($fullPath).Replace(".","")

		if (ShouldSkip $name $skippedPaths) {
			return
		}

		Write-Host "[$timeStamp] [$extension] [$changeType] : '$name'" -ForegroundColor DarkGreen
	}

	$identifier = "FileRenamed_$idf"
	Register-ObjectEvent $fsw Renamed -SourceIdentifier $identifier -MessageData $MessageData -Action {
		$name = $Event.SourceEventArgs.Name
		$changeType = $Event.SourceEventArgs.ChangeType
		$timeStamp = $Event.TimeGenerated
		$fullPath = $Event.SourceEventArgs.FullPath
		$session = $Event.MessageData.session
		$MediaLibaryPath = $Event.MessageData.MediaLibaryPath
		$newFilenameWithoutExtension = [System.IO.Path]::GetFileNameWithoutExtension($fullPath)
		$filenameWithoutExtension = [System.IO.Path]::GetFileNameWithoutExtension($Event.SourceEventArgs.OldName)
		$extension = [System.IO.Path]::GetExtension($fullPath).Replace(".","")
		$ThemeFolder = $Event.MessageData.ThemeFolder

		if (ShouldSkip $name $skippedPaths) {
			return
		}

		[string]$relativePath = GetRelativePath $fullPath $ThemeFolder

		$mediaPath = "master:/media library/$MediaLibaryPath/$relativePath/$filenameWithoutExtension"
		Write-Host "[$timeStamp] [$extension] [$changeType] : '$name'" -ForegroundColor DarkCyan
		Invoke-RemoteScript -Session $session -ScriptBlock {
			if (Test-Path $using:mediaPath) {
				Rename-Item -Path $using:mediaPath -NewName $using:newFilenameWithoutExtension
			}
		}
		Write-Host "[$timeStamp] [$extension] [SERVER:$changeType] : '$mediaPath'" -ForegroundColor Cyan
	}

	$identifier = "FileDeleted_$idf"
	Register-ObjectEvent $fsw Deleted -SourceIdentifier $identifier -MessageData $MessageData -Action {
		$name = $Event.SourceEventArgs.Name
		$changeType = $Event.SourceEventArgs.ChangeType
		$timeStamp = $Event.TimeGenerated
		$fullPath = $Event.SourceEventArgs.FullPath
		$session = $Event.MessageData.session
		$MediaLibaryPath = $Event.MessageData.MediaLibaryPath;
		$filenameWithoutExtension = [System.IO.Path]::GetFileNameWithoutExtension($fullPath)
		$extension = [System.IO.Path]::GetExtension($fullPath).Replace(".","")
		$ThemeFolder = $Event.MessageData.ThemeFolder

		[string]$relativePath = GetRelativePath $fullPath $ThemeFolder

		if (ShouldSkip $name $skippedPaths) {
			return
		}

		$mediaPath = "master:/media library/$MediaLibaryPath/$relativePath/$filenameWithoutExtension"
		Write-Host "[$timeStamp] [$extension] [$changeType] : '$name'" -ForegroundColor DarkRed
		Invoke-RemoteScript -Session $session -ScriptBlock {
			if (Test-Path $using:mediaPath) {
				Remove-Item -Path $using:mediaPath -Force
			}
		}

		Write-Host "[$timeStamp] [$extension] [SERVER:$changeType] : '$mediaPath'" -ForegroundColor Red
	}

	$identifier = "FileChanged_$idf"
	Register-ObjectEvent $fsw Changed -SourceIdentifier $identifier -MessageData $MessageData -Action {
		$name = $Event.SourceEventArgs.Name
		$changeType = $Event.SourceEventArgs.ChangeType
		$timeStamp = $Event.TimeGenerated
		$fullPath = $Event.SourceEventArgs.FullPath
		$session = $Event.MessageData.session
		$MediaLibaryPath = $Event.MessageData.MediaLibaryPath
		$filenameWithoutExtension = [System.IO.Path]::GetFileNameWithoutExtension($fullPath)
		$extension = [System.IO.Path]::GetExtension($fullPath).Replace(".","")
		$ThemeFolder = $Event.MessageData.ThemeFolder

		if (ShouldSkip $name $skippedPaths) {
			return
		}

		if ((Get-Item $fullPath) -is [System.IO.DirectoryInfo]) {
			Write-Host "'$name' is a folder - skipping upload." -ForegroundColor White
			return
		}

		[string]$relativePath = GetRelativePath $fullPath $ThemeFolder


		Write-Host "[$timeStamp] [$extension] [$changeType] : '$name'" -ForegroundColor DarkYellow
		Get-Item -Path $fullPath | Send-RemoteItem -Session $session -RootPath Media -Destination "$MediaLibaryPath/$relativePath"

		$mediaPath = "master:/media library/$MediaLibaryPath/$relativePath/$filenameWithoutExtension"
		Write-Host "[$timeStamp] [$extension] [SERVER:$changeType] : '$mediaPath'" -ForegroundColor Yellow
	}

	Write-Host "Uploaded to '$protocolHost/-/media/$MediaLibaryPath'" -ForegroundColor Green
	Write-Host "Watcher started." -ForegroundColor Green
}

function Stop-FolderMediaSync {
	param(
		[Parameter(Mandatory = $true,Position = 0)]
		[ValidateScript({ Test-Path -Path $_ })]
		[string]$Path,
		[Parameter(Mandatory = $true,Position = 1)]
		[ValidateScript({ Test-Path -Path $_ })]
		[string]$ThemeFolder
	)

	$idf = GenerateIdentifier $ThemeFolder
	$identifiers = @("FileCreated_$idf", "FileRenamed_$idf", "FileDeleted_$idf", "FileChanged_$idf") 
	
	foreach ($identifier in $identifiers) {
		Unregister-Event -SourceIdentifier $identifier
		Write-Host "Event subscription with identifier '$identifier' has been removed" -ForegroundColor Cyan
	}
}

<#
    .SYNOPSIS
        Stops synchronization of local folder (with subfolders) with Media Library (matching structure) on a remote Sitecore instance

    .DESCRIPTION
        The script allows you to work with SXA Themes in a more comfortable way.
        You can need specify the instance hostname, a folder to watch and the location of your Theme in Media Library 
        in the variables directly below and run the script that will monitor and upload your changes.

        The script requires that SPE Remoting is deployed and enabled on the Sitecore Server.
        The script requires that SPE Remoting PowerShell Module is installed on your local machine.
                
        The following endpoints must be enabled:
            - remoting
            - mediaUpload

		The script requires the same xml config that was profided for Start-MediaSyncWatcher
    
	.NOTES
		Author PowerShell:  Adam Najmanowicz
		Version: 1.0.0 29.March.2017

#>
function Stop-MediaSyncWatcher {
	param(
		[Parameter(Mandatory = $true,Position = 0)]
		[Alias("configPath")]
		[ValidateScript({ Test-Path -Path $_ })]
		[string]$Path
	)

	$themeFolders = GetConfigNodeValues $Path "mediaSync/folder/@path"
	foreach ($themeFolder in $themeFolders) {
		Stop-FolderMediaSync -Path $Path -ThemeFolder $themeFolder
	}	
}

<#
    .SYNOPSIS
		Synchronize local folder (with subfolders) with Media Library (matching structure) on a remote Sitecore instance        
    
    .DESCRIPTION
        The script allows you to stops synchronization of local folder (with subfolders) with Media Library (matching structure) on a remote Sitecore instance

        The script requires that SPE Remoting is deployed and enabled on the Sitecore Server.
        The script requires that SPE Remoting PowerShell Module is installed on your local machine.
                
        The following endpoints must be enabled:
            - remoting
            - mediaUpload

		The script requires xml config file with a following structure
		
	    <?xml version="1.0" encoding="utf-8"?>
	    <mediaSync>
            <folder path="C:\Projects\Showcase">
                <site>
                    <host>http://sxa</host>
                    <credentials>
                        <login>sitecore\admin</login>
                        <password>b</password>
                    </credentials>
                </site>
                <media>
                    <mediaFolder>Project/Showcase</mediaFolder>			
                    <skippedPaths>
                        <mask>.sass-cache*</mask>
                        <mask>*optimized*</mask>				
                        <mask>Styles.css</mask>
                        <mask>node_modules*</mask>
                        <mask>Showcase.zip</mask>
                        <mask>Scripts\concat.js</mask>				
                    </skippedPaths>
                </media>
            </folder>			<!-- it's possible to add here few more folders for sync -->			
        </mediaSync>
		
    .NOTES
		Author PowerShell:  Adam Najmanowicz
		Version: 1.0.0 29.March.2017

#>
function Start-MediaSyncWatcher {
	param(
		[Parameter(Mandatory = $true,Position = 0)]
		[Alias("configPath")]
		[ValidateScript({ Test-Path -Path $_ })]
		[string]$Path
	)
    	
	$themeFolders = GetConfigNodeValues $Path "mediaSync/folder/@path"
	foreach ($themeFolder in $themeFolders) {
		Start-FolderMediaSync -Path $Path -ThemeFolder $themeFolder
	}
}