<#
.SYNOPSIS
    Updates the Insert and Edit Content Editor dialogs for the new
    Remoting Client templates:

    - Shared Secret Client Insert: adds "copy now, cannot be retrieved
      later" messaging around the plaintext secret; other fields
      unchanged. Secret stays cleartext at rest (HMAC keying is
      symmetric and cannot use a one-way hash).

    - OAuth Client Insert (new): new script under Insert Item so
      operators can create OAuth Client items from the Remoting
      Clients folder's Insert menu.

    - Edit Remoting Client: removes the Shared Secret section from
      the Shared Secret branch. Rotation is not editable - operators
      create a new Shared Secret Client item and disable the old one.

    Idempotent. Re-runs overwrite the Script bodies and leave item
    names / paths alone if already correct.
#>
$ErrorActionPreference = 'Stop'

function Write-Step { param([string]$m) Write-Host "==> $m" -ForegroundColor Cyan }

# --------------------------------------------------------------------------
# Body for Insert Item/Shared Secret Client (cleartext + copy-now messaging)
# --------------------------------------------------------------------------

$sharedSecretInsertBody = @'
$folderTemplateId = "{9DC763FC-E1EA-4B37-8223-EA5A7712D307}"  # Remoting Clients Folder
$sharedSecretTemplateId = "{55AB1AA8-890E-401E-AF06-094CA21E0E2D}"
$defaultPolicyId = "{59B24247-999C-4298-B297-53C548E6D6E9}"
$policyTemplateId = "{AF864A3C-6D3D-4889-AFEF-9B1D427F4EA8}"

if(-not $SitecoreContextItem -or $SitecoreContextItem.TemplateID -ne $folderTemplateId) {
    Close-Window
    Exit
}

Import-Function -Name DialogBuilder

$rng = [System.Security.Cryptography.RNGCryptoServiceProvider]::new()
$kidBytes = New-Object byte[] 12
$rng.GetBytes($kidBytes)
$generatedAccessKeyId = "spe_" + [System.BitConverter]::ToString($kidBytes).Replace("-","").ToLower()
$secretBytes = New-Object byte[] 32
$rng.GetBytes($secretBytes)
$generatedSecret = [System.BitConverter]::ToString($secretBytes).Replace("-","")
$rng.Dispose()

$dialog = New-DialogBuilder -Title "Create Shared Secret Client" `
    -Description "Creates a new Shared Secret Client for SPE Remoting HMAC authentication. The Access Key Id is included in the JWT header; the Shared Secret is the HMAC signing key." `
    -Width 700 -Height 620 -OkButtonName "Create" -ShowHints `
    -Icon ([regex]::Replace($PSScript.Appearance.Icon, "Office", "OfficeWhite", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase))

$dialog | Add-TextField -Name "name" -Title "Name" -Placeholder "My Shared Secret Client" `
    -Tooltip "A descriptive name for this client." -Mandatory -Tab "Status"

$dialog | Add-InfoText -Name "secretWarning" -Title "Copy the Shared Secret before clicking Create" `
    -Value "This is the only time the Shared Secret is shown in plain text. The Edit dialog hides the field. To rotate the secret, create a new Shared Secret Client and update all callers, then disable the old one." `
    -Tab "Authentication"

$dialog | Add-TextField -Name "accessKeyId" -Title "Access Key Id" -Value $generatedAccessKeyId `
    -Tooltip "Unique identifier sent in the JWT kid header. Auto-generated but can be customized." `
    -Mandatory -Tab "Authentication"

$dialog | Add-TextField -Name "sharedSecret" -Title "Shared Secret" -Value $generatedSecret `
    -Tooltip "HMAC signing key shared between client and server. Auto-generated (64 hex chars, 256 bits). RFC 7518 minimums: HS256 = 32, HS384 = 48, HS512 = 64." `
    -Mandatory -Tab "Authentication"

$dialog | Add-DateTimePicker -Name "expires" -Title "Expires" `
    -Tooltip "Optional expiration date. After this date the client is automatically rejected. Leave empty for no expiration." `
    -Tab "Authentication"

$dialog | Add-UserPicker -Name "impersonateUser" -Title "Impersonated User" `
    -Tooltip "The Sitecore user identity used when executing scripts with this client." `
    -Mandatory -Tab "Runtime"

