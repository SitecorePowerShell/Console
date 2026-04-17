# UI-test-only: create a remoting policy under Settings\Access\Policies
# that blocks Write-Host. Script is idempotent.
$policiesPath  = "master:\system\Modules\PowerShell\Settings\Access\Policies"
$testName      = "ui-test-block-writehost"
$templatePath  = "Modules/PowerShell Console/Remoting/Remoting Policy"
$fullPath      = "$policiesPath\$testName"

if (Test-Path $fullPath) {
    Remove-Item -Path $fullPath -Permanently -Recurse
}

$policy = New-Item -Path $policiesPath -Name $testName -ItemType $templatePath

$policy.Editing.BeginEdit() | Out-Null
$policy["Full Language"]   = ""
$policy["Allowed Commands"] = "Get-Item`nGet-ChildItem`nOut-Default"
$policy.Editing.EndEdit()  | Out-Null

[Spe.Core.Settings.Authorization.RemotingPolicyManager]::Invalidate()

"SETUP_OK"
