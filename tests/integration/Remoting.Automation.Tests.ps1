# Remoting Tests - RemotingAutomation
# Converted from Pester to custom assert format

Write-Host "`n  [Remote Script]" -ForegroundColor White

$session = New-ScriptSession -Username "sitecore\admin" -Password "b" -ConnectionUri $protocolHost

$expected = [datetime]::Today
$actual = Invoke-RemoteScript -Session $session -ScriptBlock { [datetime]::Today }
Assert-Equal $actual $expected "returns when no parameters passed"

$expected = "sitecore\admin"
$actual = Invoke-RemoteScript -Session $session -ScriptBlock { Get-User -Id $using:expected | Select-Object -ExpandProperty Name }
Assert-Equal $actual $expected "returns when the `$Using variable passed"

$expected = "abc"
$actual = Invoke-RemoteScript -Session $session -ScriptBlock { $abc = "abc"; $abc } -Raw
Assert-Equal $actual $expected "returns raw results"

$expected = "abc"
$actual = Invoke-RemoteScript -Session $session -ScriptBlock { $abc = "abc"; $abc; } -Raw
Assert-Equal $actual $expected "returns raw results with error"

$expected = @(1,1,2,3,5,8,13)
$actual = Invoke-RemoteScript -Session $session -ScriptBlock { $myints = @(1,1,2,3,5,8,13); $myints }
Assert-Equal $actual $expected "Sitecore Kittens should not exist"

$expected = @(1,1,2,3,5,8,13)
$actual = Invoke-RemoteScript -Session $session -ScriptBlock { Do-Something }
Assert-NotEqual $actual $expected "returns with error"

Stop-ScriptSession -Session $session
