# UI-test-only: open a modal ListView over plain objects (no items needed).
1..3 | ForEach-Object { [pscustomobject]@{ Name = "Row $_" } } | Show-ListView -Modal -Property Name -Title "Grip test"
