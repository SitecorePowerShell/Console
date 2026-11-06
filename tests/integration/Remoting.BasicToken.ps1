Clear-Host

$name = 'sitecore\admin'
$password = 'b'
$hostname = "https://spe.dev.local"

Import-Module -Name SPE -Force
$session = New-ScriptSession -Username $name -Password $password -ConnectionUri $hostname
$watch = [System.Diagnostics.Stopwatch]::StartNew()
Invoke-RemoteScript -ScriptBlock {
    $env:COMPUTERNAME
} -Session $session -Raw
$watch.Stop()
$watch.ElapsedMilliseconds / 1000
Stop-ScriptSession -Session $session