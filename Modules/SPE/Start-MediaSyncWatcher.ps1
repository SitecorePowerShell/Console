function global:ShouldSkip ([string]$path,[array]$skippedPaths) {
	$skipped = $false;
	$skippedPaths | % {
		if ($path -like $_) {
			return $true;
		}
	}
	return $false;
}

function global:GetRelativePath {
	param(
		[string]$fullPath,
		[string]$ThemeFolder
	)

	[System.Uri]$fileUri = [System.IO.Path]::GetDirectoryName($fullPath);
	[System.Uri]$themeUri = "$ThemeFolder\"
	[string]$relativePath = $themeUri.MakeRelativeUri($fileUri);
	return $relativePath
}


function global:GetConfigNodeValue ([string]$configPath,[string]$xpath)
{
	$config = [xml](gc $configPath)
	return $config.SelectSingleNode($xpath).InnerText
}

function global:GetConfigNodeValues ([string]$configPath,[string]$xpath)
{
	$nodes = Select-Xml -Path $configPath -XPath $xpath
	foreach ($node in $nodes)
	{
		$value = $node.Node.Value
		if ([string]::IsNullOrWhiteSpace($value)) {
			$value = $node.Node.InnerText
		}
		$value
	}
}

function global:GenerateIdentifier {
	param(
		[string]$mediaPath,
		[string]$ThemeFolder
	)
    return [string]::Join("_", $mediaPath.Split([System.IO.Path]::GetInvalidFileNameChars()));
}


