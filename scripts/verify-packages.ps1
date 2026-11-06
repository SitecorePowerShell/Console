#Requires -Version 5.1
<#
.SYNOPSIS
    Verifies that generated packages contain all items defined by SCS module.json files.
.DESCRIPTION
    Compares serialized YAML items (source of truth from SCS modules) against
    the package XML manifest to detect missing or extra items.
.EXAMPLE
    .\Verify-Packages.ps1
#>

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path "$PSScriptRoot/..").Path
$modulesDir = Join-Path $projectRoot "serialization/modules"
$serializationDir = Join-Path $modulesDir "serialization"

# ── Step 1: Parse module.json files ──────────────────────────────────────────

Write-Host "`n=== Step 1: Parsing module.json files ===" -ForegroundColor Cyan

$moduleJsonFiles = Get-ChildItem -Path $modulesDir -Filter "*.module.json"
$folderToDatabase = @{}  # serialization folder name -> database
$folderToPath = @{}      # serialization folder name -> Sitecore path
$expectedRoles = @()
$expectedUsers = @()

foreach ($file in $moduleJsonFiles) {
    $module = Get-Content $file.FullName -Raw | ConvertFrom-Json
    Write-Host "  Module: $($module.namespace)" -ForegroundColor Gray

    if ($module.PSObject.Properties["items"] -and $module.items.PSObject.Properties["includes"]) {
        foreach ($include in $module.items.includes) {
            $folderToDatabase[$include.name] = $include.database
            $folderToPath[$include.name] = $include.path
            Write-Host "    Include: $($include.name) -> $($include.database):$($include.path)"
        }
    }

    if ($module.PSObject.Properties["roles"]) {
        foreach ($role in $module.roles) {
            $expectedRoles += "$($role.domain)\$($role.pattern)"
        }
    }

    if ($module.PSObject.Properties["users"]) {
        foreach ($user in $module.users) {
            $expectedUsers += "$($user.domain)\$($user.pattern)"
        }
    }
}

Write-Host "  Folder-to-database mappings: $($folderToDatabase.Count)"
Write-Host "  Expected roles: $($expectedRoles.Count), Expected users: $($expectedUsers.Count)"

# ── Step 2: Parse YAML files ────────────────────────────────────────────────

Write-Host "`n=== Step 2: Parsing serialized YAML files ===" -ForegroundColor Cyan

$yamlItems = @{}  # GUID -> object with Path, Database, File
$yamlErrors = @()

$topLevelFolders = Get-ChildItem -Path $serializationDir -Directory |
    Where-Object { $_.Name -ne "_roles" -and $_.Name -ne "_users" }

foreach ($folder in $topLevelFolders) {
    $database = $folderToDatabase[$folder.Name]
    if (-not $database) {
        $yamlErrors += "No database mapping found for serialization folder: $($folder.Name)"
        continue
    }

    $ymlFiles = Get-ChildItem -Path $folder.FullName -Filter "*.yml" -Recurse
    foreach ($ymlFile in $ymlFiles) {
        $lines = Get-Content $ymlFile.FullName -TotalCount 10
        $id = $null
        $path = $null

        foreach ($line in $lines) {
            $cleanLine = $line -replace '\r', ''
            if ($cleanLine -match '^ID:\s*"?([0-9a-fA-F-]+)"?\s*$') {
                $id = $Matches[1]
            }
            if ($cleanLine -match '^Path:\s*(.+)$') {
                $path = $Matches[1].Trim().Trim('"')
            }
        }

        if (-not $id) {
            $yamlErrors += "No ID found in: $($ymlFile.FullName)"
            continue
        }

        $normalizedGuid = "{" + $id.ToUpper() + "}"

        if ($yamlItems.ContainsKey($normalizedGuid)) {
            $yamlErrors += "Duplicate YAML GUID $normalizedGuid in $($ymlFile.FullName) and $($yamlItems[$normalizedGuid].File)"
        }

        $yamlItems[$normalizedGuid] = [PSCustomObject]@{
            Guid     = $normalizedGuid
            Path     = $path
            Database = $database
            File     = $ymlFile.FullName
            Folder   = $folder.Name
        }
    }
}

Write-Host "  YAML items parsed: $($yamlItems.Count)"
if ($yamlErrors.Count -gt 0) {
    Write-Host "  YAML parse errors: $($yamlErrors.Count)" -ForegroundColor Yellow
    foreach ($err in $yamlErrors) { Write-Host "    WARNING: $err" -ForegroundColor Yellow }
}

# ── Step 3: Parse package XML ───────────────────────────────────────────────

Write-Host "`n=== Step 3: Parsing package XML ===" -ForegroundColor Cyan

