# Remoting Tests - Web Api
# Converted from Pester to custom assert format

Write-Host "`n  [Web API Responses - Setup]" -ForegroundColor White

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost
Invoke-RemoteScript -Session $session -ScriptBlock {
    # Getting Started (may not exist in all instances)
    $module = Get-Item "master:" -ID "{ED2CF34E-1A59-444D-806E-51DB1E560093}" -ErrorAction SilentlyContinue
    if ($module) { $module.Enabled = "1" }
    # Advanced Web API
    $advModule = Get-Item "master:" -ID "{CACE2E80-0CD2-48BD-894E-8784B7F2B00B}" -ErrorAction SilentlyContinue
    if ($advModule) { $advModule.Enabled = "1" }
}
Stop-ScriptSession -Session $session

# After a Sitecore restart, v2 API scripts may not be available immediately.
# Probe with a short retry before running assertions.
$webApiReady = $false
for ($i = 0; $i -lt 3; $i++) {
    try {
        $probe = Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/ChildrenAsJson" -method Post `
            -Body @{user="sitecore\admin"; password="b"; depth=1} -ErrorAction Stop
        if ($probe) { $webApiReady = $true; break }
    } catch {
        Write-Host "  Web API not ready yet, retrying in 5s..." -ForegroundColor Yellow
        Start-Sleep -Seconds 5
    }
}

if (-not $webApiReady) {
    Skip-Test "Web API tests" "v2 API scripts not available after restart"
    Stop-ScriptSession -Session $session
    return
}

Write-Host "`n  [POST Methods]" -ForegroundColor White

$postParams = @{user="sitecore\admin"; password="b"; depth=1}
$items = Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/ChildrenAsJson" -method Post -Body $postParams
Assert-Equal $items.Count 6 "ChildrenAsJson script should return 6 objects as children to root item"

$postParams = @{user="sitecore\admin"; password="b"}
$html = Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/ChildrenAsHtml" -method Post -Body $postParams
Assert-Type $html "XmlDocument" "ChildrenAsHtml Script should return XML Document Object"

$postParams = @{user="sitecore\admin"; password="b"}
$result = Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/HomeAndDescendants?offset=0&limit=2&fields=(Name,ItemPath,Id)" -method Post -Body $postParams
Assert-Type $result "PSCustomObject" "HomeAndDescendants Script should return JSON object"
Assert-Equal $result.Status "Success" "HomeAndDescendants Status should be Success"
Assert-Equal $result.Results.Count 2 "HomeAndDescendants should return 2 results"
Assert-Equal $result.Results[0].Name "Home" "HomeAndDescendants first result should be Home"

Write-Host "`n  [GET Methods]" -ForegroundColor White

$items = Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/ChildrenAsJson?user=sitecore%5Cadmin&password=b"
Assert-Equal $items.Count 6 "ChildrenAsJson Script - should return 6 objects as children to root item"

$html = Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/ChildrenAsHtml?user=sitecore%5Cadmin&password=b"
Assert-Type $html "XmlDocument" "ChildrenAsHtml script should return XML Document Object"

Write-Host "`n  [Web API invalid calls]" -ForegroundColor White

Assert-Throw { Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/NonExistingScript?user=sitecore%5Cadmin&password=b" } "(404) Not Found" "Non existing script should throw exception"

Assert-Throw { Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/ChildrenAsHtml?user=admin&password=invalid" } "(401) Unauthorized" "Wrong password should throw exception"

Assert-Throw { Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/ChildrenAsHtml?user=non_existing&password=invalid" } "(401) Unauthorized" "Non existing user should throw exception"

Assert-Throw { Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/NotFound" } "" "Not found script without credentials should throw exception"

Write-Host "`n  [Web API Responses - Teardown]" -ForegroundColor White

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost
Invoke-RemoteScript -Session $session -ScriptBlock {
    # Getting Started
    $module = Get-Item "master:" -ID "{ED2CF34E-1A59-444D-806E-51DB1E560093}" -ErrorAction SilentlyContinue
    if ($module) { $module.Enabled = "" }
    # Advanced Web API
    $advModule = Get-Item "master:" -ID "{CACE2E80-0CD2-48BD-894E-8784B7F2B00B}" -ErrorAction SilentlyContinue
    if ($advModule) { $advModule.Enabled = "" }
}
Stop-ScriptSession -Session $session
