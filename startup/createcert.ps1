param (
    [ValidateNotNullOrEmpty()][string]$certificatename = "cert",
    [ValidateNotNullOrEmpty()][SecureString]$certificatepassword = ("b" | ConvertTo-SecureString -Force -AsPlainText),
    [ValidateNotNullOrEmpty()][string]$dnsName = "*.dev.local"
)

. $PSScriptRoot\rsakeytools.ps1

# setup certificate properties including the commonName (DNSName) property for Chrome 58+
$certificate = New-SelfSignedCertificate `
    -Subject $dnsName `
    -DnsName $dnsName `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -NotBefore (Get-Date) `
    -NotAfter (Get-Date).AddYears(10) `
    -CertStoreLocation "cert:CurrentUser\My" `
    -FriendlyName $dnsName `
    -HashAlgorithm SHA256 `
    -KeyUsage DigitalSignature, KeyEncipherment, DataEncipherment `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.1") `
    -KeyExportPolicy Exportable `
    -KeySpec KeyExchange
$certificatePath = 'Cert:\CurrentUser\My\' + ($certificate.ThumbPrint)
# create temporary certificate path
$tmpPath = $PSScriptRoot
if ([string]::IsNullOrEmpty($tmpPath)) {
    $tmpPath = $PWD
}
if (!(test-path $tmpPath)) {
    New-Item -ItemType Directory -Force -Path $tmpPath
}
# set certificate password here
$pfxPassword = $certificatepassword
$pfxFilePath = $tmpPath + "\" + $certificatename + ".pfx"
$cerFilePath = $tmpPath + "\" + $certificatename + ".cer"
$cerPasswordFilePath = $tmpPath + "\" + $certificatename + ".password.txt"

# create pfx certificate
Write-Host "Exporting certificate to $($pfxFilePath)"
Export-PfxCertificate -Cert $certificatePath -FilePath $pfxFilePath -Password $pfxPassword
#Export-Certificate -Cert $certificatePath -FilePath $cerFilePath
$bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($certificatepassword)
$unsecuredPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
$unsecuredPassword | Out-File -FilePath $cerPasswordFilePath
# import the pfx certificate
Write-Host "Importing certificate"
Import-PfxCertificate -FilePath $pfxFilePath Cert:\LocalMachine\My -Password $pfxPassword -Exportable
$pfx = Import-PfxCertificate -FilePath $pfxFilePath -CertStoreLocation Cert:\LocalMachine\Root -Password $pfxPassword -Exportable
#$pfx = Get-PfxCertificate -FilePath $pfxFilePath -Password $pfxPassword
# optionally delete the physical certificates (don’t delete the pfx file as you need to copy this to your app directory)
#Remove-Item $cerFilePath

###
$content = @(
    '-----BEGIN CERTIFICATE-----'
    [System.Convert]::ToBase64String($certificate.RawData, 'InsertLineBreaks')
    '-----END CERTIFICATE-----'
)

$content | Out-File -FilePath $cerFilePath -Encoding ascii

$keyPasswordFilePath = $tmpPath + "\" + $certificatename + ".key"
$parameters = $certificate.PrivateKey.ExportParameters($true)
$data = [RSAKeyUtils]::PrivateKeyToPKCS8($parameters)
$content = @(
    '-----BEGIN PRIVATE KEY-----'
    [System.Convert]::ToBase64String($data, 'InsertLineBreaks')
    '-----END PRIVATE KEY-----'
)

$content | Out-File -FilePath $keyPasswordFilePath -Encoding ascii
#& "C:\Program Files\Git\usr\bin\openssl.exe" pkcs12 -in $pfxFilePath -nocerts -nodes -out $keyPasswordFilePath -passin pass:$unsecuredPassword
#& "C:\Program Files\Git\usr\bin\openssl.exe" pkcs12 -in $pfxFilePath -clcerts -nokeys -out $cerFilePath -passin pass:$unsecuredPassword
