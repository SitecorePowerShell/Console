# Remoting Tests - SOAP Endpoint Profile Enforcement (Issue #1426)
# Tests that restriction profiles are enforced on the SOAP RemoteAutomation.asmx endpoint.
# These are run AFTER tests/configs/profiles/z.Spe.RestrictionProfiles.Tests.config is deployed.
# Run via: .\Run-RemotingTests.ps1 (automatically deployed and run in the profile phase)

$soapUrl = "$protocolHost/-/PowerShell/RemoteAutomation.asmx"

function Invoke-SoapExecuteScript {
    param(
        [string]$Script,
        [string]$UserName = "sitecore\admin",
        [string]$Password = "b"
    )
    $body = @"
<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
               xmlns:ns="http://sitecorepowershellextensions/">
  <soap:Body>
    <ns:ExecuteScriptBlockinSite2>
      <ns:userName>$UserName</ns:userName>
      <ns:password>$Password</ns:password>
      <ns:script>$([System.Security.SecurityElement]::Escape($Script))</ns:script>
      <ns:cliXmlArgs></ns:cliXmlArgs>
      <ns:siteName>website</ns:siteName>
      <ns:sessionId></ns:sessionId>
    </ns:ExecuteScriptBlockinSite2>
  </soap:Body>
</soap:Envelope>
"@
    $params = @{
        Uri         = $soapUrl
        Method      = "POST"
        Body        = $body
        ContentType = "text/xml; charset=utf-8"
        Headers     = @{ "SOAPAction" = '"http://sitecorepowershellextensions/ExecuteScriptBlockinSite2"' }
        ErrorAction = "Stop"
    }
    if ($PSVersionTable.PSVersion.Major -ge 6) {
        $params["SkipCertificateCheck"] = $true
    }
    return Invoke-WebRequest @params
}

# ============================================================================
#  Test Group 1: SOAP Profile Command Blocklist
# ============================================================================
Write-Host "`n  [Test Group 1: SOAP Profile Command Blocklist]" -ForegroundColor White

# 1a. Write commands blocked by read-only profile on SOAP
try {
    $response = Invoke-SoapExecuteScript -Script 'Remove-Item -Path "master:/content/nonexistent" -ErrorAction Stop'
    # If we get here, check if the response contains an error
    $blocked = $response.Content -match "blocked command" -or $response.Content -match "Remove-Item"
    Assert-True $blocked "SOAP read-only profile blocks Remove-Item"
} catch {
    # SOAP throws on server errors - exception message should indicate blocking
    $msg = $_.Exception.Message
    $blocked = $msg -match "blocked" -or $msg -match "Remove-Item" -or $_.Exception.Response.StatusCode -eq 500
    Assert-True $blocked "SOAP read-only profile blocks Remove-Item (server error)"
}

# 1b. Read commands still allowed on SOAP
try {
    $response = Invoke-SoapExecuteScript -Script 'Get-Item -Path "master:/" | Select-Object -ExpandProperty Name'
    $hasContent = $response.Content -match "sitecore"
    Assert-True $hasContent "SOAP read-only profile allows Get-Item"
} catch {
    Assert-True $false "SOAP read-only profile should allow Get-Item but threw: $($_.Exception.Message)"
}

# ============================================================================
#  Test Group 2: SOAP Language Mode Enforcement
# ============================================================================
Write-Host "`n  [Test Group 2: SOAP Language Mode Enforcement]" -ForegroundColor White

# The read-only profile sets ConstrainedLanguage on SOAP too
try {
    $response = Invoke-SoapExecuteScript -Script '$ExecutionContext.SessionState.LanguageMode.ToString()'
    $isCLM = $response.Content -match "ConstrainedLanguage"
    Assert-True $isCLM "SOAP enforces ConstrainedLanguage under read-only profile"
} catch {
    Assert-True $false "SOAP language mode check threw: $($_.Exception.Message)"
}

# ============================================================================
#  Test Group 3: SOAP Execution Escape Prevention
# ============================================================================
Write-Host "`n  [Test Group 3: SOAP Execution Escape Prevention]" -ForegroundColor White

try {
    $response = Invoke-SoapExecuteScript -Script 'Invoke-Expression "1+1"'
    $blocked = $response.Content -match "blocked" -or $response.Content -match "Invoke-Expression"
    Assert-True $blocked "SOAP blocks Invoke-Expression under read-only profile"
} catch {
    $msg = $_.Exception.Message
    $blocked = $msg -match "blocked" -or $msg -match "Invoke-Expression"
    Assert-True $blocked "SOAP blocks Invoke-Expression (server error)"
}

# ============================================================================
#  Test Group 4: SOAP Module Restriction
# ============================================================================
Write-Host "`n  [Test Group 4: SOAP Module Restriction]" -ForegroundColor White

try {
    $response = Invoke-SoapExecuteScript -Script 'Import-Module SqlServer'
    $blocked = $response.Content -match "blocked" -or $response.Content -match "Import-Module"
    Assert-True $blocked "SOAP blocks Import-Module under read-only profile"
} catch {
    $msg = $_.Exception.Message
    $blocked = $msg -match "blocked" -or $msg -match "Import-Module"
    Assert-True $blocked "SOAP blocks Import-Module (server error)"
}
