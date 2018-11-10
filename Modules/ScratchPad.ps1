$Username = "admin"
$Password = "b"
$Source = "https://spe.dev.local"
$Destination = "http://sc827"

$localSession = New-ScriptSession -user $Username -pass $Password -conn $Source
$remoteSession = New-ScriptSession -user $Username -pass $Password -conn $Destination

$emptyScript = {
    param(
        $Session,
        $RootId
    )

    $parentId = $RootId
    Invoke-RemoteScript -ScriptBlock {
        "a<#split#>{541A4139-9118-4D27-8E0A-913F351CA7A2}"           
    } -Session $Session -Raw
}

$sourceScript = {
    param(
        $Session,
        $RootId
    )

    $parentId = $RootId
    Invoke-RemoteScript -ScriptBlock {
            
        $parentItem = Get-Item -Path "master:" -ID $using:parentId

        $parentYaml = $parentItem | ConvertTo-RainbowYaml

        $children = $parentItem.GetChildren()
        $childIds = ($children | Where-Object { $_.HasChildren } | Select-Object -ExpandProperty ID) -join "|"

        $builder = New-Object System.Text.StringBuilder
        $builder.AppendLine($parentYaml) > $null
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
    } -Session $Session
}

$watch = [System.Diagnostics.Stopwatch]::StartNew()
$rootId = "{37D08F47-7113-4AD6-A5EB-0C0B04EF6D05}"

$queue = [System.Collections.Queue]::Synchronized( (New-Object System.Collections.Queue) )
$queue.Enqueue($rootId)
Clear-Host

$threads = 10

$pool = [RunspaceFactory]::CreateRunspacePool(1, [int]$env:NUMBER_OF_PROCESSORS+1)
$pool.ApartmentState = "MTA"
$pool.Open()
$runspaces = [System.Collections.ArrayList]@()

$count = 0
while ($runspaces.Count -gt 0 -or $queue.Count -gt 0) {

    if($runspaces.Count -eq 0 -and $queue.Count -gt 0) {
        $parentId = $Queue.Dequeue()
        if(![string]::IsNullOrEmpty($parentId)) {
            Write-Host "Adding runspace for $($parentId)" -ForegroundColor Green
            $runspace = [PowerShell]::Create()
            $runspace.AddScript($sourceScript) > $null
            $runspace.AddArgument($LocalSession) > $null
            $runspace.AddArgument($parentId) > $null
            $runspace.RunspacePool = $pool

            $runspaces.Add([PSCustomObject]@{ Pipe = $runspace; Status = $runspace.BeginInvoke() }) > $null
        }
    }

    $currentRunspaces = $runspaces.ToArray()
    $currentRunspaces | ForEach-Object { 
        $currentRunspace = $_
        if($currentRunspace.Status.IsCompleted) {
            if($queue.Count -gt 0) {
                $parentId = $Queue.Dequeue()
                if(![string]::IsNullOrEmpty($parentId)) {
                    Write-Host "Adding runspace for $($parentId)" -ForegroundColor Green
                    $runspace = [PowerShell]::Create()
                    $runspace.AddScript($sourceScript) > $null
                    $runspace.AddArgument($LocalSession) > $null
                    $runspace.AddArgument($parentId) > $null
                    $runspace.RunspacePool = $pool

                    $runspaces.Add([PSCustomObject]@{ Pipe = $runspace; Status = $runspace.BeginInvoke() }) > $null
                }
            }
            $response = $currentRunspace.Pipe.EndInvoke($currentRunspace.Status)
            
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

                    $runspaces.Add([PSCustomObject]@{ Pipe = $runspace; Status = $runspace.BeginInvoke() }) > $null
                }

                if(![string]::IsNullOrEmpty($split[1])) {           
                    $childIds = $split[1].Split("|", [System.StringSplitOptions]::RemoveEmptyEntries)

                    foreach($childId in $childIds) {
                        Write-Host "- Enqueue id $($childId)"
                        $Queue.Enqueue($childId)
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