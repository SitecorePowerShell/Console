$serverUrl = "https://spe.dev.local/"
$file = "C:\Temp\zoom-tiger.jpg"

$filename = [System.IO.Path]::GetFileName($file)
Add-Type -AssemblyName "System.Web"
$contentType = [System.Web.MimeMapping]::GetMimeMapping($file)

$url = @(
    $serverUrl, 
    "/sitecore modules/PowerShell/Services/RemoteScriptCall.ashx",
    "?script=/sitecore/media library/Images/$($filename)",
    "&sc_database=master&apiVersion=media&scriptDb=master"
) -join ''

$username = "admin"
$password = "b"
$authBytes = [System.Text.Encoding]::GetEncoding("iso-8859-1").GetBytes("$($username):$($password)")
$headers = @{
    Authorization = "Basic $([System.Convert]::ToBase64String($authBytes))"
}

Invoke-WebRequest -Uri $url -Method Post -InFile $file -ContentType $contentType -Headers $headers
