# Remoting.ClientRetry.Teardown.ps1
# Removes test items created by Remoting.ClientRetry.Setup.ps1.

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost

Invoke-RemoteScript -Session $session -ScriptBlock {
    $remotingPath = "master:/sitecore/system/Modules/PowerShell/Settings/Access"
    $clientsFolder = Get-Item -Path "$remotingPath/Remoting Clients" -ErrorAction SilentlyContinue
    $policiesFolder = Get-Item -Path "$remotingPath/Policies" -ErrorAction SilentlyContinue

    if ($clientsFolder) {
        Get-ChildItem -Path "master:$($clientsFolder.Paths.FullPath)" -Recurse |
            Where-Object { $_.Name -like "Test-ClientRetry*" } |
            Remove-Item -Force
    }
    if ($policiesFolder) {
        Get-ChildItem -Path "master:$($policiesFolder.Paths.FullPath)" -Recurse |
            Where-Object { $_.Name -like "Test-ClientRetry*" } |
            Remove-Item -Force
    }
} | Out-Null

Stop-ScriptSession -Session $session
Write-Host "  ClientRetry test items removed." -ForegroundColor Gray
