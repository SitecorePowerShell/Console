# Remoting.RestrictionProfiles.Setup.ps1
# Creates test override items BEFORE the profile config is deployed.
# Called by Run-RemotingTests.ps1 in the unrestricted phase.
# Requires: SPE Remoting enabled, shared secret configured, NO profile config deployed

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost

Write-Host "`n  [Profile Override Setup: creating test items]" -ForegroundColor Cyan

$createResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $folder = Get-Item -Path "master:/sitecore/system/Modules/PowerShell/Settings/Remoting/Restriction Profiles"
    if (-not $folder) { return "FOLDER_NOT_FOUND" }

    # Clean up any leftover from previous test runs (including renamed test items)
    $existing = Get-ChildItem -Path "master:$($folder.Paths.FullPath)" -Recurse | Where-Object { $_.Name -eq "Test-AllowPublishItem" -or $_.Name -eq "Test-BlockGetDatabase" }
    if ($existing) { $existing | Remove-Item -Force }

    $override = New-Item -Path "master:$($folder.Paths.FullPath)/Test-AllowPublishItem" `
        -ItemType "/sitecore/templates/Modules/PowerShell Console/Remoting/Restriction Profile"
    $override.Editing.BeginEdit()
    $override["Enabled"] = "1"
    $override["Base Profile"] = "read-only"
    $override["Additional Allowed Commands"] = "Publish-Item"
    $override.Editing.EndEdit() | Out-Null
    "CREATED:$($override.ID)"
} -Raw

if ($createResult -like "CREATED:*") {
    Write-Host "    Override item created: $createResult" -ForegroundColor Green
} else {
    Write-Host "    ERROR creating override item: $createResult" -ForegroundColor Red
}

Stop-ScriptSession -Session $session