function global:Start-FolderMediaSync {
	param(
		[Parameter(Mandatory = $true,Position = 0)]
		[string]$configPath,
		[Parameter(Mandatory = $true,Position = 1)]
		[string]$ThemeFolder
	)

	$hostXpath = "mediaSync/folder[@path='$ThemeFolder']/site/host"
	$protocolHost = global:GetConfigNodeValue $configPath $hostXpath

	$userNameXpath = "mediaSync/folder[@path='$ThemeFolder']/site/credentials/login"
	$userName = global:GetConfigNodeValue $configPath $userNameXpath

	$passwordXpath = "mediaSync/folder[@path='$ThemeFolder']/site/credentials/password"
	$password = global:GetConfigNodeValue $configPath $passwordXpath

    $mediaLibaryPathXpath = "mediaSync/folder[@path='$ThemeFolder']/media/mediaFolder"
	$MediaLibaryPath = global:GetConfigNodeValue $configPath $mediaLibaryPathXpath

	$skippedPathsXpath = "mediaSync/folder[@path='$ThemeFolder']/media/skippedPaths/mask"
	$skippedPaths = global:GetConfigNodeValues $configPath $skippedPathsXpath

	$filter = '*.*'
	#$logfile = "C:\Projects\Showcase.log\outlog.$([DateTime]::Now.ToString('yyMMdd_HHmm')).txt"

	$session = New-ScriptSession -Username $userName -Password $password -ConnectionUri $protocolHost
	$MessageData = New-Object PSObject -Property @{ MediaLibaryPath = $MediaLibaryPath.Trim("/"); logfile = $logfile; session = $session; ThemeFolder = $ThemeFolder.Trim("\") }

	$fsw = New-Object IO.FileSystemWatcher $ThemeFolder,$filter -Property @{ IncludeSubdirectories = $true; NotifyFilter = [IO.NotifyFilters]'FileName, LastWrite' }

	Write-Host "Watching '$ThemeFolder'" -fore green

	$idf = GenerateIdentifier $ThemeFolder
	$identifier = "FileCreated_$idf"
	Register-ObjectEvent $fsw Created -SourceIdentifier $identifier -MessageData $MessageData -Action {
		$name = $Event.SourceEventArgs.Name
		$changeType = $Event.SourceEventArgs.ChangeType
		$timeStamp = $Event.TimeGenerated
		$extension = [System.IO.Path]::GetExtension($fullPath).Replace(".","");

		if (global:ShouldSkip $name $skippedPaths) {
			return;
		}

		Write-Host "[$timeStamp] [$extension] [$changeType] : '$name'" -fore DarkGreen
		#Out-File -FilePath $Event.MessageData.logfile -Append -InputObject "The file '$name' was $changeType at $timeStamp"
	}

	$identifier = "FileRenamed_$idf"
	Register-ObjectEvent $fsw Renamed -SourceIdentifier $identifier -MessageData $MessageData -Action {
		$name = $Event.SourceEventArgs.Name;
		$changeType = $Event.SourceEventArgs.ChangeType;
		$timeStamp = $Event.TimeGenerated;
		$fullPath = $Event.SourceEventArgs.FullPath;
		$session = $Event.MessageData.session;
		$MediaLibaryPath = $Event.MessageData.MediaLibaryPath;
		$newFilenameWithoutExtension = [System.IO.Path]::GetFileNameWithoutExtension($fullPath);
		$filenameWithoutExtension = [System.IO.Path]::GetFileNameWithoutExtension($Event.SourceEventArgs.OldName);
		$extension = [System.IO.Path]::GetExtension($fullPath).Replace(".","");
		$ThemeFolder = $Event.MessageData.ThemeFolder;

		if (global:ShouldSkip $name $skippedPaths) {
			return;
		}

		[string]$relativePath = global:GetRelativePath $fullPath $ThemeFolder

		$mediaPath = "master:/media library/$MediaLibaryPath/$relativePath/$filenameWithoutExtension"
		Write-Host "[$timeStamp] [$extension] [$changeType] : '$name'" -fore DarkCyan
		#Write-Host "'$name' $changeType at $timeStamp. Renaming in Media Library..." -fore Cyan
		Invoke-RemoteScript -Session $session -ScriptBlock {
			if (Test-Path $using:mediaPath) {
				Rename-Item -Path $using:mediaPath -NewName $using:newFilenameWithoutExtension
			}
		}
		Write-Host "[$timeStamp] [$extension] [SERVER:$changeType] : '$mediaPath'" -fore Cyan
		#Write-Host "'$mediaPath' was renamed to 'master:\media library\$MediaLibaryPath\$relativePath\$newFilenameWithoutExtension'" -fore Cyan
	}

	$identifier = "FileDeleted_$idf"
	Register-ObjectEvent $fsw Deleted -SourceIdentifier $identifier -MessageData $MessageData -Action {
		$name = $Event.SourceEventArgs.Name;
		$changeType = $Event.SourceEventArgs.ChangeType;
		$timeStamp = $Event.TimeGenerated;
		$fullPath = $Event.SourceEventArgs.FullPath;
		$session = $Event.MessageData.session;
		$MediaLibaryPath = $Event.MessageData.MediaLibaryPath;
		$filenameWithoutExtension = [System.IO.Path]::GetFileNameWithoutExtension($fullPath);
		$extension = [System.IO.Path]::GetExtension($fullPath).Replace(".","");
		$ThemeFolder = $Event.MessageData.ThemeFolder;

		[string]$relativePath = global:GetRelativePath $fullPath $ThemeFolder

		if (global:ShouldSkip $name $skippedPaths) {
			return;
		}

		$mediaPath = "master:/media library/$MediaLibaryPath/$relativePath/$filenameWithoutExtension"
		#Write-Host "'$name' $changeType at $timeStamp. Removing from Media Library..." -fore Red
		Write-Host "[$timeStamp] [$extension] [$changeType] : '$name'" -fore DarkRed
		Invoke-RemoteScript -Session $session -ScriptBlock {
			if (Test-Path $using:mediaPath) {
				Remove-Item -Path $using:mediaPath -Force
			}
		}

		Write-Host "[$timeStamp] [$extension] [SERVER:$changeType] : '$mediaPath'" -fore Red
		#Write-Host "'$mediaPath' was deleted" -fore Red
		#Out-File -FilePath $Event.MessageData.logfile -Append -InputObject "The file '$name' was $changeType at $timeStamp"
	}

	$identifier = "FileChanged_$idf"
	Register-ObjectEvent $fsw Changed -SourceIdentifier $identifier -MessageData $MessageData -Action {
		$name = $Event.SourceEventArgs.Name;
		$changeType = $Event.SourceEventArgs.ChangeType;
		$timeStamp = $Event.TimeGenerated;
		$fullPath = $Event.SourceEventArgs.FullPath;
		$session = $Event.MessageData.session;
		$MediaLibaryPath = $Event.MessageData.MediaLibaryPath;
		$filenameWithoutExtension = [System.IO.Path]::GetFileNameWithoutExtension($fullPath);
		$extension = [System.IO.Path]::GetExtension($fullPath).Replace(".","");
		$ThemeFolder = $Event.MessageData.ThemeFolder;

		if (global:ShouldSkip $name $skippedPaths) {
			return;
		}

		if ((Get-Item $fullPath) -is [System.IO.DirectoryInfo]) {
			Write-Host "'$name' is a folder - skipping upload." -fore White
			return;
		}

		[string]$relativePath = global:GetRelativePath $fullPath $ThemeFolder


		Write-Host "[$timeStamp] [$extension] [$changeType] : '$name'" -fore DarkYellow
		#Write-Host "'$name' $changeType at $timeStamp. Uploading..." -fore Yellow
		Get-Item -Path $fullPath | Send-RemoteItem -Session $session -RootPath Media -Destination "$MediaLibaryPath/$relativePath"

		$mediaPath = "master:/media library/$MediaLibaryPath/$relativePath/$filenameWithoutExtension"
		Write-Host "[$timeStamp] [$extension] [SERVER:$changeType] : '$mediaPath'" -fore Yellow
		#Write-Host "'$mediaPath' was uploaded" -fore Yellow
		#Out-File -FilePath $Event.MessageData.logfile -Append -InputObject "The file '$name' was $changeType at $timeStamp"
	}

	Write-Host "Uploaded to '$protocolHost/-/media/$MediaLibaryPath'" -fore green
	Write-Host "Watcher started." -fore green
}

function global:Stop-FolderMediaSync {
	param(
		[Parameter(Mandatory = $true,Position = 0)]
		[string]$configPath,
		[Parameter(Mandatory = $true,Position = 1)]
		[string]$ThemeFolder
	)

	$idf = GenerateIdentifier $ThemeFolder
	$identifiers = @("FileCreated_$idf", "FileRenamed_$idf", "FileDeleted_$idf", "FileChanged_$idf") 
	
	foreach ($identifier in $identifiers)
	{
		Unregister-Event -SourceIdentifier $identifier
		Write-Host "Event subscription with identifier '$identifier' has been removed" -fore Cyan
	}
}


function Stop-MediaSyncWatcher {
	param(
		[Parameter(Mandatory = $true,Position = 0)]
		[string]$configPath
	)
	<#

    .SYNOPSIS
        Synchronize local folder (with subfolders) with Media Library (matching structure) on a remote Sitecore instance

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


	$themeFolders = global:GetConfigNodeValues $configPath "mediaSync/folder/@path"
	foreach ($themeFolder in $themeFolders)
	{
		global:Stop-FolderMediaSync $configPath $themeFolder
	}	
}

function Start-MediaSyncWatcher {
	param(
		[Parameter(Mandatory = $true,Position = 0)]
		[string]$configPath
	)
	<#

    .SYNOPSIS
        Stops synchronization of local folder (with subfolders) with Media Library (matching structure) on a remote Sitecore instance

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
    	
	$themeFolders = global:GetConfigNodeValues $configPath "mediaSync/folder/@path"
	foreach ($themeFolder in $themeFolders)
	{
		global:Start-FolderMediaSync $configPath $themeFolder
	}
}