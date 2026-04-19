# Remoting.ClientRetry.Setup.ps1
# Creates test API Keys for client-side retry behavior tests.
# Short ThrottleWindow (3s) keeps test duration manageable.
# Called by Run-RemotingTests.ps1 before the client-retry test phase.

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost

Write-Host "`n  [ClientRetry Setup: creating test API Keys]" -ForegroundColor Cyan

$createResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $remotingPath = "master:/sitecore/system/Modules/PowerShell/Settings/Access"
    $remoting = Get-Item -Path $remotingPath
    if (-not $remoting) { return "ERROR:REMOTING_NOT_FOUND" }

    $fieldIds = @{
        KeyAccessKeyId       = "{C4D5E6F7-8A9B-4C0D-1E2F-3A4B5C6D7E8F}"
        KeySharedSecret      = "{BBF52C26-7825-4F7B-88FF-2DB2785C5954}"
        KeyEnabled           = "{8D158FCA-E8F3-4D94-8469-C782B099EC07}"
        KeyPolicy            = "{ECB2A0C9-3AC3-4FF8-A66C-6D4AE4AA2C21}"
        KeyImpersonateUser   = "{5EB16BF4-605A-457C-8588-5D9833FF4DD9}"
        KeyRequestLimit      = "{33D88116-A954-4954-A94C-A7AE083BC983}"
        KeyThrottleWindow    = "{9F12735C-65C2-401E-A499-3C3597452440}"
        KeyThrottleAction    = "{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}"
        KeyExpires           = "{B2C3D4E5-F6A7-4B89-C0D1-E2F3A4B5C6D7}"
        PolicyFullLanguage   = "{B7D4F2A1-3C58-4E9D-A612-8F5C7D4E3B2A}"
        PolicyAuditLevel     = "{FB657388-BF96-475D-AE69-EBF028F47432}"
    }

    $apiKeysFolder = Get-Item -Path "$remotingPath/API Keys" -ErrorAction SilentlyContinue
    if (-not $apiKeysFolder) {
        $apiKeysFolder = New-Item -Path "$remotingPath/API Keys" -ItemType "Common/Folder"
    }
    $policiesFolder = Get-Item -Path "$remotingPath/Policies" -ErrorAction SilentlyContinue
    if (-not $policiesFolder) {
        $policiesFolder = New-Item -Path "$remotingPath/Policies" -ItemType "Common/Folder"
    }

    $existingPolicies = Get-ChildItem -Path "master:$($policiesFolder.Paths.FullPath)" -Recurse |
        Where-Object { $_.Name -like "Test-ClientRetry*" }
    if ($existingPolicies) { $existingPolicies | Remove-Item -Force }
    $existingKeys = Get-ChildItem -Path "master:$($apiKeysFolder.Paths.FullPath)" -Recurse |
        Where-Object { $_.Name -like "Test-ClientRetry*" }
    if ($existingKeys) { $existingKeys | Remove-Item -Force }

    $apiKeyTemplate = "/sitecore/templates/Modules/PowerShell Console/Remoting/Remoting API Key"
    $policyTemplate = "/sitecore/templates/Modules/PowerShell Console/Remoting/Remoting Policy"

    $policy = New-Item -Path "master:$($policiesFolder.Paths.FullPath)/Test-ClientRetryPolicy" -ItemType $policyTemplate
    $policy.Editing.BeginEdit()
    $policy.Fields[$fieldIds.PolicyFullLanguage].Value = "1"
    $policy.Fields[$fieldIds.PolicyAuditLevel].Value = "None"
    $policy.Editing.EndEdit() | Out-Null

    # Tight-window key: RequestLimit=1, Window=3s (Retry-After will be 1-3s)
    $retryKey = New-Item -Path "master:$($apiKeysFolder.Paths.FullPath)/Test-ClientRetryQuick" -ItemType $apiKeyTemplate
    $retryKey.Editing.BeginEdit()
    $retryKey.Fields[$fieldIds.KeyEnabled].Value = "1"
    $retryKey.Fields[$fieldIds.KeyAccessKeyId].Value = "spe_test_client_retry_01"
    $retryKey.Fields[$fieldIds.KeySharedSecret].Value = "Test-ClientRetry-Secret-K3y!-LongEnough-For-Validation"
    $retryKey.Fields[$fieldIds.KeyPolicy].Value = $policy.ID.ToString()
    $retryKey.Fields[$fieldIds.KeyRequestLimit].Value = "1"
    $retryKey.Fields[$fieldIds.KeyThrottleWindow].Value = "3"
    $retryKey.Fields[$fieldIds.KeyThrottleAction].Value = "Block"
    $retryKey.Fields[$fieldIds.KeyImpersonateUser].Value = "sitecore\admin"
    $retryKey.Editing.EndEdit() | Out-Null

    # Observability key: RequestLimit=5, Window=60s (for Gap 4 rate-limit verbose tests)
    $obsKey = New-Item -Path "master:$($apiKeysFolder.Paths.FullPath)/Test-ClientRetryObs" -ItemType $apiKeyTemplate
    $obsKey.Editing.BeginEdit()
    $obsKey.Fields[$fieldIds.KeyEnabled].Value = "1"
    $obsKey.Fields[$fieldIds.KeyAccessKeyId].Value = "spe_test_client_obs_01"
    $obsKey.Fields[$fieldIds.KeySharedSecret].Value = "Test-ClientObs-Secret-K3y!-LongEnough-For-Validation"
    $obsKey.Fields[$fieldIds.KeyPolicy].Value = $policy.ID.ToString()
    $obsKey.Fields[$fieldIds.KeyRequestLimit].Value = "5"
    $obsKey.Fields[$fieldIds.KeyThrottleWindow].Value = "60"
    $obsKey.Fields[$fieldIds.KeyThrottleAction].Value = "Block"
    $obsKey.Fields[$fieldIds.KeyImpersonateUser].Value = "sitecore\admin"
    $obsKey.Editing.EndEdit() | Out-Null

    # Disabled key (for Gap 3 - X-SPE-AuthFailureReason=disabled)
    $disabledKey = New-Item -Path "master:$($apiKeysFolder.Paths.FullPath)/Test-ClientRetryDisabled" -ItemType $apiKeyTemplate
    $disabledKey.Editing.BeginEdit()
    $disabledKey.Fields[$fieldIds.KeyEnabled].Value = "0"
    $disabledKey.Fields[$fieldIds.KeyAccessKeyId].Value = "spe_test_client_disabled_01"
    $disabledKey.Fields[$fieldIds.KeySharedSecret].Value = "Test-ClientDisabled-Secret-K3y!-LongEnough-For-Validation"
    $disabledKey.Fields[$fieldIds.KeyPolicy].Value = $policy.ID.ToString()
    $disabledKey.Fields[$fieldIds.KeyImpersonateUser].Value = "sitecore\admin"
    $disabledKey.Editing.EndEdit() | Out-Null

    @(
        "CREATED"
        "Policy=$($policy.ID)"
        "RetryKey=$($retryKey.ID)"
        "ObsKey=$($obsKey.ID)"
        "DisabledKey=$($disabledKey.ID)"
    ) -join "|"
} -Raw

if ($createResult -like "CREATED*") {
    Write-Host "    ClientRetry test items created: $createResult" -ForegroundColor Green
} else {
    Write-Host "    ERROR creating ClientRetry test items: $createResult" -ForegroundColor Red
}

Stop-ScriptSession -Session $session