$policy = Get-Item -Path "master:" -ID $defaultPolicyId
$dialog | Add-Droplink -Name "policy" -Title "Remoting Policy" -Value $policy `
    -Tooltip "Controls what commands and scripts this client can execute." `
    -Source "script:{1993BD1B-0004-41BD-A857-4D293E4EA884}" `
    -Tab "Runtime"

$dialog | Add-TextField -Name "requestLimit" -Title "Request Limit" -IsNumber `
    -Tooltip "Maximum requests allowed within the throttle window. Leave empty to disable rate limiting." `
    -Tab "Throttling"

$dialog | Add-TextField -Name "throttleWindow" -Title "Throttle Window (seconds)" -IsNumber `
    -Tooltip "Duration in seconds for the rate limiting window (e.g. 60 for per-minute limiting)." `
    -Tab "Throttling"

$dialog | Add-Dropdown -Name "throttleAction" -Title "Throttle Action" -Value "" `
    -Options ([ordered]@{ "Block (HTTP 429)" = "Block"; "Bypass (allow but log)" = "Bypass" }) `
    -Placeholder "Select an action" `
    -Tooltip "Block rejects with HTTP 429. Bypass allows the request through and logs that the limit was exceeded." `
    -Tab "Throttling"

$validator = {
    $proposedName = $variables.name.Value
    if($existingNames | Where-Object { $_ -eq $proposedName }) {
        $variables.name.Error = "An item named '$proposedName' already exists."
    }
    if($variables.accessKeyId.Value.Length -lt 8) {
        $variables.accessKeyId.Error = "Access Key Id must be at least 8 characters."
    }
    if($variables.sharedSecret.Value.Length -lt 32) {
        $variables.sharedSecret.Error = "Shared Secret must be at least 32 characters. RFC 7518 minimums: HS256 = 32, HS384 = 48, HS512 = 64."
    }
    if([string]::IsNullOrEmpty($variables.impersonateUser.Value)) {
        $variables.impersonateUser.Error = "Impersonated User is required."
    }
    $pTid = "{AF864A3C-6D3D-4889-AFEF-9B1D427F4EA8}"
    if(!$variables.policy.Value -or $variables.policy.Value.TemplateID.ToString() -ne $pTid) {
        $variables.policy.Error = "Please select a Remoting Policy."
    }
    $limit = $variables.requestLimit.Value
    $window = $variables.throttleWindow.Value
    if($limit -gt 0 -and $window -le 0) {
        $variables.throttleWindow.Error = "Throttle Window is required when Request Limit is set."
    }
}

$result = $dialog | Invoke-Dialog -Validator $validator -ValidatorParameters @{ existingNames = $SitecoreContextItem.Children.Name }
if($result.Result -ne "ok") { Close-Window; Exit }

$item = New-Item -Path $SitecoreContextItem.ProviderPath -Name $name -ItemType $sharedSecretTemplateId
New-UsingBlock (New-Object Sitecore.Data.BulkUpdateContext) {
    $item.Editing.BeginEdit()
    $item["AccessKeyId"] = $accessKeyId
    $item["SharedSecret"] = $sharedSecret
    $item["Enabled"] = "1"
    $item["ImpersonatedUser"] = $impersonateUser | Select-Object -First 1
    if($policy) { $item["Policy"] = $policy.ID }
    $item["RequestLimit"]   = if($requestLimit)   { $requestLimit.ToString() } else { "" }
    $item["ThrottleWindow"] = if($throttleWindow) { $throttleWindow.ToString() } else { "" }
    $item["ThrottleAction"] = $throttleAction
    $item["Expires"] = if($expires -and $expires -ne [DateTime]::MinValue) { [Sitecore.DateUtil]::ToIsoDate($expires) } else { "" }
    $item.Editing.EndEdit() > $null
}

Show-Alert -Title "Shared Secret Client created" `
    -Text "Access Key Id: $accessKeyId`n`nShared Secret: $sharedSecret`n`nCopy both before closing. The Shared Secret is not shown again."

Close-Window
'@

# --------------------------------------------------------------------------
# Body for Insert Item/OAuth Client (new)
# --------------------------------------------------------------------------

$oauthInsertBody = @'
$folderTemplateId = "{9DC763FC-E1EA-4B37-8223-EA5A7712D307}"  # Remoting Clients Folder
$oauthTemplateId = "{E1F946A8-86E0-4CDF-BFA7-3089E669D153}"
$defaultPolicyId = "{59B24247-999C-4298-B297-53C548E6D6E9}"
$policyTemplateId = "{AF864A3C-6D3D-4889-AFEF-9B1D427F4EA8}"

