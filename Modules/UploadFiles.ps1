$VerbosePreference = "Continue"

Import-Module -Name SPE -Force

$instanceUrls = @("http://console")
$session = New-ScriptSession -Username "admin" -Password "b" -ConnectionUri $instanceUrls

Get-Item -Path C:\image.png | Send-RemoteItem -Session $session -RootPath Media -Destination "Images/"