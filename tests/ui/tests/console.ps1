Clear-Host

# Setup: create a test folder under content
$testRoot = "master:\content\spe-test"
if (Test-Path $testRoot) { Remove-Item $testRoot -Recurse -Force }
New-Item $testRoot -ItemType "Common/Folder" | Out-Null

cd $testRoot
Get-Location | Write-Host -F Cyan

foreach ($i in 1..20) {
    Write-Host "$i " -NoNewline
}

cd master:\content
Get-Location | Write-Host -F Yellow

cd $testRoot
Get-Location | Write-Host -F Green

Write-Host "Done!"

# Cleanup
cd master:\content
Remove-Item $testRoot -Recurse -Force
