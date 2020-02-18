Import-Module -Name SPE -Force

$protocolHost = "https://spe.dev.local"

$session = New-ScriptSession -Username "sitecore\admin" -Password "b" -ConnectionUri $protocolHost -Timeout 10

Invoke-RemoteScript -Session $session -ScriptBlock { Get-Location } -Verbose