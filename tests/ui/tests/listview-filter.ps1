# UI-test-only: create a few items under spe-test and show them in a ListView.
$root = "master:\content\spe-test"
if (-not (Test-Path $root)) {
    New-Item -Path "master:\content" -Name "spe-test" -ItemType "Common/Folder" | Out-Null
}
foreach ($name in "Layout One", "Layout Two", "Other One") {
    if (-not (Test-Path "$root\$name")) {
        New-Item -Path $root -Name $name -ItemType "Common/Folder" | Out-Null
    }
}
Get-ChildItem -Path $root -WithParent | Show-ListView -ViewName "DataManagement"
