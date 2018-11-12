$Username = "admin"
$Password = "b"
$Source = "https://spe.dev.local"
$Destination = "http://sc827"

$localSession = New-ScriptSession -user $Username -pass $Password -conn $Source
$remoteSession = New-ScriptSession -user $Username -pass $Password -conn $Destination

$sourceScript = {
    param(
        $Session,
        [string]$RootId,
        [bool]$IncludeParent = $true
    )

    $parentId = $RootId
    $serializeParent = $IncludeParent
    Invoke-RemoteScript -ScriptBlock {
            
        $parentItem = Get-Item -Path "master:" -ID $using:parentId

        $parentYaml = $parentItem | ConvertTo-RainbowYaml

        $children = $parentItem.GetChildren([Sitecore.Collections.ChildListOptions]::IgnoreSecurity -bor [Sitecore.Collections.ChildListOptions]::SkipSorting)
        $childIds = ($children | Where-Object { $_.HasChildren } | Select-Object -ExpandProperty ID) -join "|"

        $builder = New-Object System.Text.StringBuilder
        if($using:serializeParent) {
            $builder.AppendLine($parentYaml) > $null
        }
        foreach($child in $children) {
            $childYaml = $child | ConvertTo-RainbowYaml
            $builder.AppendLine($childYaml) > $null
        }
        $builder.Append("<#split#>") > $null
        $builder.Append($childIds) > $null

        $builder.ToString()
            
    } -Session $Session -Raw
}

$destinationScript = {
    param(
        $Session,
        $Yaml
    )

    $rainbowYaml = $Yaml
    $shouldOverwrite = $false

    $feedback = Invoke-RemoteScript -ScriptBlock {
        $checkExistingItem = !$using:shouldOverwrite
        $rainbowItems = [regex]::Split($using:rainbowYaml, "(?=---)") | 
            Where-Object { ![string]::IsNullOrEmpty($_) } | ConvertFrom-RainbowYaml
        
        $totalItems = $rainbowItems.Count
        $importedItems = 0
        foreach($rainbowItem in $rainbowItems) {
            
            if($checkExistingItem) {
                if((Test-Path -Path "$($rainbowItem.DatabaseName):{$($rainbowItem.Id)}")) { continue }
            }
            $importedItems += 1
            Import-RainbowItem -Item $rainbowItem
        }

        [PSCustomObject]@{
            TotalItems = $totalItems
            ImportedItems = $importedItems
        }

        $oldCacheSize = [regex]::CacheSize
        [regex]::CacheSize = 0
        [GC]::Collect()
        [regex]::CacheSize = $oldCacheSize
    } -Session $Session -Raw
}

$watch = [System.Diagnostics.Stopwatch]::StartNew()
$rootId = "{37D08F47-7113-4AD6-A5EB-0C0B04EF6D05}"

$queue = New-Object System.Collections.Concurrent.ConcurrentQueue[object]

$queue.Enqueue($rootId)
Clear-Host

$threads = 10

$pool = [RunspaceFactory]::CreateRunspacePool(1, $env:NUMBER_OF_PROCESSORS)
#$pool.ApartmentState = "MTA"
$pool.Open()
$runspaces = [System.Collections.ArrayList]@()

$count = 0
while ($runspaces.Count -gt 0 -or $queue.Count -gt 0) {

    if($runspaces.Count -eq 0 -and $queue.Count -gt 0) {
        $parentId = ""
        if($queue.TryDequeue([ref]$parentId) -and ![string]::IsNullOrEmpty($parentId)) {
            Write-Host "Adding runspace for $($parentId)" -ForegroundColor Green
            $runspace = [PowerShell]::Create()
            $runspace.AddScript($sourceScript) > $null
            $runspace.AddArgument($LocalSession) > $null
            $runspace.AddArgument($parentId) > $null
            $runspace.AddArgument($true) > $null
            $runspace.RunspacePool = $pool

            $runspaces.Add([PSCustomObject]@{ Pipe = $runspace; Status = $runspace.BeginInvoke(); Id = $parentId; Time = [datetime]::Now; Operation = "Pull" }) > $null
        }
    }

    $currentRunspaces = $runspaces.ToArray()
    foreach($currentRunspace in $currentRunspaces) { 
        if($currentRunspace.Status.IsCompleted) {
            if($queue.Count -gt 0) {
                $parentId = ""

                if($queue.TryDequeue([ref]$parentId) -and ![string]::IsNullOrEmpty($parentId)) {
                    Write-Host "Adding runspace for $($parentId)" -ForegroundColor Green
                    $runspace = [PowerShell]::Create()
                    $runspace.AddScript($sourceScript) > $null
                    $runspace.AddArgument($LocalSession) > $null
                    $runspace.AddArgument($parentId) > $null
                    $runspace.AddArgument($false) > $null
                    $runspace.RunspacePool = $pool
                    
                    $runspaces.Add([PSCustomObject]@{ 
                        Pipe = $runspace; 
                        Status = $runspace.BeginInvoke(); 
                        Id = $parentId; 
                        Time = [datetime]::Now;
                        Operation = "Pull"
                    }) > $null
                }
            }
            $response = $currentRunspace.Pipe.EndInvoke($currentRunspace.Status)
            Write-Host "Processed $($currentRunspace.Operation) $($currentRunspace.Id) in $(([datetime]::Now - $currentRunspace.Time))"
            if(![string]::IsNullOrEmpty($response)) {
                $count++
                $split = $response -split "<#split#>"
             
                $yaml = $split[0]
                if(![string]::IsNullOrEmpty($yaml)) {
                    Write-Host "Adding runspace to send to destination" -ForegroundColor Green
                    
                    $runspace = [PowerShell]::Create()
                    
                    $runspace.AddScript($destinationScript) > $null
                    $runspace.AddArgument($remoteSession) > $null
                    $runspace.AddArgument($yaml) > $null
                    $runspace.RunspacePool = $pool

                    $runspaces.Add([PSCustomObject]@{ Pipe = $runspace; Status = $runspace.BeginInvoke(); Id = $currentRunspace.Id; Time = [datetime]::Now; Operation = "Push" }) > $null
                }

                if(![string]::IsNullOrEmpty($split[1])) {
                              
                    $childIds = $split[1].Split("|", [System.StringSplitOptions]::RemoveEmptyEntries)

                    foreach($childId in $childIds) {
                        Write-Host "- Enqueue id $($childId)"
                        if(!$Queue.TryAdd($childId)) {
                            Write-Host "Failed to add $($childId)" -ForegroundColor White -BackgroundColor Red
                        }
                    }
                }
            }

            $currentRunspace.Pipe.Dispose()
            $runspaces.Remove($currentRunspace)
        }
    }
}


$pool.Close() 
$pool.Dispose()

Stop-ScriptSession -Session $localSession
Write-Host "All jobs completed!"
$watch.Stop()
$watch.ElapsedMilliseconds / 1000