Clear-Host

Import-Module -Name SPE -Force

function Copy-RainbowContent {
    [CmdletBinding()]
    param(
        [string]$Source,
        [string]$Destination,
        [string]$Username,
        [string]$Password,
        [string]$RootId,
        [switch]$Recurse,
        [switch]$Overwrite
    )
    
    $threads = $env:NUMBER_OF_PROCESSORS

    Write-Host "Transfering items from $($Source) to $($Destination)" -ForegroundColor Yellow

    $localSession = New-ScriptSession -user $Username -pass $Password -conn $Source
    $remoteSession = New-ScriptSession -user $Username -pass $Password -conn $Destination

    $sourceScript = {
        param(
            $Session,
            [string]$RootId,
            [bool]$IncludeParent = $true,
            [bool]$IncludeChildren = $false,
            [bool]$RecurseChildren = $false
        )

        $parentId = $RootId
        $serializeParent = $IncludeParent
        $serializeChildren = $IncludeChildren
        $recurseChildren = $RecurseChildren
        Invoke-RemoteScript -ScriptBlock {
            
            $parentItem = Get-Item -Path "master:" -ID $using:parentId
            $isMediaItem = $parentItem.Paths.IsMediaItem

            $parentYaml = $parentItem | ConvertTo-RainbowYaml

            $builder = New-Object System.Text.StringBuilder
            if($using:serializeParent -or $isMediaItem) {
                $builder.AppendLine($parentYaml) > $null
            }

            $children = $parentItem.GetChildren()

            if($using:serializeChildren) {                

                if(!$isMediaItem) {
                    foreach($child in $children) {
                        $childYaml = $child | ConvertTo-RainbowYaml
                        $builder.AppendLine($childYaml) > $null
                    }
                }
            }

            if($using:recurseChildren) {
                $builder.Append("<#split#>") > $null

                $childIds = ($children | Where-Object { $_.HasChildren -or $isMediaItem } | Select-Object -ExpandProperty ID) -join "|"
                $builder.Append($childIds) > $null
            }

            $builder.ToString()
            
        } -Session $Session -Raw
    }

    $destinationScript = {
        param(
            $Session,
            [string]$Yaml,
            [bool]$Overwrite
        )

        $rainbowYaml = $Yaml
        $shouldOverwrite = $Overwrite

        Invoke-RemoteScript -ScriptBlock {
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
            } | ConvertTo-Json

            $oldCacheSize = [regex]::CacheSize
            [regex]::CacheSize = 0
            [GC]::Collect()
            [regex]::CacheSize = $oldCacheSize
        } -Session $Session -Raw
    }

    $watch = [System.Diagnostics.Stopwatch]::StartNew()

    $queue = New-Object System.Collections.Concurrent.ConcurrentQueue[object]
    $pushedLookup = New-Object System.Collections.Generic.HashSet[string]
    $pushParentChildrenLookup = [ordered]@{}
    $pushChildParentLookup = [ordered]@{}
    $pushChildRunspaceLookup = [ordered]@{}

    $serializeParent = $false
    $serializeChildren = $Recurse.IsPresent
    $recurseChildren = $Recurse.IsPresent

    if(!$Overwrite.IsPresent -and $Recurse.IsPresent) {
        $sourceItemIds = Invoke-RemoteScript -Session $localSession -ScriptBlock { 
            $rootItem = Get-Item -Path "master:" -ID $using:rootId

            $itemIds = (@($rootItem) + @($rootItem.Axes.GetDescendants()) | Select-Object -ExpandProperty ID) -join "|"
            $itemIds
        } -Raw

        $destinationItemIds = Invoke-RemoteScript -Session $remoteSession -ScriptBlock {
            $rootItem = Get-Item -Path "master:" -ID $using:rootId

            $itemIds = (@($rootItem) + @($rootItem.Axes.GetDescendants()) | Select-Object -ExpandProperty ID) -join "|"
            $itemIds
        } -Raw

        $referenceIds = $sourceItemIds.Split("|", [System.StringSplitOptions]::RemoveEmptyEntries)
        $differenceIds = $destinationItemIds.Split("|", [System.StringSplitOptions]::RemoveEmptyEntries)

        $queueIds = Compare-Object -ReferenceObject $referenceIds -DifferenceObject $differenceIds | 
            Where-Object { $_.SideIndicator -eq "<=" } | Select-Object -ExpandProperty InputObject

        foreach($queueId in $queueIds) {
            $queue.Enqueue($queueId)
        }

        $serializeParent = $true
        $serializeChildren = $false
        $recurseChildren = $false

        $threads = 1

    } else {
        $queue.Enqueue($rootId)
    }

    $pool = [RunspaceFactory]::CreateRunspacePool(1, $threads)
    $pool.Open()
    $runspaces = [System.Collections.ArrayList]@()

    $count = 0
    while ($runspaces.Count -gt 0 -or $queue.Count -gt 0) {

        if($runspaces.Count -eq 0 -and $queue.Count -gt 0) {
            $itemId = ""
            if($queue.TryDequeue([ref]$itemId) -and ![string]::IsNullOrEmpty($itemId)) {
                Write-Host "[Pull] $($itemId)" -ForegroundColor Green
                $runspace = [PowerShell]::Create()
                $runspace.AddScript($sourceScript) > $null
                $runspace.AddArgument($LocalSession) > $null
                $runspace.AddArgument($itemId) > $null
                $runspace.AddArgument($true) > $null
                $runspace.AddArgument($serializeChildren) > $null
                $runspace.AddArgument($recurseChildren) > $null
                $runspace.RunspacePool = $pool

                $runspaces.Add([PSCustomObject]@{ Pipe = $runspace; Status = $runspace.BeginInvoke(); Id = $itemId; Time = [datetime]::Now; Operation = "Pull" }) > $null
            }
        }

        $currentRunspaces = $runspaces.ToArray()
        foreach($currentRunspace in $currentRunspaces) { 
            if($currentRunspace.Status.IsCompleted) {
                if($queue.Count -gt 0) {
                    $itemId = ""

                    if($queue.TryDequeue([ref]$itemId) -and ![string]::IsNullOrEmpty($itemId)) {
                    
                        Write-Host "[Pull] $($itemId)" -ForegroundColor Green
                        $runspace = [PowerShell]::Create()
                        $runspace.AddScript($sourceScript) > $null
                        $runspace.AddArgument($LocalSession) > $null
                        $runspace.AddArgument($itemId) > $null
                        $runspace.AddArgument($serializeParent) > $null
                        $runspace.AddArgument($serializeChildren) > $null
                        $runspace.AddArgument($recurseChildren) > $null
                        $runspace.RunspacePool = $pool
                    
                        $runspaceItem = [PSCustomObject]@{ Pipe = $runspace; Status = $runspace.BeginInvoke(); Id = $itemId; Time = [datetime]::Now; Operation = "Pull"} 
                        $addToRunspaces = $true
                        if($pushChildParentLookup.Contains($itemId)) {
                            $parentId = $pushChildParentLookup[$itemId]
                            if($pushedLookup.Contains($parentId)) {
                                $pushChildRunspaceLookup[$itemId] = $runspaceItem
                                $addToRunspaces = $false
                            } else {
                                $pushChildParentLookup.Remove($itemId)
                            }
                        }

                        if($addToRunspaces) {
                            $runspaces.Add($runspaceItem) > $null
                        }
                    }
                }
                $response = $currentRunspace.Pipe.EndInvoke($currentRunspace.Status)
                Write-Host "[$($currentRunspace.Operation)] $($currentRunspace.Id) completed" -ForegroundColor Gray
                Write-Host "- Processed in $(([datetime]::Now - $currentRunspace.Time))" -ForegroundColor Gray
                if($currentRunspace.Operation -eq "Push") {
                    if(![string]::IsNullOrEmpty($response)) {
                        $feedback = $response | ConvertFrom-Json
                        Write-Host "- Imported $($feedback.ImportedItems)/$($feedback.TotalItems) items in destination" -ForegroundColor Gray
                    }
                    $pushedLookup.Remove($currentRunspace.Id) > $null

                    if($pushParentChildrenLookup.Contains($currentRunspace.Id)) {
                        $childIds = $pushParentChildrenLookup[$currentRunspace.Id]
                        foreach($childId in $childIds) {
                            $pushChildParentLookup.Remove($childId)                          
                            if($pushChildRunspaceLookup.Contains($childId)) {                                
                                $runspace = $pushChildRunspaceLookup[$childId]
                                $runspaces.Add($runspace) > $null
                                $pushChildRunspaceLookup.Remove($childId)
                            }
                        }

                        $pushParentChildrenLookup.Remove($currentRunspace.Id)
                    }
                }
                if($currentRunspace.Operation -eq "Pull" -and ![string]::IsNullOrEmpty($response)) {
                    $count++
                    $split = $response -split "<#split#>"
             
                    $yaml = $split[0]
                    [bool]$queueChildren = $false
                    if(![string]::IsNullOrEmpty($yaml)) {
                        Write-Host "[Push] $($currentRunspace.Id)" -ForegroundColor Green
                    
                        $runspace = [PowerShell]::Create()
                    
                        $runspace.AddScript($destinationScript) > $null
                        $runspace.AddArgument($remoteSession) > $null
                        $runspace.AddArgument($yaml) > $null
                        $runspace.AddArgument($Overwrite.IsPresent) > $null
                        $runspace.RunspacePool = $pool

                        $runspaces.Add([PSCustomObject]@{ Pipe = $runspace; Status = $runspace.BeginInvoke(); Id = $currentRunspace.Id; Time = [datetime]::Now; Operation = "Push" }) > $null
                        $pushedLookup.Add($currentRunspace.Id) > $null
                        $queueChildren = $true
                    }

                    if(![string]::IsNullOrEmpty($split[1])) {
                              
                        $childIds = $split[1].Split("|", [System.StringSplitOptions]::RemoveEmptyEntries)

                        foreach($childId in $childIds) {
                            Write-Host "- [Pull] Adding $($childId) to queue" -ForegroundColor Gray
                            if(!$Queue.TryAdd($childId)) {
                                Write-Host "Failed to add $($childId)" -ForegroundColor White -BackgroundColor Red
                            }
                        }

                        if($queueChildren) {
                            $pushParentChildrenLookup[$currentRunspace.Id] = $childIds
                            foreach($childId in $childIds) {
                                $pushChildParentLookup[$childId] = $currentRunspace.Id
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

    $watch.Stop()
    $totalSeconds = $watch.ElapsedMilliseconds / 1000

    Write-Host "[Done] Completed transfer in $($totalSeconds) seconds" -ForegroundColor Yellow
}

$copyProps = @{
    Source = "https://spe.dev.local"
    Destination = "http://sc827"
    Username = "admin"
    Password = "b"    
}

# Content
$rootId = "{37D08F47-7113-4AD6-A5EB-0C0B04EF6D05}"

# Migrate a single item only if it's missing
#Copy-RainbowContent @copyProps -RootId $rootId

# Migrate all items only if they are missing
#Copy-RainbowContent @copyProps -RootId $rootId -Recurse

# Migrate a single item and overwrite if it exists
Copy-RainbowContent @copyProps -RootId $rootId -Overwrite

# Migrate all items overwriting if they exist
#Copy-RainbowContent @copyProps -RootId $rootId -Overwrite -Recurse

# Images
$rootId = "{15451229-7534-44EF-815D-D93D6170BFCB}"

#Copy-RainbowContent @copyProps -RootId "{15451229-7534-44EF-815D-D93D6170BFCB}"