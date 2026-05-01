# https://www.reddit.com/r/PowerShell/comments/8bc3rb/generate_jwt_json_web_token_in_powershell/

function New-Jwt {
    param (
        [Parameter(Mandatory = $true)]
        [ValidateSet("HS256", "HS384", "HS512")]
        [string]$Algorithm,
        [Parameter()]
        [string]$Type = "JWT",
        [Parameter(Mandatory = $true)]
        [string]$Issuer,
        [Parameter(Mandatory = $true)]
        [string]$Audience,
        [Parameter()]
        [string]$Name,
        [Parameter()]
        [string]$KeyId,
        [Parameter()]
        [int]$ValidForSeconds = 30,
        [Parameter(Mandatory = $true)]
        [string]$SecretKey,
        [Parameter()]
        [string]$ClientSessionId,
        [Parameter()]
        [switch]$NoIssuedAt,
        [Parameter()]
        [long]$NotBefore = 0,
        [Parameter()]
        [long]$IssuedAt = 0
    )

    $exp = [datetimeoffset]::UtcNow.AddSeconds($ValidForSeconds).ToUnixTimeSeconds()
    $header = [ordered]@{alg = $Algorithm; typ = $type}
    if ($KeyId) { $header['kid'] = $KeyId }
    $payload = [ordered]@{iss = $Issuer; exp = $exp; aud = $Audience}
    if ($Name) { $payload['name'] = $Name }
    if ($ClientSessionId) { $payload['client_session'] = $ClientSessionId }
    # iat is emitted by default so MaxTokenLifetimeSeconds (when configured server-side)
    # has a value to compare against. Pass -NoIssuedAt to opt out for third-party compat scenarios.
    if ($IssuedAt -ne 0) {
        $payload['iat'] = $IssuedAt
    } elseif (-not $NoIssuedAt) {
        $payload['iat'] = [datetimeoffset]::UtcNow.ToUnixTimeSeconds()
    }
    if ($NotBefore -ne 0) { $payload['nbf'] = $NotBefore }

    $headerjson = $header | ConvertTo-Json -Compress
    $payloadjson = $payload | ConvertTo-Json -Compress

    $headerjsonbase64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($headerjson)).Split('=')[0].Replace('+', '-').Replace('/', '_')
    $payloadjsonbase64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($payloadjson)).Split('=')[0].Replace('+', '-').Replace('/', '_')

    $ToBeSigned = $headerjsonbase64 + "." + $payloadjsonbase64

    $SigningAlgorithm = switch ($Algorithm) {
        "HS256" {New-Object System.Security.Cryptography.HMACSHA256}
        "HS384" {New-Object System.Security.Cryptography.HMACSHA384}
        "HS512" {New-Object System.Security.Cryptography.HMACSHA512}
    }

    $SigningAlgorithm.Key = [System.Text.Encoding]::UTF8.GetBytes($SecretKey)
    $Signature = [Convert]::ToBase64String($SigningAlgorithm.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($ToBeSigned))).Split('=')[0].Replace('+', '-').Replace('/', '_')

    $token = "$headerjsonbase64.$payloadjsonbase64.$Signature"
    $token
}