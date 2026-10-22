Clear-Host

1..60 | ForEach-Object { Write-Host "Output line $_" }

for ($i = 1; $i -le 10; $i++) {
    Write-Progress -Activity "Processing items" -Status "Item $i of 10" -PercentComplete ($i * 10)
    Start-Sleep -Milliseconds 500
}
Write-Progress -Activity "Processing items" -Completed

Write-Host "Long output done"
