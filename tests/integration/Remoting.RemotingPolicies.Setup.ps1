# Remoting.RemotingPolicies.Setup.ps1
# Creates test policy, Shared Secret Client, and script items for remoting policy enforcement tests.
# Called by Run-RemotingTests.ps1 before the policy test phase.
# Requires: SPE Remoting enabled, shared secret configured

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost

Write-Host "`n  [Remoting Policy Setup: creating test items]" -ForegroundColor Cyan

$createResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $remotingPath = "master:/sitecore/system/Modules/PowerShell/Settings/Access"
    $remoting = Get-Item -Path $remotingPath
    if (-not $remoting) { return "ERROR:REMOTING_NOT_FOUND" }

    # Field IDs from Templates.cs — use IDs instead of names for reliability
    $fieldIds = @{
        # RemotingPolicy.Fields
        PolicyFullLanguage   = "{B7D4F2A1-3C58-4E9D-A612-8F5C7D4E3B2A}"
        PolicyAllowedCmds    = "{5E01F1C2-27A3-4A38-8A3E-6F7E09BDE34F}"
        PolicyApprovedScripts= "{E3A9C1B4-7D56-4F28-9E83-2A1B5C6D8F47}"
        PolicyAuditLevel     = "{FB657388-BF96-475D-AE69-EBF028F47432}"
        # RemotingClient.Fields
        KeyAccessKeyId       = "{C4D5E6F7-8A9B-4C0D-1E2F-3A4B5C6D7E8F}"
        KeySharedSecret      = "{BBF52C26-7825-4F7B-88FF-2DB2785C5954}"
        KeyEnabled           = "{8D158FCA-E8F3-4D94-8469-C782B099EC07}"
        KeyPolicy            = "{ECB2A0C9-3AC3-4FF8-A66C-6D4AE4AA2C21}"
        KeyImpersonateUser   = "{5EB16BF4-605A-457C-8588-5D9833FF4DD9}"
        # Script.Fields
        ScriptBody           = "{B1A94FF0-6897-47C0-9C51-AA6ACB80B1F0}"
    }

    # Ensure Policies folder exists
    $policiesFolder = Get-Item -Path "$remotingPath/Policies" -ErrorAction SilentlyContinue
    if (-not $policiesFolder) {
        $policiesFolder = New-Item -Path "$remotingPath/Policies" -ItemType "Common/Folder"
    }

    # Ensure Remoting Clients folder exists
    $clientsFolder = Get-Item -Path "$remotingPath/Remoting Clients" -ErrorAction SilentlyContinue
    if (-not $clientsFolder) {
        $clientsFolder = New-Item -Path "$remotingPath/Remoting Clients" -ItemType "Common/Folder"
    }

    # Clean up any leftover from previous test runs
    $existing = Get-ChildItem -Path "master:$($policiesFolder.Paths.FullPath)" -Recurse |
        Where-Object { $_.Name -like "Test-*" }
    if ($existing) { $existing | Remove-Item -Force }

    $existingKeys = Get-ChildItem -Path "master:$($clientsFolder.Paths.FullPath)" -Recurse |
        Where-Object { $_.Name -like "Test-*" }
    if ($existingKeys) { $existingKeys | Remove-Item -Force }

    # Clean up test scripts from previous runs (under Test/Web API for v2 endpoint access)
    $webApiRoot = Get-Item -Path "master:/sitecore/system/Modules/PowerShell/Script Library/Test/Web API"
    if ($webApiRoot) {
        $existingScripts = Get-ChildItem -Path "master:$($webApiRoot.Paths.FullPath)" |
            Where-Object { $_.Name -like "Test-*" }
        if ($existingScripts) { $existingScripts | Remove-Item -Recurse -Force }
    }

    $policyTemplate = "/sitecore/templates/Modules/PowerShell Console/Remoting/Remoting Policy"
    $sharedSecretClientTemplate = "/sitecore/templates/Modules/PowerShell Console/Remoting/Shared Secret Client"
    $scriptTemplate = "/sitecore/templates/Modules/PowerShell Console/PowerShell Script"
    $scriptLibraryTemplate = "/sitecore/templates/Modules/PowerShell Console/PowerShell Script Library"

    # =========================================================================
    # 1. Create test Script Library items (before policies, so we have IDs)
    # =========================================================================

    if (-not $webApiRoot) { return "ERROR:WEBAPI_ROOT_NOT_FOUND" }

    # Approved script: calls Remove-Item (not in read-only allowlist) to verify command allowlist bypass.
    # Runs under ConstrainedLanguage because the Test-ReadOnly policy has Full Language unchecked.
    $approvedScript = New-Item -Path "master:$($webApiRoot.Paths.FullPath)/Test-ApprovedWriteScript" -ItemType $scriptTemplate
    $approvedScript.Editing.BeginEdit()
    $approvedScript.Fields[$fieldIds.ScriptBody].Value = @"
Remove-Item -Path "master:/content/nonexistent-approval-test" -ErrorAction SilentlyContinue
"APPROVED_SCRIPT_OK"
"@
    $approvedScript.Editing.EndEdit() | Out-Null

    # Unapproved script: same commands but NOT added to policy's Approved Scripts
    $unapprovedScript = New-Item -Path "master:$($webApiRoot.Paths.FullPath)/Test-UnapprovedWriteScript" -ItemType $scriptTemplate
    $unapprovedScript.Editing.BeginEdit()
    $unapprovedScript.Fields[$fieldIds.ScriptBody].Value = @"
Remove-Item -Path "master:/content/nonexistent-unapproved-test" -ErrorAction SilentlyContinue
"UNAPPROVED_SCRIPT_OK"
"@
    $unapprovedScript.Editing.EndEdit() | Out-Null

    # =========================================================================
    # 2. Create policies (with approved script references)
    # =========================================================================

    # Read-only policy: ConstrainedLanguage + allowlist of read commands + one approved script
    $readOnly = New-Item -Path "master:$($policiesFolder.Paths.FullPath)/Test-ReadOnly" -ItemType $policyTemplate
    $readOnly.Editing.BeginEdit()
    # Full Language left unchecked = ConstrainedLanguage (default)
    $readOnly.Fields[$fieldIds.PolicyAllowedCmds].Value = @"
