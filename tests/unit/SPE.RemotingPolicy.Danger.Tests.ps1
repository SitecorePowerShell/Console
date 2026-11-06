# Unit tests for RemotingItemEventHandler.IsDangerousAllowlisted.
#
# At save time, RemotingItemEventHandler scans the cleaned Allowed Commands
# list for cmdlets that neutralize the policy (Invoke-Expression, dynamic
# eval; Invoke-Command, runtime file load / remote escape; Import-Module,
# loads unreviewed cmdlets; Set-Alias / New-Alias, alias rebind bypass;
# etc.) and emits a Warn-level audit log entry per match. Save proceeds
# either way - the YAML deserialization path bypasses this handler under
# EventDisabler, so hard-rejection wouldn't be a real boundary.
#
# IsDangerousAllowlisted is the pure-function predicate that drives the
# logging. This tests the registry shape; the actual log emission
# requires Sitecore.Kernel and is exercised manually.

Write-Host "`n  [RemotingItemEventHandler: IsDangerousAllowlisted]" -ForegroundColor White

$abstractionsPath = "$PSScriptRoot\..\..\src\Spe.Abstractions\bin\Debug\Spe.Abstractions.dll"
$spePath          = "$PSScriptRoot\..\..\src\Spe\bin\Debug\Spe.dll"

if (-not ((Test-Path $abstractionsPath) -and (Test-Path $spePath))) {
    Skip-Test "All IsDangerousAllowlisted tests" "Build artifacts not found - run 'task build' first"
    return
}

try { Add-Type -Path $abstractionsPath -ErrorAction SilentlyContinue } catch { }
try { Add-Type -Path $spePath          -ErrorAction SilentlyContinue } catch { }

$asm = [Spe.Core.Settings.Authorization.RemotingPolicy].Assembly
$handlerType = $asm.GetType("Spe.Core.Settings.Authorization.RemotingItemEventHandler")
$method = $handlerType.GetMethod("IsDangerousAllowlisted",
    [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Static)

if (-not $method) {
    Skip-Test "All IsDangerousAllowlisted tests" "IsDangerousAllowlisted not found - helper not yet added"
    return
}

function Check {
    param([string]$Name)
    $invokeArgs = [object[]]::new(1)
    $invokeArgs[0] = $Name
    return $method.Invoke($null, $invokeArgs)
}

# ============================================================
# Each dangerous entry returns its expected reason code
# ============================================================
Write-Host "`n  [Dangerous cmdlets return their reason code]" -ForegroundColor White
Assert-Equal (Check "Invoke-Expression") "dynamicEval"                   "Invoke-Expression -> dynamicEval"
Assert-Equal (Check "Invoke-Command")    "runtimeFileLoadOrRemoteEscape" "Invoke-Command -> runtimeFileLoadOrRemoteEscape"
Assert-Equal (Check "Import-Module")     "loadsUnreviewedCmdlets"        "Import-Module -> loadsUnreviewedCmdlets"
Assert-Equal (Check "Start-Process")     "processEscape"                 "Start-Process -> processEscape"
Assert-Equal (Check "Set-Alias")         "aliasRebindBypass"             "Set-Alias -> aliasRebindBypass"
Assert-Equal (Check "New-Alias")         "aliasRebindBypass"             "New-Alias -> aliasRebindBypass"
Assert-Equal (Check "Add-Type")          "dotNetCompileLoad"             "Add-Type -> dotNetCompileLoad"
Assert-Equal (Check "Update-TypeData")   "typeExtension"                 "Update-TypeData -> typeExtension"
Assert-Equal (Check "Update-FormatData") "formatExtension"               "Update-FormatData -> formatExtension"

# ============================================================
# Case-insensitive lookup
# ============================================================
Write-Host "`n  [Case-insensitive lookup]" -ForegroundColor White
Assert-Equal (Check "invoke-expression") "dynamicEval"          "lowercase Invoke-Expression matches"
Assert-Equal (Check "INVOKE-COMMAND")    "runtimeFileLoadOrRemoteEscape" "uppercase Invoke-Command matches"
Assert-Equal (Check "Import-MODULE")     "loadsUnreviewedCmdlets" "mixed-case Import-Module matches"

# ============================================================
# Benign cmdlets return null
# ============================================================
Write-Host "`n  [Benign cmdlets return null]" -ForegroundColor White
Assert-Equal (Check "Get-Item")        $null "Get-Item -> null (benign)"
Assert-Equal (Check "Get-ChildItem")   $null "Get-ChildItem -> null (benign)"
Assert-Equal (Check "ConvertTo-Json")  $null "ConvertTo-Json -> null (StreamBaseline)"
Assert-Equal (Check "Where-Object")    $null "Where-Object -> null (pipeline filter)"
Assert-Equal (Check "ForEach-Object")  $null "ForEach-Object -> null (pipeline transform)"
Assert-Equal (Check "Write-Host")      $null "Write-Host -> null (StreamBaseline)"

# ============================================================
# Edge cases
# ============================================================
Write-Host "`n  [Edge cases]" -ForegroundColor White
Assert-Equal (Check $null) $null "null input -> null"
Assert-Equal (Check "")    $null "empty string -> null"
Assert-Equal (Check "Definitely-NotACmdlet") $null "unknown cmdlet -> null"

# Module-qualified form returns null - the helper expects canonical short
# form (sanitizer strips qualifiers before this check is applied).
Assert-Equal (Check "Microsoft.PowerShell.Utility\Invoke-Expression") $null "qualified form returns null (sanitizer canonicalizes first)"
