# Generate-TrustedScripts.ps1
# Generates a Sitecore config patch (Spe.TrustedScripts.config) for the
# ScriptTrustRegistry using an explicit allowlist of Sitecore item paths.
#
# Zero-trust model: only scripts listed in -TrustedItemPaths are trusted.
# All other scripts default to Untrusted at runtime (run under the
# caller's constrained language mode and command restrictions).
# New scripts must be explicitly added to the allowlist.
#
# For each trusted script, the tool:
#   - Extracts the item GUID from serialized YAML
#   - Computes a SHA256 content hash of the script body
#   - Parses the PowerShell AST for exported function names
#
# Usage: powershell -File scripts/Generate-TrustedScripts.ps1
#        powershell -File scripts/Generate-TrustedScripts.ps1 -Trust System

[CmdletBinding()]
param(
    [string]$SerializationRoot,

    [string]$OutputPath,

    # Explicit allowlist of Sitecore item paths to trust (zero-trust model).
    # Only scripts whose item path exactly matches an entry are included.
    # New scripts default to Untrusted unless added here.
    [string[]]$TrustedItemPaths = @(
        # Core functions used by remoting scripts via Import-Function
        "/sitecore/system/Modules/PowerShell/Script Library/SPE/Core/Platform/Functions/BaseXlsx"
        "/sitecore/system/Modules/PowerShell/Script Library/SPE/Core/Platform/Functions/Clear-Archive"
        "/sitecore/system/Modules/PowerShell/Script Library/SPE/Core/Platform/Functions/Compress-Archive"
        "/sitecore/system/Modules/PowerShell/Script Library/SPE/Core/Platform/Functions/ConvertTo-Xlsx"
        "/sitecore/system/Modules/PowerShell/Script Library/SPE/Core/Platform/Functions/Expand-Archive"
        "/sitecore/system/Modules/PowerShell/Script Library/SPE/Core/Platform/Functions/Export-Xlsx"
        "/sitecore/system/Modules/PowerShell/Script Library/SPE/Core/Platform/Functions/Get-LockedChildItem"
        "/sitecore/system/Modules/PowerShell/Script Library/SPE/Core/Platform/Functions/Invoke-ApiScript"
        "/sitecore/system/Modules/PowerShell/Script Library/SPE/Core/Platform/Functions/Invoke-SqlCommand"
        "/sitecore/system/Modules/PowerShell/Script Library/SPE/Core/Platform/Functions/New-PackagePostStep"
        "/sitecore/system/Modules/PowerShell/Script Library/SPE/Core/Platform/Functions/Resolve-Error"

        # Web API endpoints executed by restfulv2 service
        "/sitecore/system/Modules/PowerShell/Script Library/SPE/Training/Web API and Remoting/Advanced Web API/Web API/ChildrenAsHtml"
        "/sitecore/system/Modules/PowerShell/Script Library/SPE/Training/Web API and Remoting/Advanced Web API/Web API/ChildrenAsJson"
        "/sitecore/system/Modules/PowerShell/Script Library/SPE/Training/Web API and Remoting/Advanced Web API/Web API/ChildrenAsXml"
        "/sitecore/system/Modules/PowerShell/Script Library/SPE/Training/Web API and Remoting/Advanced Web API/Web API/HomeAndDescendants"
        "/sitecore/system/Modules/PowerShell/Script Library/SPE/Training/Web API and Remoting/Simple Web API/Web API/TrainingWebApi"
    ),

    [ValidateSet("Trusted", "System")]
    [string]$Trust = "Trusted",

    [switch]$AllowTopLevel,

    [ValidateSet("Constrain", "Block", "Warn")]
    [string]$OnHashMismatch = "Constrain"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent (Resolve-Path $MyInvocation.MyCommand.Path) }
if (-not $SerializationRoot) {
    $SerializationRoot = Join-Path $scriptDir "..\serialization\modules\serialization\SPE"
}
if (-not $OutputPath) {
    $OutputPath = Join-Path $scriptDir "..\src\Spe\App_Config\Include\Spe\Spe.TrustedScripts.config"
}

$ScriptTemplateId = "dd22f1b3-bd87-4db2-9e7d-f7a496888d43"
$ScriptFieldId    = "b1a94ff0-6897-47c0-9c51-aa6acb80b1f0"

function Get-YamlScriptItems {
    [CmdletBinding()]
    param([string]$RootPath)

    $yamlFiles = Get-ChildItem -Path $RootPath -Filter "*.yml" -Recurse -File

    foreach ($file in $yamlFiles) {
        $content = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)

        # Quick check: must be a PowerShell Script template
        if ($content -notmatch "Template:\s*[`"']?$ScriptTemplateId[`"']?") {
            continue
        }

        # Extract item ID
        if ($content -notmatch '(?m)^ID:\s*"([^"]+)"') {
            continue
        }
        $itemId = $Matches[1]

        # Extract item path (for the name)
        $itemName = ""
        $rawPath = ""
        if ($content -match '(?m)^Path:\s*(.+)$') {
            $rawPath = $Matches[1].Trim().Trim('"', "'")
            $itemName = $rawPath.Split('/')[-1]
        }

        # Extract script body from the Script field
        $scriptBody = Get-ScriptFieldValue -YamlContent $content
        if ([string]::IsNullOrWhiteSpace($scriptBody)) {
            continue
        }

        [PSCustomObject]@{
            FilePath   = $file.FullName
            ItemId     = $itemId
            ItemName   = $itemName
            ItemPath   = $rawPath
            ScriptBody = $scriptBody
        }
    }
}

function Get-ScriptFieldValue {
    [CmdletBinding()]
    param([string]$YamlContent)

    # Find the Script field by its ID in SharedFields
    # Pattern: field entry with our Script field ID followed by Value: |
    $lines = $YamlContent -split "`n"
    $inScriptField = $false
    $foundValue = $false
    $bodyLines = [System.Collections.Generic.List[string]]::new()
    $valueIndent = -1

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i] -replace "`r$", ""

        if ($line -match "^\s*-\s*ID:\s*[`"']?$ScriptFieldId[`"']?") {
            $inScriptField = $true
            continue
        }

        if ($inScriptField -and -not $foundValue) {
            if ($line -match '^\s*Value:\s*\|') {
                $foundValue = $true
                continue
            }
            # If we hit another field key without finding Value, skip
            if ($line -match '^\s*-\s*ID:') {
                $inScriptField = $false
                continue
            }
            continue
        }

        if ($foundValue) {
            # Determine the indentation of the block scalar content
            if ($valueIndent -lt 0) {
                if ($line -match '^(\s+)\S') {
                    $valueIndent = $Matches[1].Length
                } elseif ($line -match '^\S') {
                    # No indented content, empty script
                    break
                } else {
                    # Blank line before content starts - include it
                    $bodyLines.Add("")
                    continue
                }
            }

            # Check if we've exited the block scalar (less or equal indent to parent)
            if ($line -match '^\S') {
                break
            }
            if ($line -match '^(\s+)\S' -and $Matches[1].Length -lt $valueIndent) {
                break
            }

            # Strip the block scalar indentation
            if ($line.Length -ge $valueIndent) {
                $bodyLines.Add($line.Substring($valueIndent))
            } else {
                $bodyLines.Add($line.TrimStart())
            }
        }
    }

    if ($bodyLines.Count -eq 0) {
        return $null
    }

    # Trim trailing empty lines
    while ($bodyLines.Count -gt 0 -and [string]::IsNullOrWhiteSpace($bodyLines[$bodyLines.Count - 1])) {
        $bodyLines.RemoveAt($bodyLines.Count - 1)
    }

    return ($bodyLines -join "`n")
}

function Get-ExportedFunctions {
    [CmdletBinding()]
    param([string]$ScriptBody)

    $functions = [System.Collections.Generic.List[string]]::new()

    try {
        $tokens = $null
        $errors = $null
        $ast = [System.Management.Automation.Language.Parser]::ParseInput(
            $ScriptBody, [ref]$tokens, [ref]$errors)

        $functionDefs = $ast.FindAll({
            param($node)
            $node -is [System.Management.Automation.Language.FunctionDefinitionAst]
        }, $false)  # $false = don't search nested (top-level only)

        foreach ($funcDef in $functionDefs) {
            $functions.Add($funcDef.Name)
        }
    }
    catch {
        Write-Warning "AST parse failed: $_"
    }

    return $functions
}

function Get-ContentHash {
    [CmdletBinding()]
    param([string]$ScriptBody)

    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($ScriptBody)
        $hashBytes = $sha256.ComputeHash($bytes)
        $hex = [System.BitConverter]::ToString($hashBytes).Replace("-", "").ToLowerInvariant()
        return "sha256:$hex"
    }
    finally {
        $sha256.Dispose()
    }
}