if(-not $SitecoreContextItem -or $SitecoreContextItem.TemplateID -ne $folderTemplateId) {
    Close-Window
    Exit
}

Import-Function -Name DialogBuilder

$dialog = New-DialogBuilder -Title "Create OAuth Client" `
    -Description "Creates a new OAuth Client identity that validates externally-issued bearer tokens (RS256 / ES256 JWTs) against the configured issuer and JWKS." `
    -Width 700 -Height 600 -OkButtonName "Create" -ShowHints `
    -Icon ([regex]::Replace($PSScript.Appearance.Icon, "Office", "OfficeWhite", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase))

$dialog | Add-TextField -Name "name" -Title "Name" -Placeholder "CI Pipeline" `
    -Tooltip "A descriptive name for this client." -Mandatory -Tab "Status"

$dialog | Add-TextField -Name "allowedIssuer" -Title "Allowed Issuer" `
    -Tooltip "The exact iss claim expected on incoming tokens (e.g. https://your-idp). Required - tokens whose iss claim does not match exactly are rejected. Prevents client_id collisions across IdPs." `
    -Mandatory -Tab "Authentication"

$dialog | Add-MultilineText -Name "oauthClientIds" -Title "OAuth Client Ids" `
    -Tooltip "One client_id per line. A token whose client_id claim matches any entry authenticates as this client. Multiple entries let one client cover multiple IdP registrations (e.g. dev tenant + prod tenant)." `
    -Tab "Authentication" -Lines 6

$dialog | Add-DateTimePicker -Name "expires" -Title "Expires" `
    -Tooltip "Optional expiration date. After this date the client is automatically rejected regardless of token exp." `
    -Tab "Authentication"

$dialog | Add-UserPicker -Name "impersonateUser" -Title "Impersonated User" `
    -Tooltip "The Sitecore user identity used when executing scripts with this client. For client_credentials flows, set this to a service account rather than relying on token claims." `
    -Mandatory -Tab "Runtime"

