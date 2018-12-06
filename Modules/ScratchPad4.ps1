Import-Module -Name SPE -Force

$protocolHost = "https://spe.dev.local"

$watch = [System.Diagnostics.Stopwatch]::StartNew()
$session = New-ScriptSession -Username "sitecore\admin" -Password "b" -ConnectionUri $protocolHost
$session.PersistentSession = $false

foreach($i in 1..20) {
    Invoke-RemoteScript -Session $session -ScriptBlock { 
        Get-Location
        #Start-Sleep -Seconds (Get-Random -Min 1 -Max 2)
    } > $null
}

$watch.Stop()
$watch.ElapsedMilliseconds / 1000

Stop-ScriptSession -Session $session #-Timeout 1 -Verbose