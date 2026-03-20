# Remoting Tests - Multiple Script Sessions
# Converted from Pester to custom assert format

Write-Host "`n  [Invoke multiple sessions]" -ForegroundColor White

if ($global:isConstrainedLanguage) {
    Skip-Test "Multiple sessions with typed user object" "CLM blocks .NET type cast [Sitecore.Security.Accounts.User]"
} else {
    $count = 0
    do {
        $session = New-ScriptSession -Username "admin" -Password "b" -ConnectionUri $protocolHost

        $script1 = {
            [Sitecore.Security.Accounts.User]$user = Get-User -Identity admin
            $user
        }

        $user = Invoke-RemoteScript -ScriptBlock $script1 -Session $session

        Assert-NotNull $user "Session $count - returns user object"
        Assert-Equal $user.Name "sitecore\admin" "Session $count - returns 'admin' user"

        Stop-ScriptSession -Session $session

        $count++
    } while($count -lt 20)

    Assert-Equal $count 20 "Should return 'admin' user 20 consecutive times without errors"
}
