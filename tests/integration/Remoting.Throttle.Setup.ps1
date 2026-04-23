# Remoting.Throttle.Setup.ps1
# Creates test API Key items with throttle settings for Block and Bypass actions.
# Called by Run-RemotingTests.ps1 before the throttle test phase.
# Requires: SPE Remoting enabled, shared secret configured

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost

Write-Host "`n  [Throttle Setup: creating test API Keys]" -ForegroundColor Cyan

$createResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $remotingPath = "master:/sitecore/system/Modules/PowerShell/Settings/Access"
    $remoting = Get-Item -Path $remotingPath
    if (-not $remoting) { return "ERROR:REMOTING_NOT_FOUND" }

    # Field IDs from Templates.cs
    $fieldIds = @{
        KeyAccessKeyId       = "{C4D5E6F7-8A9B-4C0D-1E2F-3A4B5C6D7E8F}"
        KeySharedSecret      = "{BBF52C26-7825-4F7B-88FF-2DB2785C5954}"
        KeyEnabled           = "{8D158FCA-E8F3-4D94-8469-C782B099EC07}"
        KeyPolicy            = "{ECB2A0C9-3AC3-4FF8-A66C-6D4AE4AA2C21}"
        KeyImpersonateUser   = "{5EB16BF4-605A-457C-8588-5D9833FF4DD9}"
        KeyRequestLimit      = "{33D88116-A954-4954-A94C-A7AE083BC983}"
        KeyThrottleWindow    = "{9F12735C-65C2-401E-A499-3C3597452440}"
        KeyThrottleAction    = "{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}"
        PolicyFullLanguage   = "{B7D4F2A1-3C58-4E9D-A612-8F5C7D4E3B2A}"
        PolicyAuditLevel     = "{FB657388-BF96-475D-AE69-EBF028F47432}"
    }

    # Ensure API Keys folder exists
    $apiKeysFolder = Get-Item -Path "$remotingPath/Remoting Clients" -ErrorAction SilentlyContinue
    if (-not $apiKeysFolder) {
        $apiKeysFolder = New-Item -Path "$remotingPath/Remoting Clients" -ItemType "Common/Folder"
    }

    # Ensure Policies folder exists
    $policiesFolder = Get-Item -Path "$remotingPath/Policies" -ErrorAction SilentlyContinue
    if (-not $policiesFolder) {
        $policiesFolder = New-Item -Path "$remotingPath/Policies" -ItemType "Common/Folder"
    }

    # Clean up any leftover throttle test items
    $existingPolicies = Get-ChildItem -Path "master:$($policiesFolder.Paths.FullPath)" -Recurse |
        Where-Object { $_.Name -like "Test-Throttle*" }
    if ($existingPolicies) { $existingPolicies | Remove-Item -Force }

    $existingKeys = Get-ChildItem -Path "master:$($apiKeysFolder.Paths.FullPath)" -Recurse |
        Where-Object { $_.Name -like "Test-Throttle*" }
    if ($existingKeys) { $existingKeys | Remove-Item -Force }

    $apiKeyTemplate = "/sitecore/templates/Modules/PowerShell Console/Remoting/Shared Secret Client"
    $policyTemplate = "/sitecore/templates/Modules/PowerShell Console/Remoting/Remoting Policy"

    # =========================================================================
    # 0. Create unrestricted policy for throttle tests
    # =========================================================================
    $policy = New-Item -Path "master:$($policiesFolder.Paths.FullPath)/Test-ThrottlePolicy" -ItemType $policyTemplate
    $policy.Editing.BeginEdit()
    $policy.Fields[$fieldIds.PolicyFullLanguage].Value = "1"
    $policy.Fields[$fieldIds.PolicyAuditLevel].Value = "None"
    $policy.Editing.EndEdit() | Out-Null

    # =========================================================================
    # 1. API Key with Block throttle action (limit=3, window=60s)
    # =========================================================================
    $blockKey = New-Item -Path "master:$($apiKeysFolder.Paths.FullPath)/Test-ThrottleBlock" -ItemType $apiKeyTemplate
    $blockKey.Editing.BeginEdit()
    $blockKey.Fields[$fieldIds.KeyEnabled].Value = "1"
    $blockKey.Fields[$fieldIds.KeyAccessKeyId].Value = "spe_test_throttle_block_01"
    $blockKey.Fields[$fieldIds.KeySharedSecret].Value = "Test-ThrottleBlock-Secret-K3y!-LongEnough-For-Validation"
    $blockKey.Fields[$fieldIds.KeyPolicy].Value = $policy.ID.ToString()
    $blockKey.Fields[$fieldIds.KeyRequestLimit].Value = "3"
    $blockKey.Fields[$fieldIds.KeyThrottleWindow].Value = "60"
    $blockKey.Fields[$fieldIds.KeyThrottleAction].Value = "Block"
    $blockKey.Fields[$fieldIds.KeyImpersonateUser].Value = "sitecore\admin"
    $blockKey.Editing.EndEdit() | Out-Null

    # =========================================================================
    # 2. API Key with Bypass throttle action (limit=3, window=60s)
    # =========================================================================
    $bypassKey = New-Item -Path "master:$($apiKeysFolder.Paths.FullPath)/Test-ThrottleBypass" -ItemType $apiKeyTemplate
    $bypassKey.Editing.BeginEdit()
    $bypassKey.Fields[$fieldIds.KeyEnabled].Value = "1"
    $bypassKey.Fields[$fieldIds.KeyAccessKeyId].Value = "spe_test_throttle_bypass01"
    $bypassKey.Fields[$fieldIds.KeySharedSecret].Value = "Test-ThrottleBypass-Secret-K3y!-LongEnough-For-Validation"
    $bypassKey.Fields[$fieldIds.KeyPolicy].Value = $policy.ID.ToString()
    $bypassKey.Fields[$fieldIds.KeyRequestLimit].Value = "3"
    $bypassKey.Fields[$fieldIds.KeyThrottleWindow].Value = "60"
    $bypassKey.Fields[$fieldIds.KeyThrottleAction].Value = "Bypass"
    $bypassKey.Fields[$fieldIds.KeyImpersonateUser].Value = "sitecore\admin"
    $bypassKey.Editing.EndEdit() | Out-Null

    # =========================================================================
    # 3. API Key with default throttle action (no value set -- should default to Block)
    # =========================================================================
    $defaultKey = New-Item -Path "master:$($apiKeysFolder.Paths.FullPath)/Test-ThrottleDefault" -ItemType $apiKeyTemplate
    $defaultKey.Editing.BeginEdit()
    $defaultKey.Fields[$fieldIds.KeyEnabled].Value = "1"
    $defaultKey.Fields[$fieldIds.KeyAccessKeyId].Value = "spe_test_throttle_deflt_01"
    $defaultKey.Fields[$fieldIds.KeySharedSecret].Value = "Test-ThrottleDefault-Secret-K3y!-LongEnough-For-Validation"
    $defaultKey.Fields[$fieldIds.KeyPolicy].Value = $policy.ID.ToString()
    $defaultKey.Fields[$fieldIds.KeyRequestLimit].Value = "3"
    $defaultKey.Fields[$fieldIds.KeyThrottleWindow].Value = "60"
    # ThrottleAction left empty -- should default to Block
    $defaultKey.Fields[$fieldIds.KeyImpersonateUser].Value = "sitecore\admin"
    $defaultKey.Editing.EndEdit() | Out-Null

    @(
        "CREATED"
        "Policy=$($policy.ID)"
        "BlockKey=$($blockKey.ID)"
        "BypassKey=$($bypassKey.ID)"
        "DefaultKey=$($defaultKey.ID)"
    ) -join "|"
} -Raw

if ($createResult -like "CREATED*") {
    Write-Host "    Throttle test items created: $createResult" -ForegroundColor Green
} else {
    Write-Host "    ERROR creating throttle test items: $createResult" -ForegroundColor Red
}

Stop-ScriptSession -Session $session
