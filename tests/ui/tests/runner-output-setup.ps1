# UI-test-only: script items runner-output.spec.ts launches through the Runner
# from the Content Editor. Prints NAME=ID lines the spec parses.
$root = "master:\content\spe-test"
if (-not (Test-Path $root)) {
    New-Item -Path "master:\content" -Name "spe-test" -ItemType "Common/Folder" | Out-Null
}
$folder = "$root\RunnerOutput"
if (Test-Path $folder) {
    Remove-Item -Path $folder -Recurse -Permanently
}
$folderItem = New-Item -Path $root -Name "RunnerOutput" -ItemType "Common/Folder"

$persistent = "runner-output-persist"
$scripts = [ordered]@{
    "RoSleep"      = 'Start-Sleep -Seconds 5; "sleep-done"'
    "RoStream"     = 'Write-Host "line-1"; Start-Sleep -Seconds 2; Write-Host "line-2"; Start-Sleep -Seconds 10; "stream-done"'
    "RoClean"      = 'Write-Output "hello"'
    "RoProblems"   = 'Write-Warning "warn-a"; Write-Warning "warn-b"; Write-Error "err-c"; "problems-done"'
    "RoAbort"      = '1..150 | ForEach-Object { Start-Sleep -Milliseconds 200; "tick-$_" }'
    "RoFast"       = '1..50 | ForEach-Object { "fast-$_" }'
    "RoPartial"    = 'Write-Host "ab" -NoNewline; Start-Sleep -Seconds 1; Write-Host "cd" -NoNewline'
    "RoClear"      = '"before"; Start-Sleep -Seconds 1; Clear-Host; "after"'
    "RoQuiet"      = '1..30 | ForEach-Object { "quiet-$_"; Start-Sleep -Milliseconds 100 }'
    "RoEarlyLate"  = '"early"; Start-Sleep -Seconds 4; "late"; Start-Sleep -Seconds 4'
    "RoCloseWin"   = '"bye"; Close-Window'
    "RoChild"      = @'
Start-Sleep -Seconds 3
$x = "a"
Read-Variable -Parameters @{ Name = "x"; Title = "X" } -Title "RO_CHILD" | Out-Null
"child-done"
'@
    "RoProgress"   = '1..15 | ForEach-Object { Write-Progress -Activity "Working" -Status "Second $_ of 15" -PercentComplete ($_ * 6) -SecondsRemaining (15 - $_) -CurrentOperation "Step $_ of 15"; Start-Sleep -Seconds 1 }'
    "RoBurst"      = '1..12 | ForEach-Object { "s-$_"; Start-Sleep -Milliseconds 250 }; 1..40 | ForEach-Object { "t-$_" }; Write-Host "tail" -NoNewline'
    "RoTwoErrors"  = 'Write-Error "first-err"; Write-Error "multi-line`nmessage"; "two-errors-done"'
    "RoHostErr"    = '$host.UI.WriteErrorLine("raw-host-error"); "between"; Write-Error "real-err"; "host-err-done"'
    "RoPersistErr" = 'Write-Error "persist-boom"'
    "RoPersistOk"  = '"persist-ok"'
}

"FOLDER=$($folderItem.ID)"
foreach ($name in $scripts.Keys) {
    $item = New-Item -Path $folder -Name $name -ItemType "Modules/PowerShell Console/PowerShell Script"
    $item.Editing.BeginEdit()
    $item["Script"] = $scripts[$name]
    if ($name -like "RoPersist*") {
        $item.Fields[[Sitecore.Data.ID]::Parse("{7FA141B7-7473-44A9-9BD9-2739C51FF8DA}")].Value = $persistent
    }
    $item.Editing.EndEdit() | Out-Null
    "$name=$($item.ID)"
}
"SETUP_OK"
