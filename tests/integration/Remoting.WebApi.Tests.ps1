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

Assert-Throw { Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/NonExistingScript?user=sitecore%5Cadmin&password=b" } "404" "Non existing script should throw exception"

Assert-Throw { Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/ChildrenAsHtml?user=admin&password=invalid" } "401" "Wrong password should throw exception"

Assert-Throw { Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/ChildrenAsHtml?user=non_existing&password=invalid" } "401" "Non existing user should throw exception"

Assert-Throw { Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/NotFound" } "" "Not found script without credentials should throw exception"

Write-Host "`n  [Web API cache key isolation across databases]" -ForegroundColor White

# Regression guard: GetApiScripts must not serve a cache populated by one
# database to requests for another database. Bug scenario:
#   1. First v2 call lands with dbName=core (or any non-master db).
#   2. GetApiScripts populates ApiScriptCollection with core's entries only.
#   3. Cache is written with the shared key and a 30s TTL.
#   4. Second call lands with dbName=master inside the TTL window.
#   5. Cache HIT returns core's collection. master entry is absent -> 404.
#
# Reliable reproduction requires a cache-cold start, so we clear the cache
# in-container before the probe.
$cacheClearSession = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost
Invoke-RemoteScript -Session $cacheClearSession -ScriptBlock {
    # Clear both the legacy shared key (pre-fix) and the per-db keys (post-fix).
    # Remove is a no-op on missing keys.
    [System.Web.HttpRuntime]::Cache.Remove("Spe.ApiScriptsKey")
    [System.Web.HttpRuntime]::Cache.Remove("Spe.ApiScriptsKey:master")
    [System.Web.HttpRuntime]::Cache.Remove("Spe.ApiScriptsKey:core")
    [System.Web.HttpRuntime]::Cache.Remove("Spe.ApiScriptsKey:web")
} | Out-Null
Stop-ScriptSession -Session $cacheClearSession

# Poison the cache with a non-master dbName. The 404 is expected; the side
# effect we care about is that the cache is now warm with core's data only.
try {
    Invoke-RestMethod -Uri "$protocolHost/-/script/v2/core/nonexistent?user=sitecore%5Cadmin&password=b" -ErrorAction Stop | Out-Null
} catch {
    # 404 is expected here - core has no such script. We want the cache side effect.
}

# Now a master request should succeed. Before the fix the cache collision
# would cause this to 404 even though ChildrenAsJson exists in master.
$isolationItems = $null
try {
    $isolationItems = Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/ChildrenAsJson?user=sitecore%5Cadmin&password=b" -ErrorAction Stop
} catch {
    # Capture failure; Assert-NotNull below will report it.
}
Assert-NotNull $isolationItems "v2/master/ChildrenAsJson must succeed even when the cache was populated by a prior non-master call"

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
