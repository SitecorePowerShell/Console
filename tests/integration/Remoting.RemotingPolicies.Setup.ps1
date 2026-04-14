# Remoting.RemotingPolicies.Setup.ps1
# Creates test policy, API Key, and script items for remoting policy enforcement tests.
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
        # RemotingApiKey.Fields
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

    # Ensure API Keys folder exists
    $apiKeysFolder = Get-Item -Path "$remotingPath/API Keys" -ErrorAction SilentlyContinue
    if (-not $apiKeysFolder) {
        $apiKeysFolder = New-Item -Path "$remotingPath/API Keys" -ItemType "Common/Folder"
    }

    # Clean up any leftover from previous test runs
    $existing = Get-ChildItem -Path "master:$($policiesFolder.Paths.FullPath)" -Recurse |
        Where-Object { $_.Name -like "Test-*" }
    if ($existing) { $existing | Remove-Item -Force }

    $existingKeys = Get-ChildItem -Path "master:$($apiKeysFolder.Paths.FullPath)" -Recurse |
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
    $apiKeyTemplate = "/sitecore/templates/Modules/PowerShell Console/Remoting/Remoting API Key"
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
    # 3. Create API Keys
    # =========================================================================

    # API Key bound to the read-only policy (Droplink stores item ID)
    $apiKeyReadOnly = New-Item -Path "master:$($apiKeysFolder.Paths.FullPath)/Test-ReadOnlyKey" -ItemType $apiKeyTemplate
    $apiKeyReadOnly.Editing.BeginEdit()
    $apiKeyReadOnly.Fields[$fieldIds.KeyEnabled].Value = "1"
    $apiKeyReadOnly.Fields[$fieldIds.KeyAccessKeyId].Value = "spe_test_readonly_key_001"
    $apiKeyReadOnly.Fields[$fieldIds.KeySharedSecret].Value = "Test-ReadOnly-Secret-K3y!-LongEnough-For-Validation"
    $apiKeyReadOnly.Fields[$fieldIds.KeyPolicy].Value = $readOnly.ID.ToString()
    $apiKeyReadOnly.Fields[$fieldIds.KeyImpersonateUser].Value = "sitecore\admin"
    $apiKeyReadOnly.Editing.EndEdit() | Out-Null

    # API Key with no policy assigned - should be denied (policy required)
    $apiKeyNoPolicy = New-Item -Path "master:$($apiKeysFolder.Paths.FullPath)/Test-NoPolicyKey" -ItemType $apiKeyTemplate
    $apiKeyNoPolicy.Editing.BeginEdit()
    $apiKeyNoPolicy.Fields[$fieldIds.KeyEnabled].Value = "1"
    $apiKeyNoPolicy.Fields[$fieldIds.KeyAccessKeyId].Value = "spe_test_nopolicy_key_001"
    $apiKeyNoPolicy.Fields[$fieldIds.KeySharedSecret].Value = "Test-NoPolicy-Secret-K3y!-LongEnough-For-Validation"
    $apiKeyNoPolicy.Fields[$fieldIds.KeyImpersonateUser].Value = "sitecore\admin"
    $apiKeyNoPolicy.Editing.EndEdit() | Out-Null

    # API Key bound to the standard audit policy
    $apiKeyStandardAudit = New-Item -Path "master:$($apiKeysFolder.Paths.FullPath)/Test-StandardAuditKey" -ItemType $apiKeyTemplate
    $apiKeyStandardAudit.Editing.BeginEdit()
    $apiKeyStandardAudit.Fields[$fieldIds.KeyEnabled].Value = "1"
    $apiKeyStandardAudit.Fields[$fieldIds.KeyAccessKeyId].Value = "spe_test_stdaudit_key_001"
    $apiKeyStandardAudit.Fields[$fieldIds.KeySharedSecret].Value = "Test-StandardAudit-Secret-K3y!-LongEnough-For-Validation"
    $apiKeyStandardAudit.Fields[$fieldIds.KeyPolicy].Value = $standardAudit.ID.ToString()
    $apiKeyStandardAudit.Fields[$fieldIds.KeyImpersonateUser].Value = "sitecore\admin"
    $apiKeyStandardAudit.Editing.EndEdit() | Out-Null

    # API Key bound to the full audit policy
    $apiKeyFullAudit = New-Item -Path "master:$($apiKeysFolder.Paths.FullPath)/Test-FullAuditKey" -ItemType $apiKeyTemplate
    $apiKeyFullAudit.Editing.BeginEdit()
    $apiKeyFullAudit.Fields[$fieldIds.KeyEnabled].Value = "1"
    $apiKeyFullAudit.Fields[$fieldIds.KeyAccessKeyId].Value = "spe_test_fullaudit_key_001"
    $apiKeyFullAudit.Fields[$fieldIds.KeySharedSecret].Value = "Test-FullAudit-Secret-K3y!-LongEnough-For-Validation"
    $apiKeyFullAudit.Fields[$fieldIds.KeyPolicy].Value = $fullAudit.ID.ToString()
    $apiKeyFullAudit.Fields[$fieldIds.KeyImpersonateUser].Value = "sitecore\admin"
    $apiKeyFullAudit.Editing.EndEdit() | Out-Null

    @(
        "CREATED"
        "ReadOnly=$($readOnly.ID)"
        "Unrestricted=$($unrestricted.ID)"
        "StandardAudit=$($standardAudit.ID)"
        "FullAudit=$($fullAudit.ID)"
        "ApiKeyReadOnly=$($apiKeyReadOnly.ID)"
        "ApiKeyNoPolicy=$($apiKeyNoPolicy.ID)"
        "ApiKeyStandardAudit=$($apiKeyStandardAudit.ID)"
        "ApiKeyFullAudit=$($apiKeyFullAudit.ID)"
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
