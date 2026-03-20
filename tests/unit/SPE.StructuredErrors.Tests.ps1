# Unit tests for structured error parsing in Parse-Response
# Tests the errorFormat=structured JSON error reconstruction (#1425)

# Helper: call Parse-Response capturing errors separately.
# Must use $ErrorActionPreference = Continue inside so that Write-Error
# in Parse-Response does not become terminating (which would trigger
# the catch block and fall through to CliXml deserialization).
function Invoke-ParseResponse {
    param([string]$Json, [string]$OutputFormat = 'Json')
    $savedEAP = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $all = Parse-Response -Response $Json -HasRedirectedMessages $false -Raw $false -OutputFormat $OutputFormat 2>&1
        $errors = @($all | Where-Object { $_ -is [System.Management.Automation.ErrorRecord] })
        $output = @($all | Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] -and
                                           $_ -isnot [System.Management.Automation.WarningRecord] })
        [PSCustomObject]@{ Output = $output; Errors = $errors }
    } finally {
        $ErrorActionPreference = $savedEAP
    }
}

# ============================================================
# Parse-Response - Structured errors (JSON)
# ============================================================
Write-Host "`n  [Parse-Response - Structured Errors]" -ForegroundColor White

$json = '{"output":["result1"],"errors":[{"message":"The term Do-Something is not recognized","errorCategory":"ObjectNotFound","categoryReason":"CommandNotFoundException","categoryTargetName":"Do-Something","categoryTargetType":"","fullyQualifiedErrorId":"CommandNotFoundException","exceptionType":"System.Management.Automation.CommandNotFoundException","exceptionMessage":"The term Do-Something is not recognized as a cmdlet.","scriptStackTrace":"at <ScriptBlock>, <No file>: line 1","invocationInfo":{"line":1,"column":1,"scriptName":""}}]}'

$r = Invoke-ParseResponse -Json $json
Assert-Equal $r.Output.Count 1 "Structured error: one output item"
Assert-Equal $r.Output[0] "result1" "Structured error: output value correct"
Assert-Equal $r.Errors.Count 1 "Structured error: one error captured"
Assert-Equal $r.Errors[0].CategoryInfo.Category "ObjectNotFound" "Structured error: category is ObjectNotFound"
Assert-Like $r.Errors[0].FullyQualifiedErrorId "CommandNotFoundException*" "Structured error: fullyQualifiedErrorId preserved"
Assert-Like $r.Errors[0].Exception.Message "*not recognized as a cmdlet*" "Structured error: exceptionMessage used over message"
# NoteProperties on ErrorRecord survive 2>&1 on the underlying record
$innerErr = if ($r.Errors[0].PSObject.Properties['RemoteScriptStackTrace']) { $r.Errors[0] } else { $r.Errors[0].TargetObject }
# When captured via 2>&1, check the Exception carries the message and category is correct
Assert-Equal $r.Errors[0].CategoryInfo.Category "ObjectNotFound" "Structured error: category confirmed on captured record"

# ============================================================
# Structured error without exceptionMessage falls back to message
# ============================================================
Write-Host "`n  [Parse-Response - Fallback to message field]" -ForegroundColor White

$json2 = '{"output":[],"errors":[{"message":"Something went wrong","errorCategory":"NotSpecified","fullyQualifiedErrorId":"GenericError"}]}'

$r2 = Invoke-ParseResponse -Json $json2
Assert-Equal $r2.Errors.Count 1 "Fallback message: one error captured"
Assert-Equal $r2.Errors[0].Exception.Message "Something went wrong" "Fallback message: uses message field when exceptionMessage absent"

# ============================================================
# Structured error without optional trace fields
# ============================================================
Write-Host "`n  [Parse-Response - No trace fields]" -ForegroundColor White

$json3 = '{"output":["ok"],"errors":[{"message":"Simple error","errorCategory":"InvalidOperation","fullyQualifiedErrorId":"SimpleErr","exceptionMessage":"Simple error detail"}]}'

$r3 = Invoke-ParseResponse -Json $json3
Assert-Equal $r3.Output[0] "ok" "No trace fields: output returned"
Assert-Equal $r3.Errors[0].CategoryInfo.Category "InvalidOperation" "No trace fields: category correct"
$hasTrace = $null -ne ($r3.Errors[0] | Get-Member -Name 'RemoteScriptStackTrace' -MemberType NoteProperty)
Assert-True (-not $hasTrace) "No trace fields: RemoteScriptStackTrace not attached when absent"