$packageXmlFiles = @(Get-ChildItem -Path (Join-Path $projectRoot "_output") -Filter "Sitecore.PowerShell.Extensions-*.xml" |
    Where-Object { $_.Name -notmatch "-IAR" })

if ($packageXmlFiles.Count -eq 0) {
    Write-Host "ERROR: No package XML found in _output/" -ForegroundColor Red
    exit 1
}

$packageXml = $packageXmlFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1
Write-Host "  Package file: $($packageXml.Name)"

[xml]$xml = Get-Content $packageXml.FullName -Raw

$xmlItems = @{}       # GUID -> object with Path, Database
$xmlDuplicates = @()  # duplicate entries
$xmlRoles = @()
$xmlUsers = @()

# Parse xitems sections
$xitemsSections = @($xml.project.Sources.ChildNodes | Where-Object { $_.LocalName -eq "xitems" })
foreach ($section in $xitemsSections) {
    $nameNode = $section.SelectSingleNode("Name")
    $sectionName = if ($nameNode) { $nameNode.InnerText } else { $section.LocalName }
    foreach ($entry in $section.Entries.ChildNodes) {
        if ($entry.LocalName -ne "x-item") { continue }
        $text = $entry.InnerText.Trim()
        # Format: /database/path/{GUID}/invariant/0
        if ($text -match '^/([^/]+)/(.+)/\{([0-9A-Fa-f-]+)\}/invariant/\d+$') {
            $db = $Matches[1]
            $itemPath = "/" + $Matches[2]
            $guid = "{" + $Matches[3].ToUpper() + "}"

            if ($xmlItems.ContainsKey($guid)) {
                $xmlDuplicates += "Duplicate XML entry: $guid ($text)"
            }

            $xmlItems[$guid] = [PSCustomObject]@{
                Guid     = $guid
                Path     = $itemPath
                Database = $db
                Raw      = $text
                Section  = $sectionName
            }
        }
    }
}

# Parse accounts sections
$accountsSections = @($xml.project.Sources.ChildNodes | Where-Object { $_.LocalName -eq "accounts" })
foreach ($section in $accountsSections) {
    foreach ($entry in $section.Entries.ChildNodes) {
        if ($entry.LocalName -ne "x-item") { continue }
        $text = $entry.InnerText.Trim()
        if ($text -match '^roles:(.+)$') {
            $xmlRoles += $Matches[1]
        }
        elseif ($text -match '^users:(.+)$') {
            $xmlUsers += $Matches[1]
        }
    }
}

Write-Host "  XML item entries: $($xmlItems.Count)"
Write-Host "  XML roles: $($xmlRoles.Count), XML users: $($xmlUsers.Count)"

# ── Step 4: Cross-reference and report ──────────────────────────────────────

Write-Host "`n=== Step 4: Cross-reference results ===" -ForegroundColor Cyan

# Build set of root item paths from module.json includes (these are expected to be absent from XML)
$rootItemPaths = @{}
foreach ($folderName in $folderToPath.Keys) {
    $db = $folderToDatabase[$folderName]
    $path = $folderToPath[$folderName]
    $rootItemPaths["$db|$path"] = $true
}

$missingFromPackage = @()
$missingRootItems = @()
$missingFromYaml = @()
$mismatches = @()
$passed = $true

# Items in YAML but missing from package XML
foreach ($guid in $yamlItems.Keys) {
    if (-not $xmlItems.ContainsKey($guid)) {
        $item = $yamlItems[$guid]
        $key = "$($item.Database)|$($item.Path)"
        if ($rootItemPaths.ContainsKey($key)) {
            $missingRootItems += $item
        }
        else {
            $missingFromPackage += $item
        }
    }
    else {
        # Check path/database mismatches
        $yamlItem = $yamlItems[$guid]
        $xmlItem = $xmlItems[$guid]

        if ($yamlItem.Database -ne $xmlItem.Database) {
            $mismatches += "Database mismatch for $guid : YAML=$($yamlItem.Database) XML=$($xmlItem.Database) Path=$($yamlItem.Path)"
        }

        if ($yamlItem.Path -and $xmlItem.Path -and $yamlItem.Path -ne $xmlItem.Path) {
            $mismatches += "Path mismatch for $guid : YAML=$($yamlItem.Path) XML=$($xmlItem.Path)"
        }
    }
}

# Items in package XML but missing from YAML
foreach ($guid in $xmlItems.Keys) {
    if (-not $yamlItems.ContainsKey($guid)) {
        $missingFromYaml += $xmlItems[$guid]
    }
}

# ── Report ──────────────────────────────────────────────────────────────────

