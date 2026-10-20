# UI-test-only: remove the items created by runner-output-setup.ps1 and the
# persistent session the RoPersist* scripts share.
if (Test-Path "master:\content\spe-test\RunnerOutput") {
    Remove-Item -Path "master:\content\spe-test\RunnerOutput" -Permanently -Recurse
}
Get-ScriptSession -Id "runner-output-persist" -ErrorAction SilentlyContinue | Remove-ScriptSession
"TEARDOWN_OK"
