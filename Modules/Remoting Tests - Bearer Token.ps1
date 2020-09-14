Clear-Host

Import-Module -Name SPE -Force

$body = @"
    $env:COMPUTERNAME
    <#b1b0b6b9-3da6-4761-9b11-2685477561b3#>
"@
<#
$name = 'sitecore\admin'
$sharedSecret = '7AF6F59C14A05786E97012F054D1FB98AC756A2E54E5C9ACBAEE147D9ED0E0DB'
$issuer = 'PowerShell Script'

$token = New-JWT -Algorithm 'HS256' -Issuer $issuer -Name $name -SecretKey $sharedSecret -ValidforSeconds 30
$url = "https://spe.dev.local/-/script/script/?sessionId=b1b0b6b9-3da6-4761-9b11-2685477561b3&rawOutput=False&persistentSession=False"

$contentType = 'application/json'
$headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]"
$headers.Add('Content-Type' , $contentType)
$headers.Add('Authorization','Bearer ' + $token)  
Invoke-WebRequest -UseBasicParsing -Uri $url -Method Post -Headers $headers -ContentType $contentType -Body $body
#>
$sharedSecret = '7AF6F59C14A05786E97012F054D1FB98AC756A2E54E5C9ACBAEE147D9ED0E0DB'
$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri "https://spe.dev.local"

Invoke-RemoteScript -ScriptBlock {
    $env:COMPUTERNAME
} -Session $session

Stop-ScriptSession -Session $session