Clear-Host

Write-Host "First line of output"

for ($i = 1; $i -le 8; $i++) {
    Write-Progress -Activity "Scanning items" -Status "$($i * 10) items found"
    Start-Sleep -Milliseconds 600
}
Write-Progress -Activity "Scanning items" -Completed

Write-Host "Scan done"