function New-TrustedScriptsConfig {
    [CmdletBinding()]
    param(
        [PSCustomObject[]]$Entries,
        [string]$TrustLevel,
        [bool]$AllowTopLevelFlag,
        [string]$HashMismatchAction
    )

    $xml = [xml]::new()
    $xml.PreserveWhitespace = $false

    $decl = $xml.CreateXmlDeclaration("1.0", "utf-8", $null)
    $xml.AppendChild($decl) | Out-Null

    # <configuration xmlns:patch="...">
    $configEl = $xml.CreateElement("configuration")
    $configEl.SetAttribute("xmlns:patch", "http://www.sitecore.net/xmlconfig/")
    $configEl.SetAttribute("xmlns:set", "http://www.sitecore.net/xmlconfig/set/")
    $xml.AppendChild($configEl) | Out-Null

    #   <sitecore>
    $sitecoreEl = $xml.CreateElement("sitecore")
    $configEl.AppendChild($sitecoreEl) | Out-Null

    #     <powershell>
    $psEl = $xml.CreateElement("powershell")
    $sitecoreEl.AppendChild($psEl) | Out-Null

    #       <trustedScripts>
    $trustEl = $xml.CreateElement("trustedScripts")
    $psEl.AppendChild($trustEl) | Out-Null

    $sortedEntries = $Entries | Sort-Object -Property ItemName

    foreach ($entry in $sortedEntries) {
        $comment = $xml.CreateComment(" $($entry.ItemPath) ")
        $trustEl.AppendChild($comment) | Out-Null

        $scriptEl = $xml.CreateElement("script")
        $scriptEl.SetAttribute("name", $entry.ItemName)
        $scriptEl.SetAttribute("itemId", "{$($entry.ItemId)}")
        $scriptEl.SetAttribute("contentHash", $entry.ContentHash)
        $scriptEl.SetAttribute("trust", $TrustLevel)

        if ($AllowTopLevelFlag) {
            $scriptEl.SetAttribute("allowTopLevel", "true")
        }

        $scriptEl.SetAttribute("onHashMismatch", $HashMismatchAction)

        # Add exports if any functions were found
        if ($entry.Functions.Count -gt 0) {
            $exportsEl = $xml.CreateElement("exports")
            foreach ($funcName in ($entry.Functions | Sort-Object)) {
                $funcEl = $xml.CreateElement("function")
                $funcEl.InnerText = $funcName
                $exportsEl.AppendChild($funcEl) | Out-Null
            }
            $scriptEl.AppendChild($exportsEl) | Out-Null
        }

        $trustEl.AppendChild($scriptEl) | Out-Null
    }

    return $xml
}

# --- Main ---

Write-Host "Scanning for script items in: $SerializationRoot"
Write-Host "Allowlisted paths: $($TrustedItemPaths.Count)"

# Build a case-insensitive lookup set from the allowlist
$allowSet = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase)
foreach ($p in $TrustedItemPaths) { $allowSet.Add($p) | Out-Null }

$allScriptItems = @(Get-YamlScriptItems -RootPath $SerializationRoot)
Write-Host "Found $($allScriptItems.Count) total script items"

# Filter to only explicitly allowlisted scripts
$trustedItems = @($allScriptItems | Where-Object { $allowSet.Contains($_.ItemPath) })
$skippedCount = $allScriptItems.Count - $trustedItems.Count
Write-Host "Matched $($trustedItems.Count) scripts from allowlist ($skippedCount untrusted)"

# Warn about allowlisted paths that weren't found
$foundPaths = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase)
foreach ($item in $trustedItems) { $foundPaths.Add($item.ItemPath) | Out-Null }
foreach ($p in $TrustedItemPaths) {
    if (-not $foundPaths.Contains($p)) {
        Write-Warning "Allowlisted path not found in YAML: $p"
    }
}

if ($trustedItems.Count -eq 0) {
    Write-Warning "No script items matched the allowlist."
    exit 1
}

$entries = [System.Collections.Generic.List[PSCustomObject]]::new()
$totalFunctions = 0

foreach ($item in $trustedItems) {
    $hash = Get-ContentHash -ScriptBody $item.ScriptBody
    $functions = @(Get-ExportedFunctions -ScriptBody $item.ScriptBody)
    $totalFunctions += $functions.Count

    $entries.Add([PSCustomObject]@{
        ItemId      = $item.ItemId
        ItemName    = $item.ItemName
        ItemPath    = $item.ItemPath
        ContentHash = $hash
        Functions   = $functions
    })
}

Write-Host "Extracted $totalFunctions function exports across $($entries.Count) scripts"

$xml = New-TrustedScriptsConfig `
    -Entries $entries.ToArray() `
    -TrustLevel $Trust `
    -AllowTopLevelFlag $AllowTopLevel.IsPresent `
    -HashMismatchAction $OnHashMismatch

# Write with consistent formatting
$settings = [System.Xml.XmlWriterSettings]::new()
$settings.Indent = $true
$settings.IndentChars = "    "
$settings.NewLineChars = "`r`n"
$settings.NewLineHandling = [System.Xml.NewLineHandling]::Replace
$settings.Encoding = [System.Text.UTF8Encoding]::new($true)  # BOM

$outputDir = Split-Path $OutputPath -Parent
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

$writer = [System.Xml.XmlWriter]::Create($OutputPath, $settings)
try {
    $xml.WriteTo($writer)
}
finally {
    $writer.Dispose()
}

Write-Host "Generated: $OutputPath"
Write-Host "  Trusted scripts: $($entries.Count)"
Write-Host "  Function exports: $totalFunctions"
Write-Host "  Excluded (Untrusted): $skippedCount"
