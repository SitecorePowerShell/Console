$VerbosePreference = "Continue"

Import-Module -Name SPE -Force

$instanceUrls = @("http://console")
$session = New-ScriptSession -Username "admin" -Password "b" -ConnectionUri $instanceUrls

Get-Item -Path C:\image.png | Send-RemoteItem -Session $session -RootPath Media -Destination "Images/"
Get-Item -Path C:\temp\data.xml | Send-RemoteItem -Session $session -RootPath App
Get-Item -Path C:\temp\data.xml | Send-RemoteItem -Session $session -RootPath Package -Destination "\"
Get-Item -Path C:\temp\largeimage.jpg | Send-RemoteItem -Session $session -RootPath App -Destination "\upload\images\"