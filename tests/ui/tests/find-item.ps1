Clear-Host

$testRoot = "master:\content\spe-test"
try {
    if (Test-Path $testRoot) { Remove-Item $testRoot -Recurse -Force }
    $root = New-Item $testRoot -ItemType "Common/Folder"
    $child = New-Item "$testRoot\find-item-child" -ItemType "Common/Folder"

    # Wait until the master index has picked up the new child
    $deadline = (Get-Date).AddSeconds(60)
    do {
        Initialize-SearchIndexItem -Item $child -Name "sitecore_master_index"
        Start-Sleep -Seconds 2
        $indexed = @(Find-Item -Path $testRoot -Criteria @{Filter = "Equals"; Field = "_group"; Value = $child.ID})
    } while ($indexed.Count -eq 0 -and (Get-Date) -lt $deadline)
    Write-Host "Indexed: $($indexed.Count)"

    $single = @(Find-Item -Path $testRoot -Template "Common/Folder" -LatestVersion | Where-Object ItemId -eq $child.ID)
    Write-Host "Single template: $($single.Count)"

    $multi = @(Find-Item -Path $testRoot -Template "Common/Folder", "Sample/Sample Item" -LatestVersion | Where-Object ItemId -eq $child.ID)
    Write-Host "Multiple templates: $($multi.Count)"

    $none = @(Find-Item -Path $testRoot -Template "Sample/Sample Item" | Where-Object ItemId -eq $child.ID)
    Write-Host "Other template: $($none.Count)"
}
finally {
    if (Test-Path $testRoot) { Remove-Item $testRoot -Recurse -Force }
    Write-Host "Done!"
}