Get-Item
Get-ChildItem
Get-Database
Get-ItemField
Get-ItemTemplate
Get-Variable
Set-Variable
Select-Object
Where-Object
ForEach-Object
Sort-Object
Measure-Object
Format-Table
Format-List
Write-Host
Write-Output
Write-Error
Out-String
Out-Null
Test-Path
Resolve-Path
Get-Date
ConvertTo-Json
ConvertFrom-Json
New-PSObject
Publish-Item
"@
    $readOnly.Fields[$fieldIds.PolicyApprovedScripts].Value = $approvedScript.ID.ToString()
    $readOnly.Fields[$fieldIds.PolicyAuditLevel].Value = "Violations"
    $readOnly.Editing.EndEdit() | Out-Null

    # Unrestricted policy: FullLanguage + no command restrictions
    $unrestricted = New-Item -Path "master:$($policiesFolder.Paths.FullPath)/Test-Unrestricted" -ItemType $policyTemplate
    $unrestricted.Editing.BeginEdit()
    $unrestricted.Fields[$fieldIds.PolicyFullLanguage].Value = "1"
    $unrestricted.Fields[$fieldIds.PolicyAuditLevel].Value = "None"
    $unrestricted.Editing.EndEdit() | Out-Null

    # Standard audit policy: FullLanguage + Standard audit level
    $standardAudit = New-Item -Path "master:$($policiesFolder.Paths.FullPath)/Test-StandardAudit" -ItemType $policyTemplate
    $standardAudit.Editing.BeginEdit()
    $standardAudit.Fields[$fieldIds.PolicyFullLanguage].Value = "1"
    $standardAudit.Fields[$fieldIds.PolicyAuditLevel].Value = "Standard"
    $standardAudit.Editing.EndEdit() | Out-Null

    # Full audit policy: FullLanguage + Full audit level
    $fullAudit = New-Item -Path "master:$($policiesFolder.Paths.FullPath)/Test-FullAudit" -ItemType $policyTemplate
    $fullAudit.Editing.BeginEdit()
    $fullAudit.Fields[$fieldIds.PolicyFullLanguage].Value = "1"
    $fullAudit.Fields[$fieldIds.PolicyAuditLevel].Value = "Full"
    $fullAudit.Editing.EndEdit() | Out-Null

    # =========================================================================
    # 3. Create Shared Secret Clients
    # =========================================================================

    # Shared Secret Client bound to the read-only policy (Droplink stores item ID)
    $clientReadOnly = New-Item -Path "master:$($clientsFolder.Paths.FullPath)/Test-ReadOnlyKey" -ItemType $sharedSecretClientTemplate
    $clientReadOnly.Editing.BeginEdit()
    $clientReadOnly.Fields[$fieldIds.KeyEnabled].Value = "1"
    $clientReadOnly.Fields[$fieldIds.KeyAccessKeyId].Value = "spe_test_readonly_key_001"
    $clientReadOnly.Fields[$fieldIds.KeySharedSecret].Value = "Test-ReadOnly-Secret-K3y!-LongEnough-For-Validation"
    $clientReadOnly.Fields[$fieldIds.KeyPolicy].Value = $readOnly.ID.ToString()
    $clientReadOnly.Fields[$fieldIds.KeyImpersonateUser].Value = "sitecore\admin"
    $clientReadOnly.Editing.EndEdit() | Out-Null

    # Shared Secret Client with no policy assigned - should be denied (policy required)
    $clientNoPolicy = New-Item -Path "master:$($clientsFolder.Paths.FullPath)/Test-NoPolicyKey" -ItemType $sharedSecretClientTemplate
    $clientNoPolicy.Editing.BeginEdit()
    $clientNoPolicy.Fields[$fieldIds.KeyEnabled].Value = "1"
    $clientNoPolicy.Fields[$fieldIds.KeyAccessKeyId].Value = "spe_test_nopolicy_key_001"
    $clientNoPolicy.Fields[$fieldIds.KeySharedSecret].Value = "Test-NoPolicy-Secret-K3y!-LongEnough-For-Validation"
    $clientNoPolicy.Fields[$fieldIds.KeyImpersonateUser].Value = "sitecore\admin"
    $clientNoPolicy.Editing.EndEdit() | Out-Null

    # Shared Secret Client bound to the standard audit policy
    $clientStandardAudit = New-Item -Path "master:$($clientsFolder.Paths.FullPath)/Test-StandardAuditKey" -ItemType $sharedSecretClientTemplate
    $clientStandardAudit.Editing.BeginEdit()
    $clientStandardAudit.Fields[$fieldIds.KeyEnabled].Value = "1"
    $clientStandardAudit.Fields[$fieldIds.KeyAccessKeyId].Value = "spe_test_stdaudit_key_001"
    $clientStandardAudit.Fields[$fieldIds.KeySharedSecret].Value = "Test-StandardAudit-Secret-K3y!-LongEnough-For-Validation"
    $clientStandardAudit.Fields[$fieldIds.KeyPolicy].Value = $standardAudit.ID.ToString()
    $clientStandardAudit.Fields[$fieldIds.KeyImpersonateUser].Value = "sitecore\admin"
    $clientStandardAudit.Editing.EndEdit() | Out-Null

    # Shared Secret Client bound to the full audit policy
    $clientFullAudit = New-Item -Path "master:$($clientsFolder.Paths.FullPath)/Test-FullAuditKey" -ItemType $sharedSecretClientTemplate
    $clientFullAudit.Editing.BeginEdit()
    $clientFullAudit.Fields[$fieldIds.KeyEnabled].Value = "1"
    $clientFullAudit.Fields[$fieldIds.KeyAccessKeyId].Value = "spe_test_fullaudit_key_001"
    $clientFullAudit.Fields[$fieldIds.KeySharedSecret].Value = "Test-FullAudit-Secret-K3y!-LongEnough-For-Validation"
    $clientFullAudit.Fields[$fieldIds.KeyPolicy].Value = $fullAudit.ID.ToString()
    $clientFullAudit.Fields[$fieldIds.KeyImpersonateUser].Value = "sitecore\admin"
    $clientFullAudit.Editing.EndEdit() | Out-Null

    @(
        "CREATED"
        "ReadOnly=$($readOnly.ID)"
        "Unrestricted=$($unrestricted.ID)"
        "StandardAudit=$($standardAudit.ID)"
        "FullAudit=$($fullAudit.ID)"
        "ClientReadOnly=$($clientReadOnly.ID)"
        "ClientNoPolicy=$($clientNoPolicy.ID)"
        "ClientStandardAudit=$($clientStandardAudit.ID)"
        "ClientFullAudit=$($clientFullAudit.ID)"
        "ApprovedScript=$($approvedScript.ID)"
        "UnapprovedScript=$($unapprovedScript.ID)"
        "ScriptFolder=$($webApiRoot.Paths.FullPath)"
    ) -join "|"
} -Raw

if ($createResult -like "CREATED*") {
    Write-Host "    Test items created: $createResult" -ForegroundColor Green
} else {
    Write-Host "    ERROR creating test items: $createResult" -ForegroundColor Red
}

Stop-ScriptSession -Session $session
