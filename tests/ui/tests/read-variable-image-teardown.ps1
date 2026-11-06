# UI-test-only: remove the media items created by read-variable-image.ps1.
if (Test-Path "master:\media library\spe-test") {
    Remove-Item -Path "master:\media library\spe-test" -Permanently -Recurse
}
"TEARDOWN_OK"
