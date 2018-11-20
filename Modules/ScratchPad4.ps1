Import-Module -Name SPE -Force

$protocolHost = "https://spe.dev.local"

$watch = [System.Diagnostics.Stopwatch]::StartNew()
$session = New-ScriptSession -Username "sitecore\admin" -Password "b" -ConnectionUri $protocolHost
$session.PersistentSession = $false
for($i = 0; $i -lt 10; $i++) {
    Invoke-RemoteScript -Session $session -ScriptBlock { 
        Get-Location
    }
}

Stop-ScriptSession -Session $session

$watch.Stop()
$watch.ElapsedMilliseconds / 1000