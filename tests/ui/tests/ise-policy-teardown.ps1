# UI-test-only: remove the test policy created by ise-policy-setup.ps1.
$policiesPath = "master:\system\Modules\PowerShell\Settings\Access\Policies"
$testName     = "ui-test-block-writehost"
$fullPath     = "$policiesPath\$testName"

if (Test-Path $fullPath) {
    Remove-Item -Path $fullPath -Permanently -Recurse
}

[Spe.Core.Settings.Authorization.RemotingPolicyManager]::Invalidate()

"TEARDOWN_OK"