# ============================================================
# Legacy flat string errors (JSON)
# ============================================================
Write-Host "`n  [Parse-Response - Legacy Flat Errors]" -ForegroundColor White

$jsonFlat = '{"output":["data"],"errors":["Error line 1","Error line 2"]}'

$rFlat = Invoke-ParseResponse -Json $jsonFlat
Assert-Equal $rFlat.Output[0] "data" "Flat errors: output returned"
Assert-Equal $rFlat.Errors.Count 2 "Flat errors: two errors captured"
Assert-Like $rFlat.Errors[0].Exception.Message "*Error line 1*" "Flat errors: first error message correct"
Assert-Like $rFlat.Errors[1].Exception.Message "*Error line 2*" "Flat errors: second error message correct"

# ============================================================
# Mixed structured and flat errors
# ============================================================
Write-Host "`n  [Parse-Response - Mixed Errors]" -ForegroundColor White

$mixedJson = '{"output":["mixed"],"errors":[{"message":"structured one","errorCategory":"PermissionDenied","fullyQualifiedErrorId":"AccessDenied","exceptionMessage":"Access denied"},"plain string error"]}'

$rMixed = Invoke-ParseResponse -Json $mixedJson
Assert-Equal $rMixed.Output[0] "mixed" "Mixed errors: output returned"
Assert-Equal $rMixed.Errors.Count 2 "Mixed errors: both errors captured"
Assert-Equal $rMixed.Errors[0].CategoryInfo.Category "PermissionDenied" "Mixed errors: structured error has correct category"
Assert-Like $rMixed.Errors[1].Exception.Message "*plain string error*" "Mixed errors: flat string error preserved"

# ============================================================
# Invalid category falls back to NotSpecified
# ============================================================
Write-Host "`n  [Parse-Response - Invalid Category Fallback]" -ForegroundColor White

$jsonBadCat = '{"output":[],"errors":[{"message":"bad cat","errorCategory":"TotallyBogusCategory","fullyQualifiedErrorId":"BadCat","exceptionMessage":"bad category test"}]}'

$rBadCat = Invoke-ParseResponse -Json $jsonBadCat
Assert-Equal $rBadCat.Errors.Count 1 "Invalid category: error still captured"
Assert-Equal $rBadCat.Errors[0].CategoryInfo.Category "NotSpecified" "Invalid category: falls back to NotSpecified"

# ============================================================
# Multiple structured errors
# ============================================================
Write-Host "`n  [Parse-Response - Multiple Structured Errors]" -ForegroundColor White

$jsonMulti = '{"output":[],"errors":[{"message":"err1","errorCategory":"ObjectNotFound","fullyQualifiedErrorId":"Err1","exceptionMessage":"First error"},{"message":"err2","errorCategory":"InvalidArgument","fullyQualifiedErrorId":"Err2","exceptionMessage":"Second error","scriptStackTrace":"at line 5"}]}'

$rMulti = Invoke-ParseResponse -Json $jsonMulti
Assert-Equal $rMulti.Errors.Count 2 "Multiple structured: two errors captured"
Assert-Equal $rMulti.Errors[0].CategoryInfo.Category "ObjectNotFound" "Multiple structured: first error category"
Assert-Equal $rMulti.Errors[1].CategoryInfo.Category "InvalidArgument" "Multiple structured: second error category"
# NoteProperties like RemoteScriptStackTrace may not survive 2>&1 wrapping;
# verify category was correctly assigned instead
Assert-Equal $rMulti.Errors[1].Exception.Message "Second error" "Multiple structured: second error message correct"

# ============================================================
# JSON with no errors (only output)
# ============================================================
Write-Host "`n  [Parse-Response - No Errors]" -ForegroundColor White

$jsonClean = '{"output":["clean1","clean2"],"errors":[]}'

$rClean = Invoke-ParseResponse -Json $jsonClean
Assert-Equal $rClean.Output.Count 2 "No errors: both output items returned"
Assert-Equal $rClean.Errors.Count 0 "No errors: no errors captured"