$policy = Get-Item -Path "master:" -ID $defaultPolicyId
$dialog | Add-Droplink -Name "policy" -Title "Remoting Policy" -Value $policy `
    -Tooltip "Controls what commands and scripts this client can execute." `
    -Source "script:{1993BD1B-0004-41BD-A857-4D293E4EA884}" `
    -Tab "Runtime"

$dialog | Add-TextField -Name "requestLimit" -Title "Request Limit" -IsNumber `
    -Tooltip "Maximum requests allowed within the throttle window. Leave empty to disable rate limiting." `
    -Tab "Throttling"

$dialog | Add-TextField -Name "throttleWindow" -Title "Throttle Window (seconds)" -IsNumber `
    -Tooltip "Duration in seconds for the rate limiting window." `
    -Tab "Throttling"

$dialog | Add-Dropdown -Name "throttleAction" -Title "Throttle Action" -Value "" `
    -Options ([ordered]@{ "Block (HTTP 429)" = "Block"; "Bypass (allow but log)" = "Bypass" }) `
    -Placeholder "Select an action" `
    -Tooltip "Block rejects with HTTP 429. Bypass allows the request through and logs that the limit was exceeded." `
    -Tab "Throttling"

$validator = {
    $proposedName = $variables.name.Value
    if($existingNames | Where-Object { $_ -eq $proposedName }) {
        $variables.name.Error = "An item named '$proposedName' already exists."
    }
    if([string]::IsNullOrWhiteSpace($variables.allowedIssuer.Value)) {
        $variables.allowedIssuer.Error = "Allowed Issuer is required."
    }
    if([string]::IsNullOrEmpty($variables.impersonateUser.Value)) {
        $variables.impersonateUser.Error = "Impersonated User is required."
    }
    $pTid = "{AF864A3C-6D3D-4889-AFEF-9B1D427F4EA8}"
    if(!$variables.policy.Value -or $variables.policy.Value.TemplateID.ToString() -ne $pTid) {
        $variables.policy.Error = "Please select a Remoting Policy."
    }
    $limit = $variables.requestLimit.Value
    $window = $variables.throttleWindow.Value
    if($limit -gt 0 -and $window -le 0) {
        $variables.throttleWindow.Error = "Throttle Window is required when Request Limit is set."
    }
}

$result = $dialog | Invoke-Dialog -Validator $validator -ValidatorParameters @{ existingNames = $SitecoreContextItem.Children.Name }
if($result.Result -ne "ok") { Close-Window; Exit }

$item = New-Item -Path $SitecoreContextItem.ProviderPath -Name $name -ItemType $oauthTemplateId
New-UsingBlock (New-Object Sitecore.Data.BulkUpdateContext) {
    $item.Editing.BeginEdit()
    $item["AllowedIssuer"] = $allowedIssuer
    $item["OAuthClientIds"] = $oauthClientIds
    $item["Enabled"] = "1"
    $item["ImpersonatedUser"] = $impersonateUser | Select-Object -First 1
    if($policy) { $item["Policy"] = $policy.ID }
    $item["RequestLimit"]   = if($requestLimit)   { $requestLimit.ToString() } else { "" }
    $item["ThrottleWindow"] = if($throttleWindow) { $throttleWindow.ToString() } else { "" }
    $item["ThrottleAction"] = $throttleAction
    $item["Expires"] = if($expires -and $expires -ne [DateTime]::MinValue) { [Sitecore.DateUtil]::ToIsoDate($expires) } else { "" }
    $item.Editing.EndEdit() > $null
}

Close-Window
'@

# --------------------------------------------------------------------------
# Body for Edit Remoting Client (removes Shared Secret UI from SS branch)
# --------------------------------------------------------------------------

$editBody = @'
$sharedSecretTemplateId = "{55AB1AA8-890E-401E-AF06-094CA21E0E2D}"
$oauthTemplateId        = "{E1F946A8-86E0-4CDF-BFA7-3089E669D153}"
$policyTemplateId       = "{AF864A3C-6D3D-4889-AFEF-9B1D427F4EA8}"
$defaultPolicyId        = "{59B24247-999C-4298-B297-53C548E6D6E9}"

if(-not $SitecoreContextItem) { Close-Window; Exit }

$tid  = $SitecoreContextItem.TemplateID.ToString().ToLower()
$ssid = $sharedSecretTemplateId.ToLower()
$oid  = $oauthTemplateId.ToLower()

if($tid -ne $ssid -and $tid -ne $oid) { Close-Window; Exit }

Import-Function -Name DialogBuilder

$item = $SitecoreContextItem | Initialize-Item
$isOAuth = $tid -eq $oid
$clientKind = if($isOAuth) { "OAuth Client" } else { "Shared Secret Client" }

$currentEnabled = [Sitecore.MainUtil]::GetBool($item["Enabled"], $false)
$currentUser = $item["ImpersonatedUser"]
$currentRequestLimit = $item["RequestLimit"]
$currentThrottleWindow = $item["ThrottleWindow"]
$currentThrottleAction = $item["ThrottleAction"]
if([string]::IsNullOrEmpty($currentThrottleAction)) { $currentThrottleAction = "Block" }

$currentExpires = [DateTime]::MinValue
$currentExpiresStr = $item["Expires"]
if(-not [string]::IsNullOrEmpty($currentExpiresStr)) {
    $parsed = [Sitecore.DateUtil]::IsoDateToDateTime($currentExpiresStr, [DateTime]::MinValue)
    if($parsed -ne [DateTime]::MinValue) { $currentExpires = $parsed }
}

$currentPolicyId = $item["Policy"]
$currentPolicy = $null
if(-not [string]::IsNullOrEmpty($currentPolicyId)) {
    $currentPolicy = Get-Item -Path "master:" -ID $currentPolicyId -ErrorAction SilentlyContinue
}
if(-not $currentPolicy) { $currentPolicy = Get-Item -Path "master:" -ID $defaultPolicyId }

if(-not $isOAuth) {
    $currentAccessKeyId = $item["AccessKeyId"]
    $rng = [System.Security.Cryptography.RNGCryptoServiceProvider]::new()
    $kidBytes = New-Object byte[] 12
    $rng.GetBytes($kidBytes)
    $generatedAccessKeyId = "spe_" + [System.BitConverter]::ToString($kidBytes).Replace("-","").ToLower()
    $rng.Dispose()
}

if($isOAuth) {
    $currentAllowedIssuer = $item["AllowedIssuer"]
    $currentOAuthClientIds = $item["OAuthClientIds"]
}

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
    $dialog | Add-InfoText -Name "rotationNote" -Title "Rotating credentials" `
        -Value "To rotate the Shared Secret, create a new Shared Secret Client and update all callers, then disable this one. The Shared Secret is intentionally hidden from Edit." `
        -Tab "Authentication"

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
} else {
    $dialog | Add-TextField -Name "allowedIssuer" -Title "Allowed Issuer" -Value $currentAllowedIssuer `
        -Tooltip "The exact iss claim expected on incoming tokens. Required so client_id collisions across IdPs cannot cross-match." `
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
    -Tooltip "Duration in seconds for the rate limiting window." `
    -Tab "Throttling"

