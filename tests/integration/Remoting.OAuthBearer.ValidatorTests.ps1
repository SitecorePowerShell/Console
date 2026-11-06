# OAuth Client save-time validator tests.
# Runs under Shared Secret remoting as sub-step (a) of Phase 7 - BEFORE the
# provider swap. Exercises RemotingItemEventHandler.OnItemSaving.
#
# Every test attempts a save that should be cancelled by the event handler
# and asserts either (i) the item does not exist after the attempt, or
# (ii) the offending field was not persisted to its attempted value.

Write-Host "`n  [OAuth Bearer Validator: save-time rejection rules]" -ForegroundColor White

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost

try {
    $oauthTemplateId = "{E1F946A8-86E0-4CDF-BFA7-3089E669D153}"
    # "Dangerous" - see Remoting.OAuthBearer.Setup.ps1 for why not "Unrestricted".
    $defaultPolicyId = "{4841A7E4-09D9-4F67-A4AB-67391569ABDE}"
    $clientsFolderPath = "master:/sitecore/system/Modules/PowerShell/Settings/Access/Remoting Clients"

    # Case 1: wildcard client_id rejected
    Write-Host "    Case: wildcard client_id" -ForegroundColor Gray
    $result = Invoke-RemoteScript -Session $session -ScriptBlock {
        $clientsPath = $params.clientsPath
        $tpl = $params.tpl
        $policyId = $params.policyId
        # Clean any leftover from previous runs
        $path = "$clientsPath/ValidatorTest-Wildcard"
        Get-Item -Path $path -ErrorAction SilentlyContinue | Remove-Item -Force
        try {
            $item = New-Item -Path $clientsPath -Name "ValidatorTest-Wildcard" -ItemType $tpl
            $item.Editing.BeginEdit()
            $item["AllowedIssuer"]   = "https://test-validator.local"
            $item["OAuthClientIds"] = "*"
            $item["Enabled"]          = "1"
            $item["ImpersonatedUser"] = "sitecore\admin"
            $item["Policy"]           = $policyId
            $item.Editing.EndEdit() | Out-Null
        } catch {
            # Save cancellation may or may not raise - treat both as valid rejection
        }
        $check = Get-Item -Path $path -ErrorAction SilentlyContinue
        if ($check) {
            $stored = $check["OAuthClientIds"]
            Get-Item -Path $path | Remove-Item -Force
            return "SAVED:$stored"
        }
        return "CANCELLED"
    } -Arguments @{ clientsPath = $clientsFolderPath; tpl = $oauthTemplateId; policyId = $defaultPolicyId } -Raw

    # Accept either outright cancellation or the wildcard not being persisted.
    # Use -notmatch against regex (literal asterisk) because the PS -like backtick
    # escape does not behave the way you'd expect for '*' in a wildcard pattern.
    $rejected = ($result -eq "CANCELLED") -or ($result -like "SAVED:*" -and $result -notmatch '\*')
    Assert-True $rejected "Wildcard client_id is rejected on save (result: $result)"

    # Case 2: short (< 8 chars) client_id rejected
    Write-Host "    Case: short client_id" -ForegroundColor Gray
    $result = Invoke-RemoteScript -Session $session -ScriptBlock {
        $clientsPath = $params.clientsPath
        $tpl = $params.tpl
        $policyId = $params.policyId
        $path = "$clientsPath/ValidatorTest-Short"
        Get-Item -Path $path -ErrorAction SilentlyContinue | Remove-Item -Force
        try {
            $item = New-Item -Path $clientsPath -Name "ValidatorTest-Short" -ItemType $tpl
            $item.Editing.BeginEdit()
            $item["AllowedIssuer"]   = "https://test-validator.local"
            $item["OAuthClientIds"] = "abc"
            $item["Enabled"]          = "1"
            $item["ImpersonatedUser"] = "sitecore\admin"
            $item["Policy"]           = $policyId
            $item.Editing.EndEdit() | Out-Null
        } catch {}
        $check = Get-Item -Path $path -ErrorAction SilentlyContinue
        if ($check) {
            $stored = $check["OAuthClientIds"]
            Get-Item -Path $path | Remove-Item -Force
            return "SAVED:$stored"
        }
        return "CANCELLED"
    } -Arguments @{ clientsPath = $clientsFolderPath; tpl = $oauthTemplateId; policyId = $defaultPolicyId } -Raw
    $rejected = ($result -eq "CANCELLED") -or ($result -like "SAVED:*" -and $result -notlike "*abc*")
    Assert-True $rejected "Short client_id is rejected on save (result: $result)"

    # Case 3: reserved word rejected
    Write-Host "    Case: reserved-word client_id" -ForegroundColor Gray
    $result = Invoke-RemoteScript -Session $session -ScriptBlock {
        $clientsPath = $params.clientsPath
        $tpl = $params.tpl
        $policyId = $params.policyId
        $path = "$clientsPath/ValidatorTest-Reserved"
        Get-Item -Path $path -ErrorAction SilentlyContinue | Remove-Item -Force
        try {
            $item = New-Item -Path $clientsPath -Name "ValidatorTest-Reserved" -ItemType $tpl
            $item.Editing.BeginEdit()
            $item["AllowedIssuer"]   = "https://test-validator.local"
            $item["OAuthClientIds"] = "administrator"
            $item["Enabled"]          = "1"
            $item["ImpersonatedUser"] = "sitecore\admin"
            $item["Policy"]           = $policyId
            $item.Editing.EndEdit() | Out-Null
        } catch {}
        $check = Get-Item -Path $path -ErrorAction SilentlyContinue
        if ($check) {
            $stored = $check["OAuthClientIds"]
            Get-Item -Path $path | Remove-Item -Force
            return "SAVED:$stored"
        }
        return "CANCELLED"
    } -Arguments @{ clientsPath = $clientsFolderPath; tpl = $oauthTemplateId; policyId = $defaultPolicyId } -Raw
    $rejected = ($result -eq "CANCELLED") -or ($result -like "SAVED:*" -and $result -notlike "*administrator*")
    Assert-True $rejected "Reserved-word client_id 'administrator' is rejected on save (result: $result)"

    # Case 4: duplicate (issuer, client_id) across items rejected
    Write-Host "    Case: duplicate (issuer, client_id) pair across items" -ForegroundColor Gray
    $result = Invoke-RemoteScript -Session $session -ScriptBlock {
        $clientsPath = $params.clientsPath
        $tpl = $params.tpl
        $policyId = $params.policyId
        $firstPath = "$clientsPath/ValidatorTest-Dup1"
        $secondPath = "$clientsPath/ValidatorTest-Dup2"
        Get-Item -Path $firstPath -ErrorAction SilentlyContinue | Remove-Item -Force
        Get-Item -Path $secondPath -ErrorAction SilentlyContinue | Remove-Item -Force

        # First save succeeds.
        $first = New-Item -Path $clientsPath -Name "ValidatorTest-Dup1" -ItemType $tpl
        $first.Editing.BeginEdit()
        $first["AllowedIssuer"]   = "https://test-validator.local"
        $first["OAuthClientIds"] = "duplicate-test-client-01"
        $first["Enabled"]          = "1"
        $first["ImpersonatedUser"] = "sitecore\admin"
        $first["Policy"]           = $policyId
        $first.Editing.EndEdit() | Out-Null

        # Second save tries the same (issuer, client_id) and should be cancelled.
        try {
            $second = New-Item -Path $clientsPath -Name "ValidatorTest-Dup2" -ItemType $tpl
            $second.Editing.BeginEdit()
            $second["AllowedIssuer"]   = "https://test-validator.local"
            $second["OAuthClientIds"] = "duplicate-test-client-01"
            $second["Enabled"]          = "1"
            $second["ImpersonatedUser"] = "sitecore\admin"
            $second["Policy"]           = $policyId
            $second.Editing.EndEdit() | Out-Null
        } catch {}

        $secondCheck = Get-Item -Path $secondPath -ErrorAction SilentlyContinue
        $secondStored = if ($secondCheck) { $secondCheck["OAuthClientIds"] } else { "" }

        Get-Item -Path $firstPath -ErrorAction SilentlyContinue | Remove-Item -Force
        Get-Item -Path $secondPath -ErrorAction SilentlyContinue | Remove-Item -Force

        if ($secondCheck -and $secondStored -like "*duplicate-test-client-01*") {
            return "DUPLICATED"
        }
        return "CANCELLED"
    } -Arguments @{ clientsPath = $clientsFolderPath; tpl = $oauthTemplateId; policyId = $defaultPolicyId } -Raw
    Assert-Equal $result "CANCELLED" "Duplicate (issuer, client_id) pair is rejected on save"

    # Case 5: whitespace/duplicate-in-field normalisation succeeds
    Write-Host "    Case: whitespace + in-field duplicate normalisation" -ForegroundColor Gray
    $result = Invoke-RemoteScript -Session $session -ScriptBlock {
        $clientsPath = $params.clientsPath
        $tpl = $params.tpl
        $policyId = $params.policyId
        $path = "$clientsPath/ValidatorTest-Normalise"
        Get-Item -Path $path -ErrorAction SilentlyContinue | Remove-Item -Force

        $item = New-Item -Path $clientsPath -Name "ValidatorTest-Normalise" -ItemType $tpl
        $item.Editing.BeginEdit()
        $item["AllowedIssuer"]   = "https://test-validator.local"
        # Intentional: leading space, trailing newline, and a duplicate
        $item["OAuthClientIds"] = "  normalise-test-client-01`r`nnormalise-test-client-02`r`nnormalise-test-client-01"
        $item["Enabled"]          = "1"
        $item["ImpersonatedUser"] = "sitecore\admin"
        $item["Policy"]           = $policyId
        $item.Editing.EndEdit() | Out-Null

        $saved = Get-Item -Path $path
        $stored = $saved["OAuthClientIds"]
        Get-Item -Path $path | Remove-Item -Force
        return $stored
    } -Arguments @{ clientsPath = $clientsFolderPath; tpl = $oauthTemplateId; policyId = $defaultPolicyId } -Raw

    # Expect: trimmed, exactly two entries, no duplicate
    $lines = @($result -split "`r?`n" | Where-Object { $_ })
    Assert-Equal $lines.Count 2 "Normalised value has 2 lines (got $($lines.Count): '$result')"
    Assert-True ($lines -contains "normalise-test-client-01") "Normalised value contains client-01"
    Assert-True ($lines -contains "normalise-test-client-02") "Normalised value contains client-02"

} finally {
    Stop-ScriptSession -Session $session -ErrorAction SilentlyContinue
}
