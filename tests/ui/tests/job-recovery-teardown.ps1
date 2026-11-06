# UI-test-only: remove the items created by job-recovery-setup.ps1.
if (Test-Path "master:\content\spe-test\JobRecovery") {
    Remove-Item -Path "master:\content\spe-test\JobRecovery" -Permanently -Recurse
}
"TEARDOWN_OK"
