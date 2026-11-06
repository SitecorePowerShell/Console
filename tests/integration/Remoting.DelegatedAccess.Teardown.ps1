# Remoting.DelegatedAccess.Teardown.ps1
# Removes test DA items, test script, test user, and test role.
# Called by Run-RemotingTests.ps1 after DA tests complete.
# Requires: SPE Remoting enabled, shared secret configured

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost

Write-Host "`n  [Delegated Access Teardown: removing test items]" -ForegroundColor Cyan

$teardownResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $results = @()

    # Remove DA config item
    $daItem = Get-ChildItem -Path "master:/sitecore/system/Modules/PowerShell/Settings/Access/Delegated Access" |
        Where-Object { $_.Name -eq "Test-DA-Mapping" }
    if ($daItem) { $daItem | Remove-Item -Force; $results += "DA_DELETED" }
    else { $results += "DA_NOT_FOUND" }

    # Remove test script
    $scriptItem = Get-ChildItem -Path "master:/sitecore/system/Modules/PowerShell/Script Library/SPE/Core/Platform/Development" |
        Where-Object { $_.Name -eq "Test-DA-WhoAmI" }
    if ($scriptItem) { $scriptItem | Remove-Item -Force; $results += "SCRIPT_DELETED" }
    else { $results += "SCRIPT_NOT_FOUND" }

    # Remove test user
    $userName = "sitecore\test-da-user"
    $user = Get-User -Identity $userName -ErrorAction SilentlyContinue
    if ($user) { Remove-User -Identity $userName -ErrorAction SilentlyContinue; $results += "USER_DELETED" }
    else { $results += "USER_NOT_FOUND" }

    # Remove test role
    $roleName = "sitecore\Test-DA-Operators"
    $role = Get-Role -Identity $roleName -ErrorAction SilentlyContinue
    if ($role) { Remove-Role -Identity $roleName -ErrorAction SilentlyContinue; $results += "ROLE_DELETED" }
    else { $results += "ROLE_NOT_FOUND" }

    $results -join "|"
} -Raw

Write-Host "    Teardown result: $teardownResult" -ForegroundColor Gray

Stop-ScriptSession -Session $session
