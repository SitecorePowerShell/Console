# Remoting Tests - Maintenance (Long running server jobs)
# Converted from Pester to custom assert format

Write-Host "`n  [Long running server jobs]" -ForegroundColor White

$session = New-ScriptSession -Username "sitecore\admin" -Password "b" -ConnectionUri $protocolHost
Test-RemoteConnection -Session $session -Quiet

$jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
        "master", "web" | Get-Database |
            ForEach-Object {
                [Sitecore.Globals]::LinkDatabase.Rebuild($_)
            }
} -AsJob
Wait-RemoteScriptSession -Session $session -Id $jobId -Delay 5 -Verbose
Assert-True $true "Rebuild link databases"

$jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
        (Rebuild-SearchIndex -Name sitecore_master_index -AsJob).Handle.ToString()
}
Wait-RemoteSitecoreJob -Session $session -Id $jobId -Delay 5 -Verbose
Assert-True $true "Rebuild search indexes"

$jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
        (Publish-Item -Path "master:\" -PublishMode Full -Recurse -RepublishAll -AsJob).Handle.ToString()
}
Wait-RemoteSitecoreJob -Session $session -Id $jobId -Delay 2 -Verbose
Assert-True $true "Republish Site"

$jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
        (Publish-Item -Path "master:\" -PublishMode Smart -Recurse -AsJob).Handle.ToString()
}
Wait-RemoteSitecoreJob -Session $session -Id $jobId -Delay 2 -Verbose
Assert-True $true "Smart Publish Site"

$jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
        (Publish-Item -Path "master:\content\home" -PublishMode SingleItem -Recurse -PublishRelatedItems -AsJob).Handle.ToString()
}
Wait-RemoteSitecoreJob -Session $session -Id $jobId -Delay 2 -Verbose
Assert-True $true "Smart Publish item with children and related items"

$jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
        (Publish-Item -Path "master:\" -PublishMode Incremental -FromDate "03/17/2016" -AsJob).Handle.ToString()
}
Wait-RemoteSitecoreJob -Session $session -Id $jobId -Delay 2 -Verbose
Assert-True $true "Incremental Publish Site"

Stop-ScriptSession -Session $session
