Import-Module -Name SPE -Force
$VerbosePreference = "Continue"

# If you need to connect to more than one instance of Sitecore add it to the list.
$instanceUrls = @("http://console","http://console")
$session = New-ScriptSession -Username admin -Password b -ConnectionUri $instanceUrls

Invoke-RemoteScript -Session $session -ScriptBlock { $env:computername }
Invoke-RemoteScript -Username admin -Password b -ConnectionUri $instanceUrls -ScriptBlock { $env:computername }
Receive-MediaItem -Session $session -Path "/sitecore/media library/Images/image" -Destination C:\ -Force
Receive-MediaItem -Username admin -Password b -ConnectionUri $instanceUrls -Path "/sitecore/media library/Images/image" -Destination C:\ -Force
Send-MediaItem -Session $session -Path C:\image1.png -Destination "/sitecore/media library/Images/"
Send-MediaItem -Username admin -Password b -ConnectionUri $instanceUrls -Path C:\image1.png -Destination "/sitecore/media library/Images/"

Stop-ScriptSession -Session $session