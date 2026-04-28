# Unit tests for RemotingItemEventHandler.SanitizeAllowedCommands.
#
# Pure-function helper that cleans the multi-line AllowedCommands field on
# RemotingPolicy save. Lenient sanitize semantics (per operator decision):
#   - trim each line
#   - drop blank lines
#   - drop lines starting with '#' (comment lines, otherwise saved as no-op
#     allowlist entries that match nothing)
#   - drop lines that don't match the cmdlet-name shape
#     ([Module\]Verb-Noun, ASCII letters/digits/underscores only)
#   - dedup case-insensitive (first occurrence wins, preserving order)
#   - return the cleaned list joined with CRLF for stable Sitecore storage
#
# Lenient on bad shape: silently drop rather than cancel save. Operators
# who type 'Get_Item' (typo) end up with the line removed; the audit log
# captures what was dropped.

Write-Host "`n  [RemotingItemEventHandler: SanitizeAllowedCommands]" -ForegroundColor White

$abstractionsPath = "$PSScriptRoot\..\..\src\Spe.Abstractions\bin\Debug\Spe.Abstractions.dll"
$spePath          = "$PSScriptRoot\..\..\src\Spe\bin\Debug\Spe.dll"

if (-not ((Test-Path $abstractionsPath) -and (Test-Path $spePath))) {
    Skip-Test "All SanitizeAllowedCommands tests" "Build artifacts not found - run 'task build' first"
    return
}

try { Add-Type -Path $abstractionsPath -ErrorAction SilentlyContinue } catch { }
try { Add-Type -Path $spePath          -ErrorAction SilentlyContinue } catch { }

