Import-Module -Name SPE -Force

$session = New-ScriptSession -Username "admin" -Password "b" -ConnectionUri "http://console"

Test-RemoteConnection -Session $session -Quiet

# Rebuild link databases
$jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
        "master", "web" | Get-Database | 
            ForEach-Object { 
                [Sitecore.Globals]::LinkDatabase.Rebuild($_)
            }
} -AsJob
Wait-RemoteScriptSession -Session $session -Id $jobId -Delay 5 -Verbose

# Rebuild search indexes
$jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
        Rebuild-SearchIndex -Name sitecore_master_index -AsJob | 
            ForEach-Object { $_.Handle.ToString() }
}
Wait-RemoteSitecoreJob -Session $session -Id $jobId -Delay 5 -Verbose

$jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
        Publish-Item -Path "master:\content\home" -Recurse -AsJob | 
            ForEach-Object { $_.Handle.ToString() }
}
Wait-RemoteScriptJob -Session $session -Id $jobId -Delay 1 -Verbose


Invoke-RemoteScript -Session $session -ScriptBlock {
    $a = 1
    $a

    $a -is [Sitecore.Kittens]
    $a
}

<#
$jobs = Invoke-RemoteScript -Session $session -ScriptBlock {
    [Sitecore.Jobs.JobManager]::GetJobs()
}
$jobs | Where-Object { $_.Category -eq "publish" -or $_.Category -eq "PublishManager" }
#>

Stop-ScriptSession -Session $session