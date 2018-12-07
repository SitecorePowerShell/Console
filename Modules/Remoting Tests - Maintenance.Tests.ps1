param(
    [Parameter()]
    [string]$protocolHost = "https://spe.dev.local"
)

Import-Module -Name SPE -Force

if(!$protocolHost){
    $protocolHost = "http://spe.dev.local"
}

Describe "Long running server jobs" {
    BeforeEach {
        $session = New-ScriptSession -Username "sitecore\admin" -Password "b" -ConnectionUri $protocolHost
	Test-RemoteConnection -Session $session -Quiet
    }
    AfterEach {
        Stop-ScriptSession -Session $session
    }

    It "Rebuild link databases" {
        $jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
                "master", "web" | Get-Database | 
                    ForEach-Object { 
                        [Sitecore.Globals]::LinkDatabase.Rebuild($_)
                    }
        } -AsJob
        Wait-RemoteScriptSession -Session $session -Id $jobId -Delay 5 -Verbose
    }
    It "Rebuild search indexes" {
        $jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
                (Rebuild-SearchIndex -Name sitecore_master_index -AsJob).Handle.ToString()
        }
        Wait-RemoteSitecoreJob -Session $session -Id $jobId -Delay 5 -Verbose
    }
    It "Republish Site" {
        $jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
                (Publish-Item -Path "master:\" -PublishMode Full -Recurse -RepublishAll -AsJob).Handle.ToString()
        }
        Wait-RemoteSitecoreJob -Session $session -Id $jobId -Delay 2 -Verbose
    }
    It "Smart Publish Site" {
        $jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
                (Publish-Item -Path "master:\" -PublishMode Smart -Recurse -AsJob).Handle.ToString()
        }
        Wait-RemoteSitecoreJob -Session $session -Id $jobId -Delay 2 -Verbose
    }        
    It "Smart Publish item with children and related items" {
        $jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
                (Publish-Item -Path "master:\content\home" -PublishMode SingleItem -Recurse -PublishRelatedItems -AsJob).Handle.ToString()
        }
        Wait-RemoteSitecoreJob -Session $session -Id $jobId -Delay 2 -Verbose
    }
    It "Incremental Publish Site" {
        $jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
                (Publish-Item -Path "master:\" -PublishMode Incremental -FromDate "03/17/2016" -AsJob).Handle.ToString()
        }
        Wait-RemoteSitecoreJob -Session $session -Id $jobId -Delay 2 -Verbose
    }
}