# Remoting.OAuthBearer.Setup.ps1
# Creates an OAuth Client test fixture before Phase 7 switches the CM's
# active auth provider to OAuth. Uses SPE remoting + Shared Secret, which
# is what's still active at this point in the run.
#
# The fixture is consumed by Remoting.OAuthBearer.Tests.ps1 after the
# provider swap. Teardown removes it and restores field state.

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost

Write-Host "`n  [OAuth Bearer Setup: creating IntegrationTest OAuth Client]" -ForegroundColor Cyan

$idHost = Get-EnvValue "ID_HOST"
$allowedIssuer = "https://$idHost"

$createResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $oauthTemplateId  = "{E1F946A8-86E0-4CDF-BFA7-3089E669D153}"
    $policyTemplateId = "{AF864A3C-6D3D-4889-AFEF-9B1D427F4EA8}"
    $policiesPath     = "master:/sitecore/system/Modules/PowerShell/Settings/Access/Policies"
    $clientsFolderPath = "master:/sitecore/system/Modules/PowerShell/Settings/Access/Remoting Clients"

    $clientsFolder = Get-Item -Path $clientsFolderPath -ErrorAction SilentlyContinue
    if (-not $clientsFolder) { return "ERROR:CLIENTS_FOLDER_NOT_FOUND" }

    # Create an unrestricted test-only policy. Built-in Restricted/Unrestricted
    # have a template mismatch in the serialized content (Remoting Policies
    # Folder instead of Remoting Policy) and never register with
    # RemotingPolicyManager. Dangerous has an allowlist that excludes Get-User.
    $policyName = "IntegrationTest-Policy"
    $existingPolicy = Get-Item -Path "$policiesPath/$policyName" -ErrorAction SilentlyContinue
    if ($existingPolicy) { $existingPolicy | Remove-Item -Force -Permanently }
    $policy = New-Item -Path $policiesPath -Name $policyName -ItemType $policyTemplateId
    $policy.Editing.BeginEdit()
    $policy["FullLanguage"]    = "1"
    $policy["AllowedCommands"] = ""
    $policy["AuditLevel"]      = "None"
    $policy.Editing.EndEdit() | Out-Null

    # Clean up any leftover client from an interrupted previous run
    $existing = Get-ChildItem -Path $clientsFolderPath -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -eq "IntegrationTest" }
    if ($existing) { $existing | Remove-Item -Force -Permanently }

    $item = New-Item -Path $clientsFolderPath -Name "IntegrationTest" -ItemType $oauthTemplateId
    $item.Editing.BeginEdit()
    $item["AllowedIssuer"]    = $params.issuer
    $item["OAuthClientIds"]  = "spe-remoting"
    $item["Enabled"]           = "1"
    $item["ImpersonatedUser"]  = "sitecore\admin"
    $item["Policy"]            = $policy.ID.ToString()
    $item.Editing.EndEdit() | Out-Null

    return "CREATED=$($item.ID)"
} -Arguments @{ issuer = $allowedIssuer } -Raw

if ($createResult -like "CREATED*") {
    Write-Host "    Created IntegrationTest OAuth Client: $createResult" -ForegroundColor Green
} else {
    Write-Host "    ERROR creating OAuth Client: $createResult" -ForegroundColor Red
}

Stop-ScriptSession -Session $session
