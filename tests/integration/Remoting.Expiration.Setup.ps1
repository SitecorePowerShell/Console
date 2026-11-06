# Remoting.Expiration.Setup.ps1
# Creates test Shared Secret Clients with expiration dates for key expiration enforcement tests.
# Called by Run-RemotingTests.ps1 before the expiration test phase.
# Requires: SPE Remoting enabled, shared secret configured

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost

Write-Host "`n  [Expiration Setup: creating test Shared Secret Clients]" -ForegroundColor Cyan

$createResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $remotingPath = "master:/sitecore/system/Modules/PowerShell/Settings/Access"

    $fieldIds = @{
        # RemotingPolicy.Fields
        PolicyFullLanguage = "{B7D4F2A1-3C58-4E9D-A612-8F5C7D4E3B2A}"
        PolicyAuditLevel   = "{FB657388-BF96-475D-AE69-EBF028F47432}"
        # SharedSecretClient.Fields
        KeyAccessKeyId     = "{C4D5E6F7-8A9B-4C0D-1E2F-3A4B5C6D7E8F}"
        KeySharedSecret    = "{BBF52C26-7825-4F7B-88FF-2DB2785C5954}"
        KeyEnabled         = "{8D158FCA-E8F3-4D94-8469-C782B099EC07}"
        KeyPolicy          = "{ECB2A0C9-3AC3-4FF8-A66C-6D4AE4AA2C21}"
        KeyImpersonateUser = "{5EB16BF4-605A-457C-8588-5D9833FF4DD9}"
        KeyExpires         = "{B2C3D4E5-F6A7-4B89-C0D1-E2F3A4B5C6D7}"
    }

    $policiesFolder = Get-Item -Path "$remotingPath/Policies" -ErrorAction SilentlyContinue
    $clientsFolder = Get-Item -Path "$remotingPath/Remoting Clients" -ErrorAction SilentlyContinue
    if (-not $policiesFolder -or -not $clientsFolder) { return "ERROR:FOLDERS_NOT_FOUND" }

    # Clean up leftover test items
    $existing = Get-ChildItem -Path "master:$($policiesFolder.Paths.FullPath)" -Recurse |
        Where-Object { $_.Name -like "Test-Expir*" }
    if ($existing) { $existing | Remove-Item -Force }

    $existingKeys = Get-ChildItem -Path "master:$($clientsFolder.Paths.FullPath)" -Recurse |
        Where-Object { $_.Name -like "Test-Expir*" }
    if ($existingKeys) { $existingKeys | Remove-Item -Force }

    $policyTemplate = "/sitecore/templates/Modules/PowerShell Console/Remoting/Remoting Policy"
    $sharedSecretClientTemplate = "/sitecore/templates/Modules/PowerShell Console/Remoting/Shared Secret Client"

    # Create an unrestricted policy for expiration tests
    $policy = New-Item -Path "master:$($policiesFolder.Paths.FullPath)/Test-ExpirationPolicy" -ItemType $policyTemplate
    $policy.Editing.BeginEdit()
    $policy.Fields[$fieldIds.PolicyFullLanguage].Value = "1"
    $policy.Fields[$fieldIds.PolicyAuditLevel].Value = "None"
    $policy.Editing.EndEdit() | Out-Null

    # Shared Secret Client that expired yesterday
    $expiredKey = New-Item -Path "master:$($clientsFolder.Paths.FullPath)/Test-ExpiredKey" -ItemType $sharedSecretClientTemplate
    $expiredKey.Editing.BeginEdit()
    $expiredKey.Fields[$fieldIds.KeyEnabled].Value = "1"
    $expiredKey.Fields[$fieldIds.KeyAccessKeyId].Value = "spe_test_expired_key_001"
    $expiredKey.Fields[$fieldIds.KeySharedSecret].Value = "Test-Expired-Secret-K3y!-LongEnough-For-Validation"
    $expiredKey.Fields[$fieldIds.KeyPolicy].Value = $policy.ID.ToString()
    $expiredKey.Fields[$fieldIds.KeyImpersonateUser].Value = "sitecore\admin"
    $expiredKey.Fields[$fieldIds.KeyExpires].Value = [Sitecore.DateUtil]::ToIsoDate([DateTime]::UtcNow.AddDays(-1))
    $expiredKey.Editing.EndEdit() | Out-Null

    # Shared Secret Client that expires tomorrow (still valid)
    $validKey = New-Item -Path "master:$($clientsFolder.Paths.FullPath)/Test-ExpirationValidKey" -ItemType $sharedSecretClientTemplate
    $validKey.Editing.BeginEdit()
    $validKey.Fields[$fieldIds.KeyEnabled].Value = "1"
    $validKey.Fields[$fieldIds.KeyAccessKeyId].Value = "spe_test_expvalid_key_001"
    $validKey.Fields[$fieldIds.KeySharedSecret].Value = "Test-ExpirationValid-Secret-K3y!-LongEnough-Valid"
    $validKey.Fields[$fieldIds.KeyPolicy].Value = $policy.ID.ToString()
    $validKey.Fields[$fieldIds.KeyImpersonateUser].Value = "sitecore\admin"
    $validKey.Fields[$fieldIds.KeyExpires].Value = [Sitecore.DateUtil]::ToIsoDate([DateTime]::UtcNow.AddDays(1))
    $validKey.Editing.EndEdit() | Out-Null

    # Shared Secret Client with no expiration (empty field, should work)
    $noExpiryKey = New-Item -Path "master:$($clientsFolder.Paths.FullPath)/Test-ExpirationNoExpiry" -ItemType $sharedSecretClientTemplate
    $noExpiryKey.Editing.BeginEdit()
    $noExpiryKey.Fields[$fieldIds.KeyEnabled].Value = "1"
    $noExpiryKey.Fields[$fieldIds.KeyAccessKeyId].Value = "spe_test_noexpiry_key_001"
    $noExpiryKey.Fields[$fieldIds.KeySharedSecret].Value = "Test-NoExpiry-Secret-K3y!-LongEnough-For-Validation"
    $noExpiryKey.Fields[$fieldIds.KeyPolicy].Value = $policy.ID.ToString()
    $noExpiryKey.Fields[$fieldIds.KeyImpersonateUser].Value = "sitecore\admin"
    # Expires field left empty
    $noExpiryKey.Editing.EndEdit() | Out-Null

    @(
        "CREATED"
        "Policy=$($policy.ID)"
        "ExpiredKey=$($expiredKey.ID)"
        "ValidKey=$($validKey.ID)"
        "NoExpiryKey=$($noExpiryKey.ID)"
    ) -join "|"
} -Raw

if ($createResult -like "CREATED*") {
    Write-Host "    Expiration test items created: $createResult" -ForegroundColor Green
} else {
    Write-Host "    ERROR creating test items: $createResult" -ForegroundColor Red
}

Stop-ScriptSession -Session $session
