<#
.SYNOPSIS
    Unifies the Content Editor context-menu "Edit ..." script so it handles
    both Shared Secret Client and OAuth Client items.

.DESCRIPTION
    Renames the script item from "Edit Shared Secret Client" to
    "Edit Remoting Client" so the menu caption is template-neutral,
    then replaces its Script body with a dispatcher that launches a
    template-appropriate dialog (HMAC fields for Shared Secret Client,
    issuer + client_ids for OAuth Client). Common fields
    (Impersonated User, Policy, Enabled, Expires, throttling) are shared.

    Idempotent. Re-runs are safe: renames only when the old path exists
    and the new one does not, and re-installs the Script body verbatim
    so any manual Content Editor tweaks are overwritten.
#>
$ErrorActionPreference = 'Stop'

function Write-Step {
    param([string]$Message)
    Write-Host "==> $Message" -ForegroundColor Cyan
}

$scriptBody = @'
$sharedSecretTemplateId = "{55AB1AA8-890E-401E-AF06-094CA21E0E2D}"
$oauthTemplateId        = "{E1F946A8-86E0-4CDF-BFA7-3089E669D153}"
$policyTemplateId       = "{AF864A3C-6D3D-4889-AFEF-9B1D427F4EA8}"
$defaultPolicyId        = "{59B24247-999C-4298-B297-53C548E6D6E9}"

if(-not $SitecoreContextItem) {
    Close-Window
    Exit
}

$tid = $SitecoreContextItem.TemplateID.ToString().ToLower()
$ssid = $sharedSecretTemplateId.ToLower()
$oid = $oauthTemplateId.ToLower()

if($tid -ne $ssid -and $tid -ne $oid) {
    Close-Window
    Exit
}

Import-Function -Name DialogBuilder

$item = $SitecoreContextItem | Initialize-Item
$isOAuth = $tid -eq $oid
$clientKind = if($isOAuth) { "OAuth Client" } else { "Shared Secret Client" }

# --- Common field values ---

$currentEnabled = [Sitecore.MainUtil]::GetBool($item["Enabled"], $false)
$currentUser = $item["ImpersonatedUser"]
$currentRequestLimit = $item["RequestLimit"]
$currentThrottleWindow = $item["ThrottleWindow"]
$currentThrottleAction = $item["ThrottleAction"]
if([string]::IsNullOrEmpty($currentThrottleAction)) { $currentThrottleAction = "Block" }

$currentExpires = [DateTime]::MinValue
$currentExpiresStr = $item["Expires"]
if(-not [string]::IsNullOrEmpty($currentExpiresStr)) {
    $parsedExpires = [Sitecore.DateUtil]::IsoDateToDateTime($currentExpiresStr, [DateTime]::MinValue)
    if($parsedExpires -ne [DateTime]::MinValue) { $currentExpires = $parsedExpires }
}

$currentPolicyId = $item["Policy"]
$currentPolicy = $null
if(-not [string]::IsNullOrEmpty($currentPolicyId)) {
    $currentPolicy = Get-Item -Path "master:" -ID $currentPolicyId -ErrorAction SilentlyContinue
}
if(-not $currentPolicy) {
    $currentPolicy = Get-Item -Path "master:" -ID $defaultPolicyId
}

# --- HMAC-specific values ---

if(-not $isOAuth) {
    $currentAccessKeyId = $item["AccessKeyId"]
    $currentSecret = $item["SharedSecret"]

    $rng = [System.Security.Cryptography.RNGCryptoServiceProvider]::new()
    $kidBytes = New-Object byte[] 12
    $rng.GetBytes($kidBytes)
    $generatedAccessKeyId = "spe_" + [System.BitConverter]::ToString($kidBytes).Replace("-","").ToLower()
    $secretBytes = New-Object byte[] 32
    $rng.GetBytes($secretBytes)
    $generatedSecret = [System.BitConverter]::ToString($secretBytes).Replace("-","")
    $rng.Dispose()

    $secretDisplay = if($currentSecret.Length -gt 8) {
        $currentSecret.Substring(0,4) + ("*" * ($currentSecret.Length - 8)) + $currentSecret.Substring($currentSecret.Length - 4)
    } else { "********" }
}

# --- OAuth-specific values ---

if($isOAuth) {
    $currentAllowedIssuer = $item["AllowedIssuer"]
    $currentOAuthClientIds = $item["OAuthClientIds"]
}

# --- Build dialog ---

$dialog = New-DialogBuilder -Title "Edit $clientKind" `
    -Description "Modify the $clientKind '$($item.Name)'." `
    -Width 700 -Height 550 -OkButtonName "Save" -ShowHints `
    -Icon ([regex]::Replace($item.Appearance.Icon, "Office", "OfficeWhite", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase))

$dialog | Add-TextField -Name "name" -Title "Name" -Value $item.Name `
    -Tooltip "A descriptive name for this $clientKind." -Mandatory `
    -Tab "Status"

$dialog | Add-Checkbox -Name "enabled" -Title "Enabled" -Value $currentEnabled `
    -Tooltip "Disabled clients cannot authenticate. Use to revoke access without deleting the item." `
    -Tab "Status"

if(-not $isOAuth) {
    $dialog | Add-RadioButtons -Name "keyIdAction" -Title "Access Key Id" -Value "Keep" `
        -Options ([ordered]@{ "Keep: $currentAccessKeyId" = "Keep"; "Rotate (generate new)" = "Rotate"; "Custom" = "Custom" }) `
        -Tooltip "The Access Key Id is sent in the JWT kid header. Changing it requires updating all callers." `
        -Tab "Authentication" -GroupId 1

    $dialog | Add-InfoText -Name "newKeyIdInfo" -Title "New Access Key Id" `
        -Value "$generatedAccessKeyId" `
        -Tab "Authentication" -ParentGroupId 1 -ShowOnValue "Rotate"

    $dialog | Add-TextField -Name "customKeyId" -Title "Custom Access Key Id" -Value $currentAccessKeyId `
        -Tooltip "Enter a custom Access Key Id. Must be at least 8 characters." `
        -Tab "Authentication" -ParentGroupId 1 -ShowOnValue "Custom"

    $dialog | Add-RadioButtons -Name "secretAction" -Title "Shared Secret" -Value "Keep" `
        -Options ([ordered]@{ "Keep current ($($currentSecret.Length) chars): $secretDisplay" = "Keep"; "Rotate (generate new)" = "Rotate" }) `
        -Tooltip "The shared secret is the HMAC signing key. Rotating requires updating all callers." `
        -Tab "Authentication" -GroupId 2

    $dialog | Add-InfoText -Name "newSecretInfo" -Title "New Shared Secret" `
        -Value "$generatedSecret" `
        -Tab "Authentication" -ParentGroupId 2 -ShowOnValue "Rotate"
} else {
    $dialog | Add-TextField -Name "allowedIssuer" -Title "Allowed Issuer" -Value $currentAllowedIssuer `
        -Tooltip "The exact iss claim expected on incoming tokens, e.g. https://your-idp. Required so client_id collisions across IdPs cannot cross-match." `
        -Tab "Authentication" -Mandatory

    $dialog | Add-MultilineText -Name "oauthClientIds" -Title "OAuth Client Ids" -Value $currentOAuthClientIds `
        -Tooltip "One client_id per line. Tokens whose client_id claim matches any line authenticate as this client." `
        -Tab "Authentication" -Lines 6
}

$dialog | Add-DateTimePicker -Name "expires" -Title "Expires" -Value $currentExpires `
    -Tooltip "Optional expiration date. After this date the client is automatically rejected. Leave empty for no expiration." `
    -Tab "Authentication"

$dialog | Add-UserPicker -Name "impersonateUser" -Title "Impersonated User" -Value $currentUser `
    -Tooltip "The Sitecore user identity used when executing scripts with this client." -Mandatory `
    -Tab "Runtime"

$dialog | Add-Droplink -Name "policy" -Title "Remoting Policy" -Value $currentPolicy `
    -Tooltip "Controls what commands and scripts this client can execute." `
    -Source "script:{1993BD1B-0004-41BD-A857-4D293E4EA884}" `
    -Tab "Runtime"

$currentLimitInt = 0
$currentWindowInt = 0
if($currentRequestLimit)  { [int]::TryParse($currentRequestLimit, [ref]$currentLimitInt) > $null }
if($currentThrottleWindow){ [int]::TryParse($currentThrottleWindow, [ref]$currentWindowInt) > $null }

$dialog | Add-TextField -Name "requestLimit" -Title "Request Limit" `
    -Value $(if($currentLimitInt -gt 0) { $currentRequestLimit } else { "" }) -IsNumber `
    -Tooltip "Maximum requests allowed within the throttle window. Leave empty to disable rate limiting." `
    -Tab "Throttling"

$dialog | Add-TextField -Name "throttleWindow" -Title "Throttle Window (seconds)" `
    -Value $(if($currentWindowInt -gt 0) { $currentThrottleWindow } else { "" }) -IsNumber `
    -Tooltip "Duration in seconds for the rate limiting window (e.g. 60 for per-minute limiting)." `
    -Tab "Throttling"

$dialog | Add-Dropdown -Name "throttleAction" -Title "Throttle Action" -Value $currentThrottleAction `
    -Options ([ordered]@{ "Block (HTTP 429)" = "Block"; "Bypass (allow but log)" = "Bypass" }) `
    -Placeholder "Select an action" `
    -Tooltip "Block rejects with HTTP 429. Bypass allows the request through and logs that the limit was exceeded." `
    -Tab "Throttling"

