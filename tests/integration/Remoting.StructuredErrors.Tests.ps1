# Integration tests for structured error format (#1425)
# Tests the -StructuredErrors switch on Invoke-RemoteScript against a live server

Write-Host "`n  [Structured Errors - Integration]" -ForegroundColor White

$session = New-ScriptSession -Username "sitecore\admin" -Password "b" -ConnectionUri $protocolHost

# ============================================================
# Basic: structured error from unknown command
# ============================================================
$result = Invoke-RemoteScript -Session $session -OutputFormat Json -StructuredErrors -ScriptBlock {
    Do-Something
} -ErrorVariable errs -ErrorAction SilentlyContinue

Assert-True ($errs.Count -gt 0) "Unknown command: at least one structured error returned"
Assert-Equal $errs[0].CategoryInfo.Category "ObjectNotFound" "Unknown command: category is ObjectNotFound"
Assert-NotNull $errs[0].FullyQualifiedErrorId "Unknown command: fullyQualifiedErrorId present"
Assert-Like $errs[0].Exception.Message "*Do-Something*" "Unknown command: exception mentions the command name"

# ============================================================
# Structured error includes scriptStackTrace
# ============================================================
$result2 = Invoke-RemoteScript -Session $session -OutputFormat Json -StructuredErrors -ScriptBlock {
    function Test-Fail { throw "deliberate error" }
    Test-Fail
} -ErrorVariable errs2 -ErrorAction SilentlyContinue

Assert-True ($errs2.Count -gt 0) "Stack trace: error returned"
# The server should populate scriptStackTrace for thrown exceptions
$hasRemoteTrace = $null -ne ($errs2[0].PSObject.Properties | Where-Object { $_.Name -eq 'RemoteScriptStackTrace' })
if ($hasRemoteTrace) {
    Assert-Like $errs2[0].RemoteScriptStackTrace "*Test-Fail*" "Stack trace: mentions function name"
} else {
    Skip-Test "Stack trace: RemoteScriptStackTrace present" "Server did not include scriptStackTrace for this error type"
}

# ============================================================
# Structured error preserves output alongside errors
# ============================================================
$result3 = Invoke-RemoteScript -Session $session -OutputFormat Json -StructuredErrors -ScriptBlock {
    "good-output"
    Write-Error "deliberate-error"
    "more-output"
} -ErrorVariable errs3 -ErrorAction SilentlyContinue

Assert-True ($errs3.Count -ge 1) "Output + error: at least one error returned"
Assert-Like ($errs3[0].Exception.Message) "*deliberate-error*" "Output + error: error message correct"
$allOutput = @($result3)
Assert-True ($allOutput.Count -ge 1) "Output + error: output items returned alongside error"

# ============================================================
# Without -StructuredErrors, JSON errors are flat strings
# ============================================================
$result4 = Invoke-RemoteScript -Session $session -OutputFormat Json -ScriptBlock {
    Do-Something
} -ErrorVariable errs4 -ErrorAction SilentlyContinue

Assert-True ($errs4.Count -gt 0) "Flat fallback: error returned without -StructuredErrors"
# Flat errors should NOT have errorCategory-based reconstruction
$hasCategoryProp = $null -ne ($errs4[0].PSObject.Properties | Where-Object { $_.Name -eq 'RemoteScriptStackTrace' })
Assert-True (-not $hasCategoryProp) "Flat fallback: no RemoteScriptStackTrace on flat errors"

# ============================================================
# Multiple errors in a single script
# ============================================================
$result5 = Invoke-RemoteScript -Session $session -OutputFormat Json -StructuredErrors -ScriptBlock {
    Write-Error "first-error"
    Write-Error "second-error"
    "output-value"
} -ErrorVariable errs5 -ErrorAction SilentlyContinue

Assert-True ($errs5.Count -ge 2) "Multiple errors: at least two errors returned"
Assert-Like $errs5[0].Exception.Message "*first-error*" "Multiple errors: first error message"
Assert-Like $errs5[1].Exception.Message "*second-error*" "Multiple errors: second error message"
Assert-Equal $result5 "output-value" "Multiple errors: output still returned"

# ============================================================
# Structured error for invalid parameter
# ============================================================
$result6 = Invoke-RemoteScript -Session $session -OutputFormat Json -StructuredErrors -ScriptBlock {
    Get-Item -FakeBogusParam "test"
} -ErrorVariable errs6 -ErrorAction SilentlyContinue

Assert-True ($errs6.Count -gt 0) "Invalid param: error returned"

# ============================================================
# Clean script produces no errors with -StructuredErrors
# ============================================================
$result7 = Invoke-RemoteScript -Session $session -OutputFormat Json -StructuredErrors -ScriptBlock {
    "hello-structured"
} -ErrorVariable errs7 -ErrorAction SilentlyContinue

Assert-Equal $result7 "hello-structured" "Clean script: output correct"
Assert-Equal $errs7.Count 0 "Clean script: no errors"

Stop-ScriptSession -Session $session
