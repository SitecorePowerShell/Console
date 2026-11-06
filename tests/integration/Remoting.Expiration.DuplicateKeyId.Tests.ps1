# Remoting Tests - Duplicate Access Key Id Rejection
# Tests that the save handler rejects Shared Secret Clients with duplicate Access Key Ids.
# Run via: .\Run-RemotingTests.ps1 (run in the expiration phase)
# Requires: SPE Remoting enabled, shared secret configured

# =============================================================================
# Test Group 1: Duplicate Access Key Id Blocked on Save
# =============================================================================
Write-Host "`n  [Test Group 1: Duplicate Access Key Id Blocked on Save]" -ForegroundColor White

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost

$duplicateResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $securityPath = "master:/sitecore/system/Modules/PowerShell/Settings/Access"

    $fieldIds = @{
        KeyAccessKeyId     = "{C4D5E6F7-8A9B-4C0D-1E2F-3A4B5C6D7E8F}"
        KeySharedSecret    = "{BBF52C26-7825-4F7B-88FF-2DB2785C5954}"
        KeyEnabled         = "{8D158FCA-E8F3-4D94-8469-C782B099EC07}"
        KeyImpersonateUser = "{5EB16BF4-605A-457C-8588-5D9833FF4DD9}"
    }

    $clientsFolder = Get-Item -Path "$securityPath/Remoting Clients" -ErrorAction SilentlyContinue
    if (-not $clientsFolder) { return "ERROR:FOLDER_NOT_FOUND" }

    $sharedSecretClientTemplate = "/sitecore/templates/Modules/PowerShell Console/Remoting/Shared Secret Client"
    $duplicateKeyId = "spe_test_duplicate_check_001"

    # Clean up leftovers
    $existing = Get-ChildItem -Path "master:$($clientsFolder.Paths.FullPath)" -Recurse |
        Where-Object { $_.Name -like "Test-Duplicate*" }
    if ($existing) { $existing | Remove-Item -Force }

    # Create the first key
    $key1 = New-Item -Path "master:$($clientsFolder.Paths.FullPath)/Test-DuplicateFirst" -ItemType $sharedSecretClientTemplate
    $key1.Editing.BeginEdit()
    $key1.Fields[$fieldIds.KeyAccessKeyId].Value = $duplicateKeyId
    $key1.Fields[$fieldIds.KeySharedSecret].Value = "Test-Duplicate-First-Secret-LongEnough-For-Validation"
    $key1.Fields[$fieldIds.KeyEnabled].Value = "1"
    $key1.Fields[$fieldIds.KeyImpersonateUser].Value = "sitecore\admin"
    $key1.Editing.EndEdit() | Out-Null

    # Create a second key with the same Access Key Id - should be blocked by save handler
    $key2 = New-Item -Path "master:$($clientsFolder.Paths.FullPath)/Test-DuplicateSecond" -ItemType $sharedSecretClientTemplate
    $key2.Editing.BeginEdit()
    $key2.Fields[$fieldIds.KeyAccessKeyId].Value = $duplicateKeyId
    $key2.Fields[$fieldIds.KeySharedSecret].Value = "Test-Duplicate-Second-Secret-LongEnough-For-Valid"
    $key2.Fields[$fieldIds.KeyEnabled].Value = "1"
    $key2.Fields[$fieldIds.KeyImpersonateUser].Value = "sitecore\admin"
    $saveResult = $key2.Editing.EndEdit()

    # Check if the second key's Access Key Id was actually persisted
    $key2Reloaded = Get-Item -Path "master:" -ID $key2.ID
    $key2SavedKeyId = $key2Reloaded.Fields[$fieldIds.KeyAccessKeyId].Value

    # Create a third key with a unique Access Key Id - should succeed
    $key3 = New-Item -Path "master:$($clientsFolder.Paths.FullPath)/Test-DuplicateUnique" -ItemType $sharedSecretClientTemplate
    $key3.Editing.BeginEdit()
    $key3.Fields[$fieldIds.KeyAccessKeyId].Value = "spe_test_unique_check_001"
    $key3.Fields[$fieldIds.KeySharedSecret].Value = "Test-Unique-Secret-K3y!-LongEnough-For-Validation"
    $key3.Fields[$fieldIds.KeyEnabled].Value = "1"
    $key3.Fields[$fieldIds.KeyImpersonateUser].Value = "sitecore\admin"
    $uniqueSaveResult = $key3.Editing.EndEdit()

    # Clean up
    @($key1, $key2, $key3) | Remove-Item -Force

    @(
        "SaveResult=$saveResult"
        "Key2SavedKeyId=$key2SavedKeyId"
        "UniqueSaveResult=$uniqueSaveResult"
    ) -join "|"
} -Raw

Stop-ScriptSession -Session $session

# Parse results
$parts = @{}
if ($duplicateResult) {
    $duplicateResult -split '\|' | ForEach-Object {
        $kv = $_ -split '=', 2
        if ($kv.Length -eq 2) { $parts[$kv[0]] = $kv[1] }
    }
}

# The save handler should have cancelled the save (EndEdit returns false)
Assert-Equal $parts['SaveResult'] "False" "Duplicate Access Key Id save is rejected"

# The duplicate key's Access Key Id field should be empty (save was cancelled)
Assert-True ([string]::IsNullOrEmpty($parts['Key2SavedKeyId'])) "Duplicate key field was not persisted"

# A unique key should save successfully
Assert-Equal $parts['UniqueSaveResult'] "True" "Unique Access Key Id save succeeds"
