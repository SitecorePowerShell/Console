# Remoting.RestrictionProfiles.Teardown.ps1
# Removes test override items AFTER the profile config is removed.
# Called by Run-RemotingTests.ps1 after the profile phase cleanup.
# Requires: SPE Remoting enabled, shared secret configured, NO profile config deployed

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost

Write-Host "`n  [Profile Override Teardown: removing test items]" -ForegroundColor Cyan

$cleanupResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $folder = Get-Item -Path "master:/sitecore/system/Modules/PowerShell/Settings/Remoting/Restriction Profiles"
    if (-not $folder) { return "FOLDER_NOT_FOUND" }

    $item = Get-ChildItem -Path "master:$($folder.Paths.FullPath)" -Recurse | Where-Object { $_.Name -eq "Test-BlockGetDatabase" }
    if ($item) {
        $item | Remove-Item -Force
        "DELETED"
    } else {
        "NOT_FOUND"
    }
} -Raw

Write-Host "    Teardown result: $cleanupResult" -ForegroundColor Gray

Stop-ScriptSession -Session $session
