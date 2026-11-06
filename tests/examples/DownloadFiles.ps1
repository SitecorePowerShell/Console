#$VerbosePreference = "Continue"

Import-Module -Name SPE

$instanceUrls = @("http://console")
$session = New-ScriptSession -Username "admin" -Password "b" -ConnectionUri $instanceUrls

$libraryPath = "/sitecore/media library/images/image.png"
Get-Item -Path C:\image.png | Send-MediaItem -Session $session -Destination $libraryPath

$savePath = "C:\image-$([datetime]::Now.ToString("yyyyddMM-HHmmss")).png"
Receive-MediaItem -Session $session -Path $libraryPath -Destination $savePath