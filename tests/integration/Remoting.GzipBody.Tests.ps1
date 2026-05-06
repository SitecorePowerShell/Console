# Remoting Tests - Gzip body hardening
# Verifies that the /-/script/script handler:
#   1. caps decompressed body size (zip-bomb DoS guard)
#   2. returns 400 (not 500) for a malformed gzip body
#   3. accepts Content-Encoding values regardless of case
#
# Run via: .\Run-RemotingTests.ps1 -TestFile Remoting.GzipBody.Tests.ps1
# Requires: SPE Remoting enabled, shared secret configured.

$serviceUrl = "$protocolHost/-/script/script/"

$handler = New-Object System.Net.Http.HttpClientHandler
if ($PSVersionTable.PSVersion.Major -ge 6) {
    $handler.ServerCertificateCustomValidationCallback = [System.Net.Http.HttpClientHandler]::DangerousAcceptAnyServerCertificateValidator
} else {
    if (-not ([System.Management.Automation.PSTypeName]'TrustAllCertsPolicy').Type) {
        Add-Type @"
public class TrustAllCertsPolicy : System.Net.ICertificatePolicy {
    public bool CheckValidationResult(System.Net.ServicePoint sp,
        System.Security.Cryptography.X509Certificates.X509Certificate cert,
        System.Net.WebRequest req, int problem) { return true; }
}
"@
    }
    [System.Net.ServicePointManager]::CertificatePolicy = [TrustAllCertsPolicy]::new()
}
$httpClient = New-Object System.Net.Http.HttpClient($handler)

function New-AdminBearerToken {
    $exp = [datetimeoffset]::UtcNow.AddSeconds(60).ToUnixTimeSeconds()
    $header = [ordered]@{ alg = "HS256"; typ = "JWT" }
    $payload = [ordered]@{ iss = "SPE Remoting"; exp = $exp; aud = $protocolHost; name = "sitecore\admin" }

    $headerJson = $header | ConvertTo-Json -Compress
    $payloadJson = $payload | ConvertTo-Json -Compress
    $headerB64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($headerJson)).Split('=')[0].Replace('+', '-').Replace('/', '_')
    $payloadB64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($payloadJson)).Split('=')[0].Replace('+', '-').Replace('/', '_')

    $hmac = New-Object System.Security.Cryptography.HMACSHA256
    $hmac.Key = [System.Text.Encoding]::UTF8.GetBytes($sharedSecret)
    $sig = [Convert]::ToBase64String($hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes("$headerB64.$payloadB64"))).Split('=')[0].Replace('+', '-').Replace('/', '_')
    return "$headerB64.$payloadB64.$sig"
}

function Compress-Bytes {
    param([byte[]]$Bytes)
    $ms = New-Object System.IO.MemoryStream
    $gz = New-Object System.IO.Compression.GZipStream($ms, [System.IO.Compression.CompressionMode]::Compress, $true)
    $gz.Write($Bytes, 0, $Bytes.Length)
    $gz.Close()
    $out = $ms.ToArray()
    $ms.Close()
    return $out
}

function Send-RawBodyRequest {
    param(
        [byte[]]$Body,
        [string]$ContentEncoding = $null,
        [string]$SessionId = ([guid]::NewGuid().ToString())
    )

    $token = New-AdminBearerToken
    $httpClient.DefaultRequestHeaders.Authorization = `
        New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token)

    $url = "${serviceUrl}?sessionId=$SessionId&rawOutput=false&outputFormat=clixml&persistentSession=false"
    $content = New-Object System.Net.Http.ByteArrayContent(@(, $Body))
    $content.Headers.ContentType = New-Object System.Net.Http.Headers.MediaTypeHeaderValue("text/plain")
    if ($ContentEncoding) {
        # TryAddWithoutValidation preserves the case the test passes in;
        # ContentEncoding.Add roundtrips through a typed parser that
        # may normalize.
        [void]$content.Headers.TryAddWithoutValidation("Content-Encoding", $ContentEncoding)
    }

    $response = $httpClient.PostAsync($url, $content).Result
    return @{
        Response = $response
        SessionId = $SessionId
    }
}

function Get-RecentAuditLines {
    docker exec spe-cm-1 powershell -NoProfile -Command "Get-ChildItem C:/inetpub/wwwroot/App_Data/logs/SPE.log.*.txt | Sort-Object LastWriteTime -Descending | Select-Object -First 1 | ForEach-Object { Get-Content `$_.FullName }"
}

# ============================================================================
#  Test Group 1: Decompression bomb cap
# ============================================================================
Write-Host "`n  [Test Group 1: Decompression bomb cap]" -ForegroundColor White

# 32MB of zeros compresses to ~30KB but expands well past the 16MB default cap.
$bombPayload = New-Object byte[] (32 * 1024 * 1024)
$gzipped = Compress-Bytes -Bytes $bombPayload
Write-Host ("  Compressed bomb size: {0} bytes (decoded: {1} bytes)" -f $gzipped.Length, $bombPayload.Length)

$bombResult = Send-RawBodyRequest -Body $gzipped -ContentEncoding "gzip"
Assert-Equal ([int]$bombResult.Response.StatusCode) 413 "Decompression bomb is rejected with 413 Payload Too Large"

Start-Sleep -Seconds 1
$bombAudit = Get-RecentAuditLines | Where-Object { $_ -match "sessionId=$($bombResult.SessionId)" -and $_ -match 'reason=bodyTooLarge' } | Select-Object -First 1
Assert-NotNull $bombAudit "Audit log carries action=scriptRejected reason=bodyTooLarge for the bomb request"
if ($bombAudit) {
    Assert-True ($bombAudit -match 'cap=\d+') "bodyTooLarge audit line includes cap=<bytes>"
}

# ============================================================================
#  Test Group 2: Malformed gzip body
# ============================================================================
Write-Host "`n  [Test Group 2: Malformed gzip body]" -ForegroundColor White

# Plain bytes labelled as gzip - GZipStream throws InvalidDataException
$junk = [System.Text.Encoding]::UTF8.GetBytes("this is not actually gzip-compressed content")
$malformedResult = Send-RawBodyRequest -Body $junk -ContentEncoding "gzip"
Assert-Equal ([int]$malformedResult.Response.StatusCode) 400 "Malformed gzip body is rejected with 400 (not unhandled 500)"

Start-Sleep -Seconds 1
$malformedAudit = Get-RecentAuditLines | Where-Object { $_ -match "sessionId=$($malformedResult.SessionId)" -and $_ -match 'reason=malformedBody' } | Select-Object -First 1
Assert-NotNull $malformedAudit "Audit log carries action=scriptRejected reason=malformedBody for the malformed request"

# ============================================================================
#  Test Group 3: Case-insensitive Content-Encoding match
# ============================================================================
Write-Host "`n  [Test Group 3: Case-insensitive Content-Encoding]" -ForegroundColor White

# Real client always sends "gzip" lowercase; an alternate client or proxy
# could send "GZIP". Server must decompress either. Assert against the
# response body too: a server that failed to detect gzip would treat the
# compressed bytes as plaintext UTF-8 (mojibake), the script would not
# parse, and the marker string would never reach the response.
$marker = "case-insensitive-marker-$(Get-Random)"
$scriptBody = "`"$marker`""
$bodyBytes = [System.Text.Encoding]::UTF8.GetBytes($scriptBody)
$gzippedBody = Compress-Bytes -Bytes $bodyBytes

$upperResult = Send-RawBodyRequest -Body $gzippedBody -ContentEncoding "GZIP"
Assert-Equal ([int]$upperResult.Response.StatusCode) 200 "Server accepts Content-Encoding: GZIP (uppercase)"
$upperBody = $upperResult.Response.Content.ReadAsStringAsync().Result
Assert-True ($upperBody -match [regex]::Escape($marker)) "Response body carries the decompressed script output (proves uppercase header was decoded)"

$mixedResult = Send-RawBodyRequest -Body $gzippedBody -ContentEncoding "GzIp"
Assert-Equal ([int]$mixedResult.Response.StatusCode) 200 "Server accepts Content-Encoding: GzIp (mixed case)"
$mixedBody = $mixedResult.Response.Content.ReadAsStringAsync().Result
Assert-True ($mixedBody -match [regex]::Escape($marker)) "Response body carries the decompressed script output (proves mixed-case header was decoded)"