$dialog | Add-InfoText -Name "rateLimitInfo" -Title "Response Headers" `
    -Value "When enabled, every response includes X-RateLimit-Limit, X-RateLimit-Remaining, and X-RateLimit-Reset headers." `
    -Tab "Throttling"

# --- Validator ---

$validator = {
    if([string]::IsNullOrEmpty($variables.impersonateUser.Value)) {
        $variables.impersonateUser.Error = "Impersonated User is required."
    }
    $policyTemplateId = "{AF864A3C-6D3D-4889-AFEF-9B1D427F4EA8}"
    if(!$variables.policy.Value -or $variables.policy.Value.TemplateID.ToString() -ne $policyTemplateId) {
        $variables.policy.Error = "Please select a Remoting Policy."
    }
    $limit = $variables.requestLimit.Value
    $window = $variables.throttleWindow.Value
    if($limit -gt 0 -and $window -le 0) {
        $variables.throttleWindow.Error = "Throttle Window is required when Request Limit is set."
    }
    if($variables.keyIdAction) {
        if($variables.keyIdAction.Value -eq "Custom" -and $variables.customKeyId.Value.Length -lt 8) {
            $variables.customKeyId.Error = "Access Key Id must be at least 8 characters."
        }
    }
    if($variables.allowedIssuer -and [string]::IsNullOrWhiteSpace($variables.allowedIssuer.Value)) {
        $variables.allowedIssuer.Error = "Allowed Issuer is required."
    }
}

$result = $dialog | Invoke-Dialog -Validator $validator

if($result.Result -ne "ok") {
    Close-Window
    Exit
}

# --- Save ---

$finalKeyId = $null
$finalSecret = $null
if(-not $isOAuth) {
    $finalKeyId = switch($keyIdAction) {
        "Rotate" { $generatedAccessKeyId }
        "Custom" { $customKeyId }
        default  { $currentAccessKeyId }
    }
    $finalSecret = if($secretAction -eq "Rotate") { $generatedSecret } else { $currentSecret }
}

New-UsingBlock (New-Object Sitecore.Data.BulkUpdateContext) {
    $item.Editing.BeginEdit()
    $item["Enabled"] = if($enabled) { "1" } else { "" }
    $item["ImpersonatedUser"] = $impersonateUser | Select-Object -First 1
    if($policy) { $item["Policy"] = $policy.ID }
    $item["RequestLimit"]   = if($requestLimit)   { $requestLimit.ToString() } else { "" }
    $item["ThrottleWindow"] = if($throttleWindow) { $throttleWindow.ToString() } else { "" }
    $item["ThrottleAction"] = $throttleAction
    $item["Expires"] = if($expires -and $expires -ne [DateTime]::MinValue) { [Sitecore.DateUtil]::ToIsoDate($expires) } else { "" }
    if(-not $isOAuth) {
        $item["AccessKeyId"] = $finalKeyId
        $item["SharedSecret"] = $finalSecret
    } else {
        $item["AllowedIssuer"] = $allowedIssuer
        $item["OAuthClientIds"] = $oauthClientIds
    }
    $item.Editing.EndEdit() > $null
}

if($name -ne $item.Name) {
    $item.Editing.BeginEdit()
    $item.Name = $name
    $item.Editing.EndEdit() > $null
}

# --- Confirmation for rotated HMAC credentials ---

if(-not $isOAuth) {
    $messages = @()
    if($keyIdAction -ne "Keep") { $messages += "Access Key Id: $finalKeyId" }
    if($secretAction -eq "Rotate") { $messages += "Shared Secret: $finalSecret" }
    if($messages.Count -gt 0) {
        Show-Alert -Title "Credentials rotated" -Text ("Update all callers with:`n`n" + ($messages -join "`n"))
    }
}

Close-Window
'@

Write-Step "Rename 'Edit Shared Secret Client' -> 'Edit Remoting Client'"
$ssName = 'Edit Shared Secret Client'
$unifiedName = 'Edit Remoting Client'
$contextMenuRoot = 'master:/sitecore/system/Modules/PowerShell/Script Library/SPE/Core/Remoting/Content Editor/Context Menu'
$oldPath = "$contextMenuRoot/$ssName"
$newPath = "$contextMenuRoot/$unifiedName"

if ((Test-Path $oldPath) -and (-not (Test-Path $newPath))) {
    Rename-Item -Path $oldPath -NewName $unifiedName
}

Write-Step "Install unified Script body"
if (Test-Path $newPath) {
    $item = Get-Item $newPath
    $item.Editing.BeginEdit()
    $item['Script'] = $scriptBody
    $item.Editing.EndEdit() > $null
    Write-Host "   body updated ($($scriptBody.Length) chars)"
}

Write-Host ""
Write-Host "Edit-dialog migration complete." -ForegroundColor Green
