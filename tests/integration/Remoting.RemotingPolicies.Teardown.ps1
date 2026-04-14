# Remoting.RemotingPolicies.Teardown.ps1
# Removes test policy, API Key, and script items AFTER the policy tests complete.
# Called by Run-RemotingTests.ps1 after the policy phase.
# Requires: SPE Remoting enabled, shared secret configured

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost

Write-Host "`n  [Remoting Policy Teardown: removing test items]" -ForegroundColor Cyan

$cleanupResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $deleted = 0

    # Clean up policy items
    $policiesFolder = Get-Item -Path "master:/sitecore/system/Modules/PowerShell/Settings/Access/Policies" -ErrorAction SilentlyContinue
    if ($policiesFolder) {
        $items = Get-ChildItem -Path "master:$($policiesFolder.Paths.FullPath)" -Recurse |
            Where-Object { $_.Name -like "Test-*" }
        if ($items) {
            $deleted += ($items | Measure-Object).Count
            $items | Remove-Item -Force
        }
    }

    # Clean up API Key items
    $apiKeysFolder = Get-Item -Path "master:/sitecore/system/Modules/PowerShell/Settings/Access/API Keys" -ErrorAction SilentlyContinue
    if ($apiKeysFolder) {
        $items = Get-ChildItem -Path "master:$($apiKeysFolder.Paths.FullPath)" -Recurse |
            Where-Object { $_.Name -like "Test-*" }
        if ($items) {
            $deleted += ($items | Measure-Object).Count
            $items | Remove-Item -Force
        }
    }

    # Clean up test script items from Web API root
    $webApiRoot = Get-Item -Path "master:/sitecore/system/Modules/PowerShell/Script Library/Test/Web API" -ErrorAction SilentlyContinue
    if ($webApiRoot) {
        $items = Get-ChildItem -Path "master:$($webApiRoot.Paths.FullPath)" |
            Where-Object { $_.Name -like "Test-*" }
        if ($items) {
            $deleted += ($items | Measure-Object).Count
            $items | Remove-Item -Force
        }
    }

    if ($deleted -gt 0) { "DELETED:$deleted" } else { "NOT_FOUND" }
} -Raw

Write-Host "    Teardown result: $cleanupResult" -ForegroundColor Gray

Stop-ScriptSession -Session $session