$asm = [Spe.Core.Settings.Authorization.RemotingPolicy].Assembly
$handlerType = $asm.GetType("Spe.Core.Settings.Authorization.RemotingItemEventHandler")
$method = $handlerType.GetMethod("SanitizeAllowedCommands",
    [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Static)

if (-not $method) {
    Skip-Test "All SanitizeAllowedCommands tests" "SanitizeAllowedCommands not found - helper not yet extracted"
    return
}

# Helper signature:
#   public static string SanitizeAllowedCommands(string input)
# NOTE: parameter named $Text deliberately - $Input is a PS automatic
# variable (pipeline iterator) and gets clobbered when used as a param name.
function Sanitize {
    param([string]$Text)
    $invokeArgs = [object[]]::new(1)
    $invokeArgs[0] = $Text
    return $method.Invoke($null, $invokeArgs)
}

# ============================================================
# Empty / null input
# ============================================================
Write-Host "`n  [Empty / null input passes through]" -ForegroundColor White
Assert-Equal (Sanitize $null) "" "null input -> empty string"
Assert-Equal (Sanitize "") "" "empty input -> empty string"
Assert-Equal (Sanitize "   `r`n   ") "" "whitespace-only input -> empty string"

# ============================================================
# Trim per-line + drop blanks
# ============================================================
Write-Host "`n  [Trim and drop blanks]" -ForegroundColor White
$result = Sanitize "  Get-Item  `r`n`r`n  Get-ChildItem`r`n"
$lines = $result -split "`r`n"
Assert-Equal $lines.Count 2 "trim + drop-blanks: 2 lines"
Assert-Equal $lines[0] "Get-Item"      "trim: line 1 has no surrounding whitespace"
Assert-Equal $lines[1] "Get-ChildItem" "trim: line 2 has no surrounding whitespace"

# ============================================================
# Dedup case-insensitive, preserve order
# ============================================================
Write-Host "`n  [Dedup case-insensitive, preserve original order]" -ForegroundColor White
$result = Sanitize "Get-Item`r`nGet-ChildItem`r`nget-item`r`nGET-ITEM"
$lines = $result -split "`r`n"
Assert-Equal $lines.Count 2 "dedup: 2 unique entries"
Assert-Equal $lines[0] "Get-Item"      "dedup: first occurrence preserved with its original casing"
Assert-Equal $lines[1] "Get-ChildItem" "dedup: distinct entries kept in input order"

# ============================================================
# Strip comment lines (#-prefixed)
# ============================================================
Write-Host "`n  [Strip comment lines]" -ForegroundColor White
$result = Sanitize "# read-only allowlist`r`nGet-Item`r`n# audited 2026-04-26`r`nGet-ChildItem"
$lines = $result -split "`r`n"
Assert-Equal $lines.Count 2 "strip-comments: 2 cmdlet lines"
Assert-True ($lines -notcontains "# read-only allowlist") "strip-comments: leading-# line dropped"
Assert-True ($lines -notcontains "# audited 2026-04-26") "strip-comments: mid-list comment dropped"
Assert-Equal $lines[0] "Get-Item" "strip-comments: surrounding cmdlet lines preserved"

$result = Sanitize "  # leading whitespace before hash`r`nGet-Item"
$lines = $result -split "`r`n"
Assert-Equal $lines.Count 1 "strip-comments: comment after trim is still recognised"
Assert-Equal $lines[0] "Get-Item" "strip-comments: only the cmdlet survives"

# ============================================================
# Drop malformed lines (don't match cmdlet shape)
# ============================================================
Write-Host "`n  [Drop malformed lines silently]" -ForegroundColor White
$result = Sanitize "Get-Item`r`nGet_Item`r`n; Remove-Item ;`r`nGet-ChildItem"
$lines = $result -split "`r`n"
Assert-Equal $lines.Count 2 "drop-bad-shape: 2 valid lines"
Assert-True ($lines -notcontains "Get_Item") "drop-bad-shape: underscore-typo dropped"
Assert-True ($lines -notcontains "; Remove-Item ;") "drop-bad-shape: semicolon junk dropped"
Assert-Equal $lines[0] "Get-Item" "drop-bad-shape: valid entries kept in order"
Assert-Equal $lines[1] "Get-ChildItem" "drop-bad-shape: valid entries kept in order"

# Lines that don't have a hyphen at all
$result = Sanitize "GetItem`r`nGet-Item"
$lines = $result -split "`r`n"
Assert-Equal $lines.Count 1 "drop-bad-shape: hyphenless line dropped"
Assert-Equal $lines[0] "Get-Item" "drop-bad-shape: valid line preserved"

# Lines with leading digit
$result = Sanitize "1Get-Item`r`nGet-Item"
$lines = $result -split "`r`n"
Assert-Equal $lines.Count 1 "drop-bad-shape: leading-digit dropped"

# Lines that look like attempts at logic
$result = Sanitize "Get-Item; Remove-Item`r`nGet-ChildItem"
$lines = $result -split "`r`n"
Assert-Equal $lines.Count 1 "drop-bad-shape: chained-statement dropped"

# ============================================================
# Module-qualified forms canonicalized to short form
# ============================================================
Write-Host "`n  [Module qualifier stripped to canonical short form]" -ForegroundColor White
$result = Sanitize "Microsoft.PowerShell.Utility\Write-Verbose`r`nMicrosoft.PowerShell.Core\Out-Null`r`nGet-Item"
$lines = $result -split "`r`n"
Assert-Equal $lines.Count 3 "qualifier-strip: 3 entries"
Assert-True ($lines -contains "Write-Verbose") "qualifier-strip: dotted-module qualifier stripped"
Assert-True ($lines -contains "Out-Null") "qualifier-strip: second qualifier stripped"
Assert-True ($lines -contains "Get-Item") "qualifier-strip: already-short form preserved"
Assert-True ($lines -notcontains "Microsoft.PowerShell.Utility\Write-Verbose") "qualifier-strip: original qualified form NOT preserved"
Assert-True ($lines -notcontains "Microsoft.PowerShell.Core\Out-Null") "qualifier-strip: second original qualified form NOT preserved"

# Cross-form dedup: short and qualified collapse to the short form first occurrence
$result = Sanitize "Get-Item`r`nMicrosoft.PowerShell.Management\Get-Item"
$lines = $result -split "`r`n"
Assert-Equal $lines.Count 1 "qualifier-strip dedup: short + qualified collapse to one entry"
Assert-Equal $lines[0] "Get-Item" "qualifier-strip dedup: short form (first occurrence) wins"

# Reverse order also dedups to short form (qualified seen first, stripped, then short is duplicate)
$result = Sanitize "Microsoft.PowerShell.Management\Get-Item`r`nGet-Item"
$lines = $result -split "`r`n"
Assert-Equal $lines.Count 1 "qualifier-strip dedup: qualified-then-short collapses to one entry"
Assert-Equal $lines[0] "Get-Item" "qualifier-strip dedup: stripped qualified form is the same canonical short form"

# Module qualifier with bogus shape still gets dropped (regex catches it before strip)
$result = Sanitize "Bad..Module\Get-Item`r`nGet-Item"
$lines = $result -split "`r`n"
Assert-Equal $lines.Count 1 "qualifier-strip: consecutive dots in module rejected at shape check"
Assert-Equal $lines[0] "Get-Item" "qualifier-strip: only valid line kept"

# ============================================================
# Combined: real-world messy input
# ============================================================
Write-Host "`n  [Combined real-world messy input]" -ForegroundColor White
$messy = @"
# Read-only policy
Get-Item
  Get-ChildItem

# Pipeline cmdlets
Where-Object
ForEach-Object
where-object

Get_Item
Microsoft.PowerShell.Utility\ConvertTo-Json
"@
$result = Sanitize $messy
$lines = $result -split "`r`n"
Assert-Equal $lines.Count 5 "messy: 5 valid unique cmdlets after sanitize"
Assert-Equal $lines[0] "Get-Item" "messy: first valid entry preserved"
Assert-Equal $lines[1] "Get-ChildItem" "messy: trimmed second entry"
Assert-Equal $lines[2] "Where-Object" "messy: pipeline cmdlet 1"
Assert-Equal $lines[3] "ForEach-Object" "messy: pipeline cmdlet 2"
Assert-Equal $lines[4] "ConvertTo-Json" "messy: module qualifier stripped to short form"
# Case-insensitive dedup is verified by the count assertion above (5 not 6)
# combined with "pipeline cmdlet 1" preserving "Where-Object" casing - we
# can't use -notcontains "where-object" here because PowerShell's array
# -contains operator is case-insensitive by default.
Assert-True ($lines -notcontains "Get_Item") "messy: underscore typo dropped"
Assert-True ($lines -notcontains "# Read-only policy") "messy: comments stripped"
Assert-True ($lines -notcontains "Microsoft.PowerShell.Utility\ConvertTo-Json") "messy: original qualified form NOT preserved"
