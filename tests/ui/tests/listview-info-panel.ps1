# UI-test-only: show a ListView with an info panel.
@(
    [pscustomobject]@{ Name = "One" }
    [pscustomobject]@{ Name = "Two" }
) | Show-ListView -Property Name -Title "Info panel" -InfoTitle "Info panel title" -InfoDescription "Info panel description that explains what the report shows."
