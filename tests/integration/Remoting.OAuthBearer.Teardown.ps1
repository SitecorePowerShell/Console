# Remoting.OAuthBearer.Teardown.ps1
# Removes the IntegrationTest OAuth Client fixture created by
# Remoting.OAuthBearer.Setup.ps1. Runs after Phase 7 swaps the active
# auth provider back to Shared Secret.

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost

Write-Host "`n  [OAuth Bearer Teardown: removing IntegrationTest OAuth Client]" -ForegroundColor Cyan

$removeResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $removed = @()
    $itemPath = "master:/sitecore/system/Modules/PowerShell/Settings/Access/Remoting Clients/IntegrationTest"
    $item = Get-Item -Path $itemPath -ErrorAction SilentlyContinue
    if ($item) {
        Remove-Item -Path $itemPath -Force -Recurse -Permanently
        $removed += "client"
    }
    $policyPath = "master:/sitecore/system/Modules/PowerShell/Settings/Access/Policies/IntegrationTest-Policy"
    $policy = Get-Item -Path $policyPath -ErrorAction SilentlyContinue
    if ($policy) {
        Remove-Item -Path $policyPath -Force -Recurse -Permanently
        $removed += "policy"
    }
    if ($removed.Count -gt 0) { return "REMOVED:$($removed -join ',')" }
    return "NOT_PRESENT"
} -Raw

Write-Host "    Teardown: $removeResult" -ForegroundColor Gray

Stop-ScriptSession -Session $session
