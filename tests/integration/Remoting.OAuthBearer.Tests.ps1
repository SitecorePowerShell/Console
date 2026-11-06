# OAuth bearer runtime tests. SharedSecret and OAuth providers both
# run concurrently under three-mode coexistence - Phase 7 does not swap
# providers, and Setup/Teardown use SharedSecret remoting alongside the
# bearer tests in this file. The fixture item (IntegrationTest OAuth
# Client) is in place from Setup.
#
# Case 8 (Enabled=false flip) runs LAST - after it, the fixture item is
# disabled and further OAuth calls for this client would all fail.
# Teardown removes the fixture regardless of Enabled state.

$getTokenScript = "$PSScriptRoot\..\..\scripts\Get-SpeOAuthToken.ps1"
if (-not (Test-Path $getTokenScript)) {
    Skip-Test -Message "Get-SpeOAuthToken.ps1 not found at $getTokenScript" -Reason "setup"
    return
}

Write-Host "`n  [OAuth Bearer: token acquisition]" -ForegroundColor White

$token = $null
try {
    $token = & $getTokenScript -ErrorAction Stop
} catch {
    $tokenError = $_.Exception.Message
}

if (-not $token) {
    Skip-Test -Message "Could not acquire OAuth token from IDS" -Reason "idp-unavailable: $tokenError"
    return
}

Assert-NotNull $token "IDS issued an access_token"
Assert-True ($token.Split('.').Count -eq 3) "Token is a 3-part JWT"

# Decode the payload for downstream assertions and mutation.
$payloadPart = $token.Split('.')[1]
switch ($payloadPart.Length % 4) { 2 { $payloadPart += "==" } 3 { $payloadPart += "=" } }
$payloadJson = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($payloadPart.Replace('-','+').Replace('_','/')))
$payload = ConvertFrom-Json $payloadJson

Assert-Equal $payload.iss ("https://" + (Get-EnvValue "ID_HOST")) "Issuer matches IDS authority"
Assert-True (@($payload.aud) -contains "spe-remoting") "Audience includes spe-remoting"
Assert-Equal $payload.client_id "spe-remoting" "client_id is spe-remoting"

# Case 6: Valid token + matching OAuth Client item -> 200
Write-Host "`n  [OAuth Bearer: valid token + matching OAuth Client item]" -ForegroundColor White
$happySession = $null
try {
    $happySession = New-ScriptSession -ConnectionUri $global:protocolHost -AccessToken $token
    $result = Invoke-RemoteScript -Session $happySession -ScriptBlock { (Get-User -Current).Name } -Raw
    Assert-Equal $result "sitecore\admin" "Bearer auth resolves to IntegrationTest's Impersonated User"
} catch {
    Write-TestResult -Pass $false -Message "Happy-path bearer round-trip failed: $($_.Exception.Message)"
} finally {
    if ($happySession) { Stop-ScriptSession -Session $happySession -ErrorAction SilentlyContinue }
}

# Case 5: tampered-payload token rejected (signature becomes invalid)
Write-Host "`n  [OAuth Bearer: tampered-signature token rejected]" -ForegroundColor White
$header = $token.Split('.')[0]
$sig = $token.Split('.')[2]
$payload.exp = [DateTimeOffset]::UtcNow.AddMinutes(-10).ToUnixTimeSeconds()
$mutatedPayloadJson = ConvertTo-Json -InputObject $payload -Compress
$mutatedPayloadBase64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($mutatedPayloadJson))
$mutatedPayloadUrl = $mutatedPayloadBase64.TrimEnd('=').Replace('+','-').Replace('/','_')
$mutatedToken = "$header.$mutatedPayloadUrl.$sig"

$mutSession = $null
$mutRejected = $false
try {
    $mutSession = New-ScriptSession -ConnectionUri $global:protocolHost -AccessToken $mutatedToken
    Invoke-RemoteScript -Session $mutSession -ScriptBlock { 1 } -Raw -ErrorAction Stop | Out-Null
} catch {
    $mutRejected = $true
}
if ($mutSession) { Stop-ScriptSession -Session $mutSession -ErrorAction SilentlyContinue }
Assert-True $mutRejected "Token with mutated payload (signature invalid) is rejected"

# Case 10: malformed token rejected
Write-Host "`n  [OAuth Bearer: malformed token rejected]" -ForegroundColor White
$junkSession = $null
$junkRejected = $false
try {
    $junkSession = New-ScriptSession -ConnectionUri $global:protocolHost -AccessToken "not.a.jwt"
    Invoke-RemoteScript -Session $junkSession -ScriptBlock { 1 } -Raw -ErrorAction Stop | Out-Null
} catch {
    $junkRejected = $true
}
if ($junkSession) { Stop-ScriptSession -Session $junkSession -ErrorAction SilentlyContinue }
Assert-True $junkRejected "Malformed (non-JWT) token is rejected"

# Case 8: Enabled=false flip takes effect immediately (no TTL wait)
# This case RUNS LAST. Flipping Enabled via the bearer session (admin
# impersonation) triggers OnItemSaved -> immediate cache invalidation.
# The second request fails because FindByIssuerAndClientId now returns
# null. Teardown removes the fixture regardless of Enabled state.
Write-Host "`n  [OAuth Bearer: Enabled=false flip takes effect immediately]" -ForegroundColor White
$enabledSession = $null
try {
    $enabledSession = New-ScriptSession -ConnectionUri $global:protocolHost -AccessToken $token

    $pre = Invoke-RemoteScript -Session $enabledSession -ScriptBlock { "alive" } -Raw
    Assert-Equal $pre "alive" "Session is healthy before the Enabled flip"

    Invoke-RemoteScript -Session $enabledSession -ScriptBlock {
        $path = "master:/sitecore/system/Modules/PowerShell/Settings/Access/Remoting Clients/IntegrationTest"
        $item = Get-Item -Path $path
        $item.Editing.BeginEdit()
        $item["Enabled"] = ""
        $item.Editing.EndEdit() | Out-Null
    } -Raw | Out-Null

    $blockedAfterDisable = $false
    try {
        Invoke-RemoteScript -Session $enabledSession -ScriptBlock { 1 } -Raw -ErrorAction Stop | Out-Null
    } catch {
        $blockedAfterDisable = $true
    }
    Assert-True $blockedAfterDisable "Post-flip request is rejected (cache was invalidated by OnItemSaved)"
} catch {
    Write-TestResult -Pass $false -Message "Enabled-flip test failed: $($_.Exception.Message)"
} finally {
    if ($enabledSession) { Stop-ScriptSession -Session $enabledSession -ErrorAction SilentlyContinue }
}
