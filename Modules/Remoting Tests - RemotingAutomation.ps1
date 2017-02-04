Import-Module -Name SPE -Force
Import-Module -Name Pester -Force

Describe "Invoke remote scripts with RemotingAutomation" {
    BeforeEach {
        $session = New-ScriptSession -Username "sitecore\admin" -Password "b" -ConnectionUri "https://spe.dev.local"
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
    }
}

