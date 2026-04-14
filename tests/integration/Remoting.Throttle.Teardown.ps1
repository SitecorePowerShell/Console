# Remoting.Throttle.Teardown.ps1
# Removes test API Key items AFTER the throttle tests complete.
# Called by Run-RemotingTests.ps1 after the throttle phase.
# Requires: SPE Remoting enabled, shared secret configured

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost

Write-Host "`n  [Throttle Teardown: removing test API Keys]" -ForegroundColor Cyan

$cleanupResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $deleted = 0

    # Clean up throttle test API Key items
    $apiKeysFolder = Get-Item -Path "master:/sitecore/system/Modules/PowerShell/Settings/Remoting/API Keys" -ErrorAction SilentlyContinue
    if ($apiKeysFolder) {
        $items = Get-ChildItem -Path "master:$($apiKeysFolder.Paths.FullPath)" -Recurse |
            Where-Object { $_.Name -like "Test-Throttle*" }
        if ($items) {
            $deleted += ($items | Measure-Object).Count
            $items | Remove-Item -Force
        }
    }

    # Clean up throttle test policy items
    $policiesFolder = Get-Item -Path "master:/sitecore/system/Modules/PowerShell/Settings/Remoting/Policies" -ErrorAction SilentlyContinue
    if ($policiesFolder) {
        $items = Get-ChildItem -Path "master:$($policiesFolder.Paths.FullPath)" -Recurse |
            Where-Object { $_.Name -like "Test-Throttle*" }
        if ($items) {
            $deleted += ($items | Measure-Object).Count
            $items | Remove-Item -Force
        }
    }

    if ($deleted -gt 0) { "DELETED:$deleted" } else { "NOT_FOUND" }
} -Raw

Write-Host "    Teardown result: $cleanupResult" -ForegroundColor Gray

Stop-ScriptSession -Session $session
