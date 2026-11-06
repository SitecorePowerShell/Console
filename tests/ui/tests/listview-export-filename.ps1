# UI-test-only: same items as listview-filter.ps1, with a custom export file name.
# The ':' is invalid in a file name and must come back as '_'.
$root = "master:\content\spe-test"
if (-not (Test-Path $root)) {
    New-Item -Path "master:\content" -Name "spe-test" -ItemType "Common/Folder" | Out-Null
}
foreach ($name in "Layout One", "Layout Two", "Other One") {
    if (-not (Test-Path "$root\$name")) {
        New-Item -Path $root -Name $name -ItemType "Common/Folder" | Out-Null
    }
}
Get-ChildItem -Path $root -WithParent | Show-ListView -ViewName "DataManagement" -ExportFileName "spe:export-{0:yyyyMMdd}"
