param(
    [Parameter()]
    [string]$protocolHost = "http://spe.dev.local"
)

Import-Module -Name SPE -Force

if(!$protocolHost){
    $protocolHost = "http://spe.dev.local"
}

$count = 0
Describe "Invoke multiple sessions" {
    It "Should return 'admin' user 20 consecutive times without errors" {
        do {
            $session = New-ScriptSession -Username "admin" -Password "b" -ConnectionUri $protocolHost
        
            $script1 = {
                [Sitecore.Security.Accounts.User]$user = Get-User -Identity admin
                $user
            }
        
            $user = Invoke-RemoteScript -ScriptBlock $script1 -Session $session

            $user | Should BeOftype System.Management.Automation.PSObject
            $user.Name | Should Be sitecore\admin

            Stop-ScriptSession -Session $session

            $count++
        } while($count -lt 20)
    }
}