Write-Host "`n=== Verification Report ===" -ForegroundColor Cyan

# Root items (expected to be absent - they're container items managed by include paths)
if ($missingRootItems.Count -gt 0) {
    Write-Host "`n  INFO: $($missingRootItems.Count) root include items not in package XML (expected)" -ForegroundColor Gray
    foreach ($item in $missingRootItems | Sort-Object Database, Path) {
        Write-Host "    $($item.Database):$($item.Path) $($item.Guid)" -ForegroundColor Gray
    }
}

# Missing from package (non-root items - these are real failures)
if ($missingFromPackage.Count -gt 0) {
    $passed = $false
    Write-Host "`n  FAIL: $($missingFromPackage.Count) YAML items missing from package XML" -ForegroundColor Red
    foreach ($item in $missingFromPackage | Sort-Object Database, Path) {
        Write-Host "    $($item.Database):$($item.Path) $($item.Guid)" -ForegroundColor Red
    }
}
else {
    Write-Host "`n  PASS: All non-root YAML items found in package XML" -ForegroundColor Green
}

# Missing from YAML (informational - may be expected)
if ($missingFromYaml.Count -gt 0) {
    Write-Host "`n  INFO: $($missingFromYaml.Count) package XML items not in YAML serialization" -ForegroundColor Yellow
    foreach ($item in $missingFromYaml | Sort-Object Database, Path) {
        Write-Host "    $($item.Database):$($item.Path) $($item.Guid)" -ForegroundColor Yellow
    }
}
else {
    Write-Host "`n  PASS: No extra items in package XML" -ForegroundColor Green
}

# Path/database mismatches
if ($mismatches.Count -gt 0) {
    $passed = $false
    Write-Host "`n  FAIL: $($mismatches.Count) path/database mismatches" -ForegroundColor Red
    foreach ($m in $mismatches) { Write-Host "    $m" -ForegroundColor Red }
}
else {
    Write-Host "`n  PASS: All paths and databases match" -ForegroundColor Green
}

# Duplicate XML entries
if ($xmlDuplicates.Count -gt 0) {
    Write-Host "`n  WARNING: $($xmlDuplicates.Count) duplicate entries in package XML" -ForegroundColor Yellow
    foreach ($d in $xmlDuplicates) { Write-Host "    $d" -ForegroundColor Yellow }
}

# Roles verification
$missingRoles = @($expectedRoles | Where-Object { $xmlRoles -notcontains $_ })
$extraRoles = @($xmlRoles | Where-Object { $expectedRoles -notcontains $_ })
if ($missingRoles.Count -gt 0) {
    $passed = $false
    Write-Host "`n  FAIL: Missing roles in package: $($missingRoles -join ', ')" -ForegroundColor Red
}
elseif ($extraRoles.Count -gt 0) {
    Write-Host "`n  INFO: Extra roles in package: $($extraRoles -join ', ')" -ForegroundColor Yellow
}
else {
    Write-Host "`n  PASS: Roles match" -ForegroundColor Green
}

# Users verification
$missingUsers = @($expectedUsers | Where-Object { $xmlUsers -notcontains $_ })
$extraUsers = @($xmlUsers | Where-Object { $expectedUsers -notcontains $_ })
if ($missingUsers.Count -gt 0) {
    $passed = $false
    Write-Host "`n  FAIL: Missing users in package: $($missingUsers -join ', ')" -ForegroundColor Red
}
elseif ($extraUsers.Count -gt 0) {
    Write-Host "`n  INFO: Extra users in package: $($extraUsers -join ', ')" -ForegroundColor Yellow
}
else {
    Write-Host "`n  PASS: Users match" -ForegroundColor Green
}

# Summary
Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "  YAML items:              $($yamlItems.Count)"
Write-Host "  Package XML items:       $($xmlItems.Count)"
Write-Host "  Root items (expected):   $($missingRootItems.Count)"
Write-Host "  Missing from package:    $($missingFromPackage.Count)"
Write-Host "  Missing from YAML:       $($missingFromYaml.Count)"
Write-Host "  Path/DB mismatches:      $($mismatches.Count)"
Write-Host "  Duplicate XML entries:   $($xmlDuplicates.Count)"
Write-Host "  Roles: expected=$($expectedRoles.Count) actual=$($xmlRoles.Count)"
Write-Host "  Users: expected=$($expectedUsers.Count) actual=$($xmlUsers.Count)"

if ($passed) {
    Write-Host "`n  RESULT: PASS" -ForegroundColor Green
    exit 0
}
else {
    Write-Host "`n  RESULT: FAIL" -ForegroundColor Red
    exit 1
}
