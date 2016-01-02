Import-Module -Name SPE -Force

if(!$session) {
    $session = New-ScriptSession -Username "admin" -Password "b" -ConnectionUri "http://console"
}

Test-RemoteConnection -Session $session -Quiet
<#
# Rebuild link databases
$job = Invoke-RemoteScript -Session $session -ScriptBlock {
        "master", "web" | Get-Database | 
            ForEach-Object { 
                [Sitecore.Globals]::LinkDatabase.Rebuild($_)
            }
} -AsJob
Wait-RemoteScriptJob -Session $session -Job $job -Delay 5 -Verbose

# Rebuild search indexes
$job = Invoke-RemoteScript -Session $session -ScriptBlock {
        Rebuild-SearchIndex -Name sitecore_master_index -AsJob
}
Wait-RemoteScriptJob -Session $session -Job $job -Delay 5 -Verbose

$job = Invoke-RemoteScript -Session $session -ScriptBlock {
        Publish-Item -Path "master:\content\home" -Recurse -AsJob
}
Wait-RemoteScriptJob -Session $session -Job $job -Delay 1 -Verbose
#>
Invoke-RemoteScript -Session $session -ScriptBlock {
        [int]$a = 1
        $a -is [Sitecore.Kittens]
}

<#
$jobs = Invoke-RemoteScript -Session $session -ScriptBlock {
    [Sitecore.Jobs.JobManager]::GetJobs()
}
$jobs | Where-Object { $_.Category -eq "publish" -or $_.Category -eq "PublishManager" }
#>