#$VerbosePreference = "Continue"

Import-Module -Name SPE -Force
$count = 0
do {
    $session = New-ScriptSession -Username "admin" -Password "b" -ConnectionUri "http://console/"

    $script1 = {
        [Sitecore.Security.Accounts.User]$user = Get-User -Identity admin
        $user
    }

    Invoke-RemoteScript -ScriptBlock $script1 -Session $session
    
    Stop-ScriptSession -Session $session

    $count++
} while($count -lt 1000)