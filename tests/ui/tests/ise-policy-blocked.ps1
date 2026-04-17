# Script body that must be blocked when the ui-test-block-writehost
# policy is active (Write-Host is not in AllowedCommands).
Write-Host "this should be blocked"
