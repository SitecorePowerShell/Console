# Remoting Tests - Item Path Restrictions (Issue #1426)
# Tests that item path restrictions in restriction profiles prevent access to
# sensitive content tree paths. Runs in Phase 3 with the read-only profile active.
# The read-only profile blocks: /sitecore/system/Modules/PowerShell/Settings/Remoting
# Requires: z.Spe.RestrictionProfiles.Tests.config deployed (profile="read-only")

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost

# ============================================================================
#  Test Group 13: Item Path Blocklist - Get-Item
# ============================================================================
Write-Host "`n  [Test Group 13: Item Path Blocklist - Get-Item]" -ForegroundColor White

# 13a. Get-Item on blocked path returns nothing
$blockedResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $item = Get-Item -Path "master:/sitecore/system/Modules/PowerShell/Settings/Remoting" -ErrorAction SilentlyContinue
    if ($item) { "ACCESSIBLE" } else { "BLOCKED" }
} -Raw
Assert-Equal $blockedResult "BLOCKED" "Get-Item on blocked path (/Settings/Remoting) returns nothing"

# 13b. Prefix matching - child of blocked path also blocked
$childResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $item = Get-Item -Path "master:/sitecore/system/Modules/PowerShell/Settings/Remoting/API Keys" -ErrorAction SilentlyContinue
    if ($item) { "ACCESSIBLE" } else { "BLOCKED" }
} -Raw
Assert-Equal $childResult "BLOCKED" "Get-Item on child of blocked path (API Keys) also blocked"

# 13c. Deeply nested child also blocked
$deepResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $item = Get-Item -Path "master:/sitecore/system/Modules/PowerShell/Settings/Remoting/Restriction Profiles" -ErrorAction SilentlyContinue
    if ($item) { "ACCESSIBLE" } else { "BLOCKED" }
} -Raw
Assert-Equal $deepResult "BLOCKED" "Get-Item on nested child (Restriction Profiles) also blocked"

# 13d. Allowed path still works
$allowedResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $item = Get-Item -Path "master:/sitecore/content" -ErrorAction SilentlyContinue
    if ($item) { "ACCESSIBLE" } else { "BLOCKED" }
} -Raw
Assert-Equal $allowedResult "ACCESSIBLE" "Get-Item on allowed path (/sitecore/content) works"

# 13e. Sitecore root still accessible (not under blocked prefix)
$rootResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $item = Get-Item -Path "master:/" -ErrorAction SilentlyContinue
    if ($item) { "ACCESSIBLE" } else { "BLOCKED" }
} -Raw
Assert-Equal $rootResult "ACCESSIBLE" "Get-Item on sitecore root still works"

# 13f. Sibling of blocked path still accessible
$siblingResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $item = Get-Item -Path "master:/sitecore/system/Modules/PowerShell/Settings" -ErrorAction SilentlyContinue
    if ($item) { "ACCESSIBLE" } else { "BLOCKED" }
} -Raw
Assert-Equal $siblingResult "ACCESSIBLE" "Get-Item on parent of blocked path (/Settings) still works"

# ============================================================================
#  Test Group 14: Item Path Blocklist - Get-ChildItem
# ============================================================================
Write-Host "`n  [Test Group 14: Item Path Blocklist - Get-ChildItem]" -ForegroundColor White

# 14a. Get-ChildItem on parent should filter out blocked children
$childItemsResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $children = Get-ChildItem -Path "master:/sitecore/system/Modules/PowerShell/Settings" -ErrorAction SilentlyContinue
    $names = $children | ForEach-Object { $_.Name }
    if ($names -contains "Remoting") { "VISIBLE" } else { "FILTERED" }
} -Raw
Assert-Equal $childItemsResult "FILTERED" "Get-ChildItem filters out blocked child (Remoting) from results"

# 14b. Get-ChildItem on blocked path itself returns nothing
$blockedChildItems = Invoke-RemoteScript -Session $session -ScriptBlock {
    $children = Get-ChildItem -Path "master:/sitecore/system/Modules/PowerShell/Settings/Remoting" -ErrorAction SilentlyContinue
    if ($children) { "HAS_CHILDREN:$($children.Count)" } else { "EMPTY" }
} -Raw
Assert-Like $blockedChildItems "EMPTY*" "Get-ChildItem on blocked path returns no children"

# ============================================================================
#  Test Group 15: Item Path Blocklist - Access by ID
# ============================================================================
Write-Host "`n  [Test Group 15: Item Path Blocklist - Access by ID]" -ForegroundColor White

# 15a. Access by ID using the Remoting folder's serialized GUID.
# The Remoting folder is created by SCS with a fixed ID.
$idTestResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $remotingId = "{1FB4BDEC-B064-4BF5-8D5B-B78978C2245B}"
    $byId = Get-Item -Path "master:" -ID $remotingId -ErrorAction SilentlyContinue
    if ($byId) { "ACCESSIBLE_BY_ID" } else { "BLOCKED_BY_ID" }
} -Raw
Assert-Equal $idTestResult "BLOCKED_BY_ID" "Get-Item by ID on blocked path is also blocked"

# ============================================================================
#  Test Group 16: Item Path Restriction Error Details
# ============================================================================
Write-Host "`n  [Test Group 16: Item Path Restriction Error Details]" -ForegroundColor White

# 16a. Error record contains the profile name
$errorResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    $err = $null
    Get-Item -Path "master:/sitecore/system/Modules/PowerShell/Settings/Remoting" -ErrorVariable err -ErrorAction SilentlyContinue
    if ($err) {
        $firstErr = $err | Select-Object -First 1
        "$($firstErr.FullyQualifiedErrorId)|$($firstErr.Exception.Message)"
    } else {
        "NO_ERROR"
    }
} -Raw
Assert-Like $errorResult "ItemPathRestricted*" "Error ID is ItemPathRestricted for blocked path"
Assert-Like $errorResult "*read-only*" "Error message includes the profile name"

# Cleanup
Stop-ScriptSession -Session $session
