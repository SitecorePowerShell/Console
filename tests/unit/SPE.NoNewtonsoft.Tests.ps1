# Unit tests for #1494: Spe.dll must not depend on Newtonsoft.Json.
#
# SPE never ships Newtonsoft.Json.dll; it relies on whatever the Sitecore
# bin folder carries. Sitecore 9.0 ships 9.0.0.0, 9.1-10.1.3 ship 11.0.0.0,
# so a Spe.dll compiled against 13.0.0.0 fails to bind on those versions.
# The fix is to serialize with System.Web.Script.Serialization (part of the
# .NET Framework) and drop the Newtonsoft reference altogether.

Write-Host "`n  [No Newtonsoft.Json dependency (#1494)]" -ForegroundColor White

$spePath = "$PSScriptRoot\..\..\src\Spe\bin\Debug\Spe.dll"

if (-not (Test-Path $spePath)) {
    Skip-Test "All Newtonsoft dependency tests" "Build artifacts not found - run 'task build' first"
    return
}

try { Add-Type -Path $spePath -ErrorAction SilentlyContinue } catch { }

$asm = [Spe.Core.Host.ScriptExecutionResult].Assembly
$refs = $asm.GetReferencedAssemblies() | ForEach-Object { $_.Name }
Assert-True ($refs -notcontains "Newtonsoft.Json") "Spe.dll does not reference Newtonsoft.Json"

# ScriptExecutionResult.ToString/Parse is the path the issue reproduces with.
# No Newtonsoft.Json.dll is loaded in this process, so this throws
# FileNotFoundException until the dependency is gone.
$result = [Spe.Core.Host.ScriptExecutionResult]::new()
$result.ScriptResult = "ok"
$result.DeferredMessages = @("message:item:refresh", "script:alert('<hi>')")
$json = $result.ToString()
$parsed = [Spe.Core.Host.ScriptExecutionResult]::Parse($json)
Assert-Equal $parsed.ScriptResult "ok" "ScriptResult round-trips"
Assert-Equal $parsed.DeferredMessages.Count 2 "DeferredMessages count round-trips"
Assert-Equal $parsed.DeferredMessages[1] "script:alert('<hi>')" "DeferredMessages content round-trips"
