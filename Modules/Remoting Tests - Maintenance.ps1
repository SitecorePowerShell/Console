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
        (Rebuild-SearchIndex -Name sitecore_master_index -AsJob).Handle.ToString()
}
Wait-RemoteSitecoreJob -Session $session -Id $jobId -Delay 5 -Verbose

# Republish Site
$jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
        (Publish-Item -Path "master:\" -PublishMode Full -Recurse -RepublishAll -AsJob).Handle.ToString()
}
Wait-RemoteSitecoreJob -Session $session -Id $jobId -Delay 2 -Verbose

# Smart Publish Site
$jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
        (Publish-Item -Path "master:\" -PublishMode Smart -Recurse -AsJob).Handle.ToString()
}
Wait-RemoteSitecoreJob -Session $session -Id $jobId -Delay 2 -Verbose

# Smart Publish item with children and related items
$jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
        (Publish-Item -Path "master:\content\home" -PublishMode SingleItem -Recurse -PublishRelatedItems -AsJob).Handle.ToString()
}
Wait-RemoteSitecoreJob -Session $session -Id $jobId -Delay 2 -Verbose

# Incremental Publish Site
$jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
        (Publish-Item -Path "master:\" -PublishMode Incremental -FromDate "03/17/2016" -AsJob).Handle.ToString()
}
Wait-RemoteSitecoreJob -Session $session -Id $jobId -Delay 2 -Verbose

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