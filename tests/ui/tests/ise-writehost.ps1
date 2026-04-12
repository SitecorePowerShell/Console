Clear-Host

# Setup
$testRoot = "master:\content\spe-test"
if (Test-Path $testRoot) { Remove-Item $testRoot -Recurse -Force }
New-Item $testRoot -ItemType "Common/Folder" | Out-Null
New-Item "$testRoot\Child1" -ItemType "Common/Folder" | Out-Null
New-Item "$testRoot\Child2" -ItemType "Common/Folder" | Out-Null

Write-Host "Header line" -BackgroundColor Black -ForegroundColor Yellow
Write-Host ""

Write-Host "Section one"
cd $testRoot
Get-ChildItem | Format-Table -Property Name, ID -AutoSize | Out-Default
Write-Host ""

Write-Host "Section two"
Get-Database master | Format-Table -Property Name, Languages -AutoSize
Write-Host ""

Write-Host "Footer line" -BackgroundColor Black -ForegroundColor Yellow

# Cleanup
cd master:\content
Remove-Item $testRoot -Recurse -Force
