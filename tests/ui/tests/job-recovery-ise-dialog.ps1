# UI-test-only: blocks on a Read-Variable dialog so the ISE can be reloaded
# while the script waits for an answer.
$answer = "ise-default"
$result = Read-Variable -Parameters @{ Name = "answer"; Title = "Answer" } -Title "RECOVERY_ISE_DIALOG"
Write-Host "ISE_RESULT=$result ISE_ANSWER=$answer"
