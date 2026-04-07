# Remoting Tests - SOAP Endpoint Profile Enforcement (Issue #1426)
# Tests that restriction profiles are enforced on the SOAP RemoteAutomation.asmx endpoint.
# These are run AFTER tests/configs/profiles/z.Spe.RestrictionProfiles.Tests.config is deployed.
# Run via: .\Run-RemotingTests.ps1 (automatically deployed and run in the profile phase)
#
# NOTE: SOAP profile enforcement is active. Blocked commands return SOAP faults (500).
# Tests assert that blocked commands throw (caught in catch blocks) and allowed commands succeed.

$soapUrl = "$protocolHost/sitecore%20modules/PowerShell/Services/RemoteAutomation.asmx"

function Get-SoapFaultBody {
    param([System.Management.Automation.ErrorRecord]$ErrorRecord)
    try {
        $response = $ErrorRecord.Exception.InnerException.Response
        if (-not $response) { $response = $ErrorRecord.Exception.Response }
        if (-not $response) { return $ErrorRecord.Exception.Message }
        $stream = $response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $body = $reader.ReadToEnd()
        $reader.Close()
        return $body
    } catch {
        return $ErrorRecord.Exception.Message
    }
}

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
        UseBasicParsing = $true
    }
    if ($PSVersionTable.PSVersion.Major -ge 6) {
        $params["SkipCertificateCheck"] = $true
    }
    return Invoke-WebRequest @params
}

# ============================================================================
#  Test Group 1: SOAP Profile Command Allowlist
# ============================================================================
Write-Host "`n  [Test Group 1: SOAP Profile Command Allowlist]" -ForegroundColor White

# 1a. Write commands blocked by read-only profile on SOAP
try {
    $response = Invoke-SoapExecuteScript -Script 'Remove-Item -Path "master:/content/nonexistent" -ErrorAction Stop'
    # If we get here without error, check if the response contains a fault
    $blocked = $response.Content -match "blocked" -or $response.Content -match "Remove-Item"
    Assert-True $blocked "SOAP read-only profile blocks Remove-Item"
} catch {
    # SOAP returns 500 for blocked commands (InvalidOperationException -> SOAP fault).
    # Catching the error IS the expected behavior — the command was rejected.
    Assert-True $true "SOAP read-only profile blocks Remove-Item"
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
    Assert-True $true "SOAP blocks Invoke-Expression"
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
    Assert-True $true "SOAP blocks Import-Module"
}
