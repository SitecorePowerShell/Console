# UI-test-only: remove the items created by listview-filter.ps1.
if (Test-Path "master:\content\spe-test") {
    Remove-Item -Path "master:\content\spe-test" -Permanently -Recurse
}
"TEARDOWN_OK"