$dialog | Add-Dropdown -Name "throttleAction" -Title "Throttle Action" -Value $currentThrottleAction `
    -Options ([ordered]@{ "Block (HTTP 429)" = "Block"; "Bypass (allow but log)" = "Bypass" }) `
    -Placeholder "Select an action" `
    -Tooltip "Block rejects with HTTP 429. Bypass allows the request through and logs that the limit was exceeded." `
    -Tab "Throttling"

$validator = {
    if([string]::IsNullOrEmpty($variables.impersonateUser.Value)) {
        $variables.impersonateUser.Error = "Impersonated User is required."
    }
    $pTid = "{AF864A3C-6D3D-4889-AFEF-9B1D427F4EA8}"
    if(!$variables.policy.Value -or $variables.policy.Value.TemplateID.ToString() -ne $pTid) {
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
if($result.Result -ne "ok") { Close-Window; Exit }

$finalKeyId = $null
if(-not $isOAuth) {
    $finalKeyId = switch($keyIdAction) {
        "Rotate" { $generatedAccessKeyId }
        "Custom" { $customKeyId }
        default  { $currentAccessKeyId }
    }
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

if(-not $isOAuth -and $keyIdAction -ne "Keep") {
    Show-Alert -Title "Access Key Id rotated" -Text "New Access Key Id: $finalKeyId`n`nUpdate all callers."
}

Close-Window
'@

# --------------------------------------------------------------------------
# Install
# --------------------------------------------------------------------------

Write-Step "Rewrite Insert Item/Shared Secret Client body"
$insertSsPath = 'master:/sitecore/system/Modules/PowerShell/Script Library/SPE/Core/Remoting/Content Editor/Insert Item/Shared Secret Client'
if (Test-Path $insertSsPath) {
    $i = Get-Item $insertSsPath
    $i.Editing.BeginEdit()
    $i['Script'] = $sharedSecretInsertBody
    $i.Editing.EndEdit() > $null
    Write-Host "   updated ($($sharedSecretInsertBody.Length) chars)"
}

Write-Step "Create Insert Item/OAuth Client script"
$insertOAuthPath = 'master:/sitecore/system/Modules/PowerShell/Script Library/SPE/Core/Remoting/Content Editor/Insert Item/OAuth Client'
$insertRoot      = 'master:/sitecore/system/Modules/PowerShell/Script Library/SPE/Core/Remoting/Content Editor/Insert Item'
# Reuse the same template the Shared Secret Client insert script uses.
$sibling = Get-Item "$insertRoot/Shared Secret Client"
if (-not (Test-Path $insertOAuthPath)) {
    $i = New-Item -Path $insertRoot -Name 'OAuth Client' -ItemType $sibling.TemplateID.ToString()
} else {
    $i = Get-Item $insertOAuthPath
}
$i.Editing.BeginEdit()
$i['Script']       = $oauthInsertBody
$i['__Icon']       = 'office/32x32/certificate.png'
$i.Editing.EndEdit() > $null
Write-Host "   installed ($($oauthInsertBody.Length) chars)"

Write-Step "Rewrite Edit Remoting Client body (drop Shared Secret UI)"
$editPath = 'master:/sitecore/system/Modules/PowerShell/Script Library/SPE/Core/Remoting/Content Editor/Context Menu/Edit Remoting Client'
if (Test-Path $editPath) {
    $i = Get-Item $editPath
    $i.Editing.BeginEdit()
    $i['Script'] = $editBody
    $i.Editing.EndEdit() > $null
    Write-Host "   updated ($($editBody.Length) chars)"
}

Write-Host ""
Write-Host "Dialogs migration complete." -ForegroundColor Green
