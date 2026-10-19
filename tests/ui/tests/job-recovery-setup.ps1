# UI-test-only: script items job-recovery.spec.ts launches through the Runner
# from the Content Editor. Prints NAME=ID lines the spec parses.
$root = "master:\content\spe-test"
if (-not (Test-Path $root)) {
    New-Item -Path "master:\content" -Name "spe-test" -ItemType "Common/Folder" | Out-Null
}
$folder = "$root\JobRecovery"
if (Test-Path $folder) {
    Remove-Item -Path $folder -Recurse -Permanently
}
$folderItem = New-Item -Path $root -Name "JobRecovery" -ItemType "Common/Folder"

$scripts = [ordered]@{
    "RecoveryListView" = @'
1..15 | ForEach-Object {
    Write-Progress -Activity "Working" -Status "Step $_ of 15" -PercentComplete ($_ * 6)
    Start-Sleep -Seconds 1
}
Get-Item -Path "master:\content\spe-test\JobRecovery" |
    Show-ListView -Modal -Title "RECOVERY_LISTVIEW" -Property Name, ID
'@
    "RecoveryDialog" = @'
$answer = "default-answer"
$result = Read-Variable -Parameters @{ Name = "answer"; Title = "Answer" } -Title "RECOVERY_DIALOG"
Write-Host "RESULT=$result ANSWER=$answer"
'@
    "RecoveryShort" = @'
Start-Sleep -Seconds 6
"RECOVERY_SHORT_DONE"
'@
    "RecoveryLong" = @'
Start-Sleep -Seconds 150
"RECOVERY_LONG_DONE"
'@
}

"FOLDER=$($folderItem.ID)"
foreach ($name in $scripts.Keys) {
    $item = New-Item -Path $folder -Name $name -ItemType "Modules/PowerShell Console/PowerShell Script"
    $item.Editing.BeginEdit()
    $item["Script"] = $scripts[$name]
    $item.Editing.EndEdit() | Out-Null
    "$name=$($item.ID)"
}
"SETUP_OK"
