# Remoting.Expiration.Teardown.ps1
# Removes test items created by Remoting.Expiration.Setup.ps1.
# Called by Run-RemotingTests.ps1 after the expiration test phase.

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost

Write-Host "`n  [Expiration Teardown: removing test items]" -ForegroundColor Cyan

$cleanupResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $deleted = 0

    $policiesFolder = Get-Item -Path "master:/sitecore/system/Modules/PowerShell/Settings/Remoting/Policies" -ErrorAction SilentlyContinue
    if ($policiesFolder) {
        $items = Get-ChildItem -Path "master:$($policiesFolder.Paths.FullPath)" -Recurse |
            Where-Object { $_.Name -like "Test-Expir*" }
        if ($items) {
            $deleted += ($items | Measure-Object).Count
            $items | Remove-Item -Force
        }
    }

    $apiKeysFolder = Get-Item -Path "master:/sitecore/system/Modules/PowerShell/Settings/Remoting/API Keys" -ErrorAction SilentlyContinue
    if ($apiKeysFolder) {
        $items = Get-ChildItem -Path "master:$($apiKeysFolder.Paths.FullPath)" -Recurse |
            Where-Object { $_.Name -like "Test-Expir*" -or $_.Name -like "Test-NoExpiry*" }
        if ($items) {
            $deleted += ($items | Measure-Object).Count
            $items | Remove-Item -Force
        }
    }

    if ($deleted -gt 0) { "DELETED:$deleted" } else { "NOT_FOUND" }
} -Raw

Write-Host "    Teardown result: $cleanupResult" -ForegroundColor Gray

Stop-ScriptSession -Session $session
