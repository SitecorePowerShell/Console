Clear-Host

$testRoot = "master:\content\spe-test"
try {
    if (Test-Path $testRoot) { Remove-Item $testRoot -Recurse -Force }
    $root = New-Item $testRoot -ItemType "Common/Folder"
    Write-Host "Created: $($root.ItemSource)"
    Write-Host "Template: $((Get-Item 'master:\templates\System\Templates\Template').ItemSource)"
    Write-Host "Initialized: $((Get-Item 'master:\templates\System\Templates\Template' | Initialize-Item).ItemSource)"
}
finally {
    if (Test-Path $testRoot) { Remove-Item $testRoot -Recurse -Force }
    Write-Host "Done!"
}
