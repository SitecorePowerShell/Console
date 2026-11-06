Clear-Host

Write-Host "First line of output"
Write-Host "Second line of output"
Write-Host "Third line of output"

for ($i = 1; $i -le 20; $i++) {
    Write-Progress -Activity "Processing items" -Status "Item $i of 20" -PercentComplete ($i * 5) -CurrentOperation "master:\content\Home\Item $i" -SecondsRemaining (20 - $i)
    Start-Sleep -Milliseconds 600
}
Write-Progress -Activity "Processing items" -Completed

Write-Host "Progress done"
