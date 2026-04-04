# Remoting Tests - Delegated Access
# Validates that DA config items correctly elevate script execution context.
# Requires: Remoting.DelegatedAccess.Setup.ps1 has been run first.

Write-Host "`n  [Delegated Access]" -ForegroundColor White

# -- Helper: resolve IDs created by setup ------------------------------------

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost

$ids = Invoke-RemoteScript -Session $session -ScriptBlock {
    $scriptItem = Get-ChildItem -Path "master:/sitecore/system/Modules/PowerShell/Script Library/SPE/Core/Platform/Development" |
        Where-Object { $_.Name -eq "Test-DA-WhoAmI" }
    $daItem = Get-ChildItem -Path "master:/sitecore/system/Modules/PowerShell/Delegated Access" |
        Where-Object { $_.Name -eq "Test-DA-Mapping" }
    "$($scriptItem.ID)|$($daItem.ID)"
} -Raw

Stop-ScriptSession -Session $session

$parts = $ids -split '\|'
$scriptItemId = $parts[0]
$daItemId = $parts[1]

if (-not $scriptItemId -or -not $daItemId) {
    Write-Host "    SKIP: Setup items not found (run Setup.ps1 first)" -ForegroundColor Yellow
    Skip-Test "DA test items not found" "Setup.ps1 must run first"
    return
}

# -- Test 1: Elevated user resolves to impersonated user ----------------------

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost

$elevatedResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $testUser = [Sitecore.Security.Accounts.User]::FromName("sitecore\test-da-user", $true)
    $item = Get-Item -Path "master:" -ID $params.sid
    [Spe.Core.Settings.DelegatedAccessManager]::GetDelegatedUser($testUser, $item).Name
} -Arguments @{ sid = $scriptItemId } -Raw

Assert-Equal $elevatedResult "sitecore\admin" "elevated user resolves to impersonated user (sitecore\admin)"

# -- Test 2: IsElevated returns true for mapped user/script -------------------

$isElevated = Invoke-RemoteScript -Session $session -ScriptBlock {
    $testUser = [Sitecore.Security.Accounts.User]::FromName("sitecore\test-da-user", $true)
    $item = Get-Item -Path "master:" -ID $params.sid
    [Spe.Core.Settings.DelegatedAccessManager]::IsElevated($testUser, $item).ToString()
} -Arguments @{ sid = $scriptItemId } -Raw

Assert-Equal $isElevated "True" "IsElevated returns True for mapped user and script"

# -- Test 3: IsElevated returns false for unmapped script ---------------------

$notElevated = Invoke-RemoteScript -Session $session -ScriptBlock {
    $testUser = [Sitecore.Security.Accounts.User]::FromName("sitecore\test-da-user", $true)
    # Use a script that is NOT in the DA mapping
    $anyScript = Get-ChildItem -Path "master:/sitecore/system/Modules/PowerShell/Script Library/SPE/Core/Platform/Development" |
        Where-Object { $_.Name -ne "Test-DA-WhoAmI" } | Select-Object -First 1
    if ($anyScript) {
        [Spe.Core.Settings.DelegatedAccessManager]::IsElevated($testUser, $anyScript).ToString()
    } else {
        "False"
    }
} -Raw

Assert-Equal $notElevated "False" "IsElevated returns False for unmapped script"

# -- Test 4: Non-elevated user is not granted DA ------------------------------

$adminNotElevated = Invoke-RemoteScript -Session $session -ScriptBlock {
    # Admin is NOT in the Test-DA-Operators role, so DA should not apply
    $item = Get-Item -Path "master:" -ID $params.sid
    [Spe.Core.Settings.DelegatedAccessManager]::IsElevated(
        [Sitecore.Context]::User,
        $item
    ).ToString()
} -Arguments @{ sid = $scriptItemId } -Raw

Assert-Equal $adminNotElevated "False" "user NOT in elevated role is denied DA"

# -- Test 5: Disabled DA item is not honored ----------------------------------

$disabledResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    # Disable the DA item
    $daItem = Get-Item -Path "master:" -ID $params.did
    $daItem.Editing.BeginEdit()
    $daItem["Enabled"] = ""
    $daItem.Editing.EndEdit() | Out-Null

    # Check elevation (cache should be invalidated by the save event)
    Start-Sleep -Milliseconds 500
    $scriptItem = Get-Item -Path "master:" -ID $params.sid
    $testUser = [Sitecore.Security.Accounts.User]::FromName("sitecore\test-da-user", $true)
    $result = [Spe.Core.Settings.DelegatedAccessManager]::IsElevated($testUser, $scriptItem).ToString()

    # Re-enable for subsequent tests
    $daItem.Editing.BeginEdit()
    $daItem["Enabled"] = "1"
    $daItem.Editing.EndEdit() | Out-Null

    $result
} -Arguments @{ sid = $scriptItemId; did = $daItemId } -Raw

Assert-Equal $disabledResult "False" "disabled DA item denies elevation"

# -- Test 6: Empty ScriptItem field denies elevation --------------------------

$emptyScriptResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $daItem = Get-Item -Path "master:" -ID $params.did
    $originalValue = $daItem["ScriptItem"]

    # Clear the script whitelist
    $daItem.Editing.BeginEdit()
    $daItem["ScriptItem"] = ""
    $daItem.Editing.EndEdit() | Out-Null

    Start-Sleep -Milliseconds 500
    $scriptItem = Get-Item -Path "master:" -ID $params.sid
    $testUser = [Sitecore.Security.Accounts.User]::FromName("sitecore\test-da-user", $true)
    $result = [Spe.Core.Settings.DelegatedAccessManager]::IsElevated($testUser, $scriptItem).ToString()

    # Restore
    $daItem.Editing.BeginEdit()
    $daItem["ScriptItem"] = $originalValue
    $daItem.Editing.EndEdit() | Out-Null

    $result
} -Arguments @{ sid = $scriptItemId; did = $daItemId } -Raw

Assert-Equal $emptyScriptResult "False" "empty ScriptItem field denies elevation (null-guard)"

Stop-ScriptSession -Session $session
