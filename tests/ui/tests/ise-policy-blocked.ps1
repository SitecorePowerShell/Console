# Script body that must be blocked when the ui-test-block-command
# policy is active (Select-Object is not in AllowedCommands; Write-* cmdlets are always allowed).
"this should be blocked" | Select-Object
