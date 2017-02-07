param(
    [Parameter()]
    [string]$protocolHost = "http://spe.dev.local"
)

Import-Module -Name SPE -Force

if(!$protocolHost){
    $protocolHost = "http://spe.dev.local"
}

Describe "Invoke remote scripts with RemotingAutomation" {
    BeforeEach {
        $session = New-ScriptSession -Username "sitecore\admin" -Password "b" -ConnectionUri $protocolHost
    }
    AfterEach {
        Stop-ScriptSession -Session $session
    }
    Context "Remote Script" {
        It "returns when no parameters passed" {
            $expected = $env:COMPUTERNAME
            $actual = Invoke-RemoteScript -Session $session -ScriptBlock { $env:computername }
            $actual | Should Be $expected
        }
        It "returns when the '`$Using' variable passed" {
            $expected = "sitecore\admin"
            $actual = Invoke-RemoteScript -Session $session -ScriptBlock { Get-User -Id $using:expected | Select-Object -ExpandProperty Name }
            $actual | Should Be $expected
        }
        It "Sitecore Kittens should not exist" { 
            Invoke-RemoteScript -Session $session -ScriptBlock { $myints = @(1,1,2,3,5,8,13); $myints } | Should Throw
        }
    }
}