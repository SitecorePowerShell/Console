# Remoting Tests - Authorization Cache Behavior
# Validates that authorization caching works correctly without stampede/thrashing

Write-Host "`n  [Authorization Cache]" -ForegroundColor White

# Test 1: Authorization remains cached across rapid requests
$session = New-ScriptSession -Username "sitecore\admin" -Password "b" -ConnectionUri $protocolHost
$results = @()
for ($i = 0; $i -lt 10; $i++) {
    $result = Invoke-RemoteScript -Session $session -ScriptBlock { "ok" } -Raw
    $results += $result
}
$allOk = ($results | Where-Object { $_ -eq "ok" }).Count -eq 10
Assert-Equal $allOk $true "authorization remains cached across rapid requests"
Stop-ScriptSession -Session $session

# Test 2: Cache stampede under concurrent load
$jobs = @()
for ($i = 0; $i -lt 5; $i++) {
    $jobs += Start-Job -ScriptBlock {
        param($uri)
        Import-Module -Name SPE -Force
        $s = New-ScriptSession -Username "sitecore\admin" -Password "b" -ConnectionUri $uri
        $result = Invoke-RemoteScript -Session $s -ScriptBlock { "ok" } -Raw
        Stop-ScriptSession -Session $s
        $result
    } -ArgumentList $protocolHost
}
$jobResults = $jobs | Wait-Job | Receive-Job
$jobs | Remove-Job
$allOk = ($jobResults | Where-Object { $_ -eq "ok" }).Count -eq 5
Assert-Equal $allOk $true "concurrent requests all succeed without cache stampede"

# Test 3: Authorization works after cache eviction (distinct sessions)
$evictionResults = @()
for ($i = 0; $i -lt 5; $i++) {
    $s = New-ScriptSession -Username "sitecore\admin" -Password "b" -ConnectionUri $protocolHost
    $result = Invoke-RemoteScript -Session $s -ScriptBlock { "ok" } -Raw
    $evictionResults += $result
    Stop-ScriptSession -Session $s
}
$allOk = ($evictionResults | Where-Object { $_ -eq "ok" }).Count -eq 5
Assert-Equal $allOk $true "authorization works correctly across distinct sessions"

# Test 4: Expired entries don't block fresh authorization
$session = New-ScriptSession -Username "sitecore\admin" -Password "b" -ConnectionUri $protocolHost
$before = Invoke-RemoteScript -Session $session -ScriptBlock { "before" } -Raw
Assert-Equal $before "before" "authorization works before expiration"
Stop-ScriptSession -Session $session

Write-Host "    Waiting for cache expiration (11s)..." -ForegroundColor Gray
Start-Sleep -Seconds 11

$session = New-ScriptSession -Username "sitecore\admin" -Password "b" -ConnectionUri $protocolHost
$after = Invoke-RemoteScript -Session $session -ScriptBlock { "after" } -Raw
Assert-Equal $after "after" "authorization works after cache expiration"
Stop-ScriptSession -Session $session
