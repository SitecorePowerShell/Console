# Remoting.DelegatedAccess.Setup.ps1
# Creates test role, test user, DA config item, and a test script.
# Called by Run-RemotingTests.ps1 in the unrestricted (Phase 1) phase.
# Requires: SPE Remoting enabled, shared secret configured

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost

Write-Host "`n  [Delegated Access Setup: creating test items]" -ForegroundColor Cyan

$setupResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $results = @()

    # 1. Create test role
    $roleName = "sitecore\Test-DA-Operators"
    if (-not (Get-Role -Identity $roleName -ErrorAction SilentlyContinue)) {
        New-Role -Identity $roleName | Out-Null
        $results += "ROLE_CREATED"
    } else {
        $results += "ROLE_EXISTS"
    }

    # 2. Create test user in the elevated role
    $userName = "sitecore\test-da-user"
    $user = Get-User -Identity $userName -ErrorAction SilentlyContinue
    if (-not $user) {
        New-User -Identity $userName -Password "Test1234!" -Enabled -ErrorAction Stop | Out-Null
        $results += "USER_CREATED"
    } else {
        $results += "USER_EXISTS"
    }
    Add-RoleMember -Identity $roleName -Members $userName -ErrorAction SilentlyContinue

    # 3. Create a test script item that outputs the context user
    $scriptLibPath = "master:/sitecore/system/Modules/PowerShell/Script Library/SPE"
    $testFolder = Get-Item -Path "$scriptLibPath/Core/Platform/Development" -ErrorAction SilentlyContinue
    if (-not $testFolder) {
        $results += "SCRIPT_FOLDER_NOT_FOUND"
        return $results -join "|"
    }

    $scriptName = "Test-DA-WhoAmI"
    $existing = Get-ChildItem -Path "master:$($testFolder.Paths.FullPath)" | Where-Object { $_.Name -eq $scriptName }
    if ($existing) { $existing | Remove-Item -Force }

    $scriptItem = New-Item -Path "master:$($testFolder.Paths.FullPath)/$scriptName" `
        -ItemType "/sitecore/templates/Modules/PowerShell Console/PowerShell Script"
    $scriptItem.Editing.BeginEdit()
    $scriptItem["Script"] = '[Sitecore.Context]::User.Name'
    $scriptItem.Editing.EndEdit() | Out-Null
    $results += "SCRIPT:$($scriptItem.ID)"

    # 4. Create DA config item mapping the test role + admin impersonation + the script
    $daFolder = Get-Item -Path "master:/sitecore/system/Modules/PowerShell/Delegated Access" -ErrorAction SilentlyContinue
    if (-not $daFolder) {
        $results += "DA_FOLDER_NOT_FOUND"
        return $results -join "|"
    }

    $daName = "Test-DA-Mapping"
    $existingDA = Get-ChildItem -Path "master:$($daFolder.Paths.FullPath)" | Where-Object { $_.Name -eq $daName }
    if ($existingDA) { $existingDA | Remove-Item -Force }

    $daItem = New-Item -Path "master:$($daFolder.Paths.FullPath)/$daName" `
        -ItemType "/sitecore/templates/Modules/PowerShell Console/PowerShell Delegated Access"
    $daItem.Editing.BeginEdit()
    $daItem["Enabled"] = "1"
    $daItem["ElevatedRole"] = $roleName
    $daItem["ImpersonatedUser"] = "sitecore\admin"
    $daItem["ScriptItem"] = $scriptItem.ID.ToString()
    $daItem.Editing.EndEdit() | Out-Null
    $results += "DA:$($daItem.ID)"

    $results -join "|"
} -Raw

Write-Host "    Setup result: $setupResult" -ForegroundColor Gray

if ($setupResult -like "*NOT_FOUND*") {
    Write-Host "    ERROR: Setup failed -- $setupResult" -ForegroundColor Red
} else {
    Write-Host "    Delegated access test items created." -ForegroundColor Green
}

Stop-ScriptSession -Session $session
