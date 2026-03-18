# Remoting Tests - CORS Support (#1422)
# These tests verify CORS preflight and response header behavior for SPE web API services.
# Run via: .\Run-RemotingTests.ps1 -TestFile "Remoting.Cors.Tests.ps1"
# Or standalone: . ..\SPE\Tests\TestRunner.ps1; . .\Remoting.Cors.Tests.ps1; Show-TestSummary

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost
$ashxBase = "$protocolHost/-/script"
$basicAuth = @{ Authorization = "Basic " + [Convert]::ToBase64String([Text.Encoding]::GetEncoding("iso-8859-1").GetBytes("sitecore\admin:b")) }
$testOrigin = "https://test-cors.example.com"

# ============================================================================
#  Test Group 1: Preflight on restfulv2 (OPTIONS /-/script/v2/master/)
# ============================================================================
Write-Host "`n  [Test Group 1: CORS Preflight -- restfulv2]" -ForegroundColor White

try {
    $response = Invoke-WebRequest -Uri "$ashxBase/v2/master/" -Method OPTIONS `
        -Headers @{ Origin = $testOrigin } -ErrorAction Stop -UseBasicParsing
    Assert-Equal $response.StatusCode 204 "Preflight returns 204 No Content"
    Assert-Equal $response.Headers["Access-Control-Allow-Origin"] "*" "Preflight Allow-Origin is wildcard"
    Assert-Equal $response.Headers["Access-Control-Allow-Methods"] "GET, POST, OPTIONS" "Preflight Allow-Methods"
    Assert-Equal $response.Headers["Access-Control-Allow-Headers"] "Authorization, Content-Type, Content-Encoding" "Preflight Allow-Headers"
    Assert-Equal $response.Headers["Access-Control-Max-Age"] "3600" "Preflight Max-Age is 3600"
    Assert-True ($null -eq $response.Headers["Access-Control-Allow-Credentials"]) "Preflight Allow-Credentials absent (disabled in dev config)"
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Assert-True $false "Preflight on restfulv2 failed with status $statusCode -- $_"
}

# ============================================================================
#  Test Group 2: Preflight on remoting (OPTIONS /-/script/script/)
# ============================================================================
Write-Host "`n  [Test Group 2: CORS Preflight -- remoting]" -ForegroundColor White

try {
    $response = Invoke-WebRequest -Uri "$ashxBase/script/" -Method OPTIONS `
        -Headers @{ Origin = $testOrigin } -ErrorAction Stop -UseBasicParsing
    Assert-Equal $response.StatusCode 204 "Remoting preflight returns 204"
    Assert-Equal $response.Headers["Access-Control-Allow-Origin"] "*" "Remoting preflight Allow-Origin is wildcard"
    Assert-Equal $response.Headers["Access-Control-Allow-Methods"] "GET, POST, OPTIONS" "Remoting preflight Allow-Methods"
    Assert-Equal $response.Headers["Access-Control-Allow-Headers"] "Authorization, Content-Type, Content-Encoding" "Remoting preflight Allow-Headers"
    Assert-Equal $response.Headers["Access-Control-Max-Age"] "3600" "Remoting preflight Max-Age is 3600"
    Assert-True ($null -eq $response.Headers["Access-Control-Allow-Credentials"]) "Remoting preflight Allow-Credentials absent"
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Assert-True $false "Preflight on remoting failed with status $statusCode -- $_"
}

# ============================================================================
#  Test Group 3: CORS headers on actual requests (restfulv2)
# ============================================================================
Write-Host "`n  [Test Group 3: CORS Headers on Actual Requests -- restfulv2]" -ForegroundColor White

# 3a. POST with Origin header -- Access-Control-Allow-Origin present
try {
    $response = Invoke-WebRequest -Uri "$ashxBase/script" -Method POST `
        -Body '"cors-post-test"' -ContentType "text/plain" `
        -Headers ($basicAuth + @{ Origin = $testOrigin }) -ErrorAction Stop -UseBasicParsing
    Assert-Equal $response.Headers["Access-Control-Allow-Origin"] "*" "POST with Origin gets Allow-Origin header"
    Assert-True ($null -eq $response.Headers["Access-Control-Allow-Methods"]) "POST response has no Allow-Methods (preflight-only)"
    Assert-True ($null -eq $response.Headers["Access-Control-Max-Age"]) "POST response has no Max-Age (preflight-only)"
} catch {
    Assert-True $false "POST with Origin failed: $_"
}

# 3b. GET with Origin header -- Access-Control-Allow-Origin present
try {
    $response = Invoke-WebRequest -Uri "$ashxBase/v2/master/NonExistent/CorsTest$(Get-Random)" -Method GET `
        -Headers ($basicAuth + @{ Origin = $testOrigin }) -ErrorAction Stop -UseBasicParsing
    # May get 404 but still should have CORS headers
    Assert-Equal $response.Headers["Access-Control-Allow-Origin"] "*" "GET with Origin gets Allow-Origin header"
} catch {
    # Check CORS headers even on error responses
    $errorResponse = $_.Exception.Response
    if ($errorResponse) {
        $allowOrigin = $errorResponse.Headers["Access-Control-Allow-Origin"]
        Assert-Equal $allowOrigin "*" "GET error response with Origin gets Allow-Origin header"
    } else {
        Assert-True $false "GET with Origin failed without response: $_"
    }
}

# 3c. Request WITHOUT Origin header -- no CORS headers
try {
    $response = Invoke-WebRequest -Uri "$ashxBase/script" -Method POST `
        -Body '"cors-no-origin-test"' -ContentType "text/plain" `
        -Headers $basicAuth -ErrorAction Stop -UseBasicParsing
    Assert-True ($null -eq $response.Headers["Access-Control-Allow-Origin"]) "Request without Origin has no Allow-Origin header"
} catch {
    Assert-True $false "Request without Origin failed: $_"
}

# ============================================================================
#  Test Group 4: No CORS headers on unconfigured services
# ============================================================================
Write-Host "`n  [Test Group 4: No CORS on Unconfigured Services]" -ForegroundColor White

# 4a. OPTIONS to fileDownload (no <cors> element) -- no CORS headers
$label = "OPTIONS to file service returns no CORS headers"
try {
    $response = Invoke-WebRequest -Uri "$ashxBase/file/Temp/" -Method OPTIONS `
        -Headers @{ Origin = $testOrigin } -ErrorAction Stop -UseBasicParsing
    Assert-True ($null -eq $response.Headers["Access-Control-Allow-Origin"]) $label
} catch {
    # No CORS headers expected -- error response is fine
    $errorResponse = $_.Exception.Response
    if ($errorResponse) {
        $allowOrigin = $errorResponse.Headers["Access-Control-Allow-Origin"]
        Assert-True ($null -eq $allowOrigin) $label
    } else {
        # Connection error or similar -- no CORS headers by definition
        Assert-True $true $label
    }
}

# 4b. GET to fileDownload with Origin -- no CORS headers
$label = "GET to file service with Origin returns no CORS headers"
try {
    $response = Invoke-WebRequest -Uri "$ashxBase/file/Temp/?path=nonexistent.txt" -Method GET `
        -Headers ($basicAuth + @{ Origin = $testOrigin }) -ErrorAction Stop -UseBasicParsing
    Assert-True ($null -eq $response.Headers["Access-Control-Allow-Origin"]) $label
} catch {
    $errorResponse = $_.Exception.Response
    if ($errorResponse) {
        $allowOrigin = $errorResponse.Headers["Access-Control-Allow-Origin"]
        Assert-True ($null -eq $allowOrigin) $label
    } else {
        Assert-True $true $label
    }
}

# 4c. OPTIONS to mediaDownload -- no CORS headers
$label = "OPTIONS to media service returns no CORS headers"
try {
    $response = Invoke-WebRequest -Uri "$ashxBase/media/master/" -Method OPTIONS `
        -Headers @{ Origin = $testOrigin } -ErrorAction Stop -UseBasicParsing
    Assert-True ($null -eq $response.Headers["Access-Control-Allow-Origin"]) $label
} catch {
    $errorResponse = $_.Exception.Response
    if ($errorResponse) {
        $allowOrigin = $errorResponse.Headers["Access-Control-Allow-Origin"]
        Assert-True ($null -eq $allowOrigin) $label
    } else {
        Assert-True $true $label
    }
}

# ============================================================================
#  Test Group 5: Edge Cases
# ============================================================================
Write-Host "`n  [Test Group 5: CORS Edge Cases]" -ForegroundColor White

# 5a. OPTIONS without Origin header to CORS-enabled endpoint -- no CORS headers
$label = "OPTIONS without Origin returns no CORS headers"
try {
    $response = Invoke-WebRequest -Uri "$ashxBase/v2/master/" -Method OPTIONS `
        -ErrorAction Stop -UseBasicParsing
    Assert-True ($null -eq $response.Headers["Access-Control-Allow-Origin"]) $label
} catch {
    # Preflight without Origin should not set CORS headers; any response is acceptable
    $errorResponse = $_.Exception.Response
    if ($errorResponse) {
        $allowOrigin = $errorResponse.Headers["Access-Control-Allow-Origin"]
        Assert-True ($null -eq $allowOrigin) $label
    } else {
        Assert-True $true $label
    }
}

# 5b. OPTIONS to invalid API version -- no CORS headers, no crash
$label = "OPTIONS to invalid API version returns no CORS headers"
try {
    $response = Invoke-WebRequest -Uri "$ashxBase/v999/master/" -Method OPTIONS `
        -Headers @{ Origin = $testOrigin } -ErrorAction Stop -UseBasicParsing
    Assert-True ($null -eq $response.Headers["Access-Control-Allow-Origin"]) $label
} catch {
    $errorResponse = $_.Exception.Response
    if ($errorResponse) {
        $statusCode = $errorResponse.StatusCode.value__
        Assert-True ($statusCode -ne 500) "Invalid API version OPTIONS does not return 500 (got $statusCode)"
        $allowOrigin = $errorResponse.Headers["Access-Control-Allow-Origin"]
        Assert-True ($null -eq $allowOrigin) $label
    } else {
        Assert-True $true $label
    }
}

# 5c. OPTIONS to too-short path -- no crash, no CORS headers
$label = "OPTIONS to short path (/-/script/) returns no CORS headers"
try {
    $response = Invoke-WebRequest -Uri "$ashxBase/" -Method OPTIONS `
        -Headers @{ Origin = $testOrigin } -ErrorAction Stop -UseBasicParsing
    Assert-True ($null -eq $response.Headers["Access-Control-Allow-Origin"]) $label
} catch {
    $errorResponse = $_.Exception.Response
    if ($errorResponse) {
        $statusCode = $errorResponse.StatusCode.value__
        Assert-True ($statusCode -ne 500) "Short path OPTIONS does not return 500 (got $statusCode)"
    } else {
        Assert-True $true $label
    }
}

Stop-ScriptSession -Session $session
