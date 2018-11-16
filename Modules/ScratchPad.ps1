Clear-Host

Import-Module -Name SPE -Force

function Copy-RainbowContent {
    [CmdletBinding(DefaultParameterSetName="Partial")]
    param(
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [pscustomobject]$SourceSession,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [pscustomobject]$DestinationSession,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]$RootId,

        [Parameter(ParameterSetName='Partial')]
        [switch]$Recurse,

        [switch]$Overwrite,

        [Parameter(ParameterSetName='Everything')]
        [switch]$Everything
    )
    
    $threads = $env:NUMBER_OF_PROCESSORS

    Write-Host "Transfering items from $($Source) to $($Destination)" -ForegroundColor Yellow

    $sourceScript = {
        param(
            $Session,
            [string]$RootId,
            [bool]$IncludeParent = $true,
            [bool]$IncludeChildren = $false,
            [bool]$RecurseChildren = $false,
            [bool]$IncludeEverything = $false
        )

        $parentId = $RootId
        $serializeParent = $IncludeParent
        $serializeChildren = $IncludeChildren
        $recurseChildren = $RecurseChildren

        $scriptEverything = {
            $parentItem = Get-Item -Path "master:" -ID $using:parentId
            $childItems = $parentItem.Axes.GetDescendants()

            $builder = New-Object System.Text.StringBuilder
            $parentYaml = $parentItem | ConvertTo-RainbowYaml
            $builder.AppendLine($parentYaml) > $null
            foreach($childItem in $childItems) {
                $childYaml = $childItem | ConvertTo-RainbowYaml
                $builder.AppendLine($childYaml) > $null
            }

            $builder.ToString()
        }

        $script = {
            $sd = New-Object Sitecore.SecurityModel.SecurityDisabler
            $db = Get-Database -Name "master"
            $parentItem = $db.GetItem([ID]$parentId)

            $parentYaml = $parentItem | ConvertTo-RainbowYaml

            $builder = New-Object System.Text.StringBuilder
            if($serializeParent -or $isMediaItem) {
                $builder.AppendLine($parentYaml) > $null
            }

            $children = $parentItem.GetChildren()

            if($serializeChildren) {                

                foreach($child in $children) {
                    $childYaml = $child | ConvertTo-RainbowYaml
                    $builder.AppendLine($childYaml) > $null
                }
            }

            if($recurseChildren) {
                $builder.Append("<#split#>") > $null

                $childIds = ($children | Where-Object { $_.HasChildren } | Select-Object -ExpandProperty ID) -join "|"
                $builder.Append($childIds) > $null
            }

            $builder.ToString()
            $sd.Dispose() > $null          
        }

        if($IncludeEverything) {
            Invoke-RemoteScript -ScriptBlock $scriptEverything  -Session $Session -Raw
        } else {
            $scriptString = $script.ToString()
            $trueFalseHash = @{$true="`$true";$false="`$false"}
            $scriptString = "`$parentId = '$($RootId)';`$serializeParent = $($trueFalseHash[$serializeParent]);`$serializeChildren = $($trueFalseHash[$serializeChildren]);`$recurseChildren = $($trueFalseHash[$recurseChildren]);" + $scriptString
            $script = [scriptblock]::Create($scriptString)

            Invoke-RemoteScript -ScriptBlock $script  -Session $Session -Raw
        }
    }

    $destinationScript = {
        param(
            $Session,
            [string]$Yaml,
            [bool]$Overwrite
        )

        $rainbowYaml = $Yaml
        $shouldOverwrite = $Overwrite

        $script = {
            $sd = New-Object Sitecore.SecurityModel.SecurityDisabler
            $ed = New-Object Sitecore.Data.Events.EventDisabler
            $buc = New-Object Sitecore.Data.BulkUpdateContext

            $rainbowItems = [regex]::Split($rainbowYaml, "(?=---)") | 
                Where-Object { ![string]::IsNullOrEmpty($_) } | ConvertFrom-RainbowYaml
        
            $totalItems = $rainbowItems.Count
            $itemsToImport = [System.Collections.ArrayList]@()
            foreach($rainbowItem in $rainbowItems) {
            
                if($checkExistingItem) {
                    if((Test-Path -Path "$($rainbowItem.DatabaseName):{$($rainbowItem.Id)}")) { continue }
                }
                $itemsToImport.Add($rainbowItem) > $null
            }
            
            $itemsToImport | ForEach-Object { Import-RainbowItem -Item $_ } > $null

            "{ TotalItems: $($totalItems), ImportedItems: $($itemsToImport.Count) }"
            $buc.Dispose() > $null
            $ed.Dispose() > $null
            $sd.Dispose() > $null
        }

        $scriptString = $script.ToString()
        $trueFalseHash = @{$true="`$true";$false="`$false"}
        $scriptString = "`$rainbowYaml = '$($rainbowYaml)';`$checkExistingItem = $($trueFalseHash[!$shouldOverwrite]);" + $scriptString
        $script = [scriptblock]::Create($scriptString)

        Invoke-RemoteScript -ScriptBlock $script -Session $Session -Raw
    }

    function New-PowerShellRunspace {
        param(
            [System.Management.Automation.Runspaces.RunspacePool]$Pool,
            [scriptblock]$ScriptBlock,
            [PSCustomObject]$Session,
            [object[]]$Arguments
        )
        
        $runspace = [PowerShell]::Create()
        $runspace.AddScript($ScriptBlock) > $null
        $runspace.AddArgument($Session) > $null
        foreach($argument in $Arguments) {
            $runspace.AddArgument($argument) > $null
        }
        $runspace.RunspacePool = $pool

        $runspace
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
    $includeEverything = $Everything.IsPresent

    if(!$Everything.IsPresent -and !$Overwrite.IsPresent) {
        Write-Host "- Preparing to compare source and destination instances"
        $compareScript = { 
            $rootItem = Get-Item -Path "master:" -ID $using:rootId
            $items = [System.Collections.ArrayList]@()
            $items.Add($rootItem) > $null
            if($using:recurseChildren) {
                $children = $rootItem.Axes.GetDescendants()
                if($children.Count -gt 0) {
                    $items.AddRange($children) > $null
                }
            }
            $itemIds = ($items | Select-Object -ExpandProperty ID) -join "|"
            $itemIds
        }

        Write-Host "- Getting list of IDs from source"
        $sourceItemIds = Invoke-RemoteScript -Session $SourceSession -ScriptBlock $compareScript -Raw

        Write-Host "- Getting list of IDs from destination"
        $destinationItemIds = Invoke-RemoteScript -Session $DestinationSession -ScriptBlock $compareScript -Raw

        $queueIds = @()
        if($sourceItemIds) {
            if($destinationItemIds) {
                $referenceIds = $sourceItemIds.Split("|", [System.StringSplitOptions]::RemoveEmptyEntries)
                $differenceIds = $destinationItemIds.Split("|", [System.StringSplitOptions]::RemoveEmptyEntries)
                $queueIds = Compare-Object -ReferenceObject $referenceIds -DifferenceObject $differenceIds | 
                    Where-Object { $_.SideIndicator -eq "<=" } | Select-Object -ExpandProperty InputObject

                foreach($queueId in $queueIds) {
                    $queue.Enqueue($queueId)
                }

                if($queue.Count -ge 1) {
                    $serializeParent = $true
                    $serializeChildren = $false
                    $recurseChildren = $false

                    $threads = 1
                } else {
                    Write-Host "- No items need to be transfered because they already exist"
                }
            } else {
                $queue.Enqueue($rootId)
            }
        } else {
            $queue.Enqueue($rootId)
        }
    } else {
        $queue.Enqueue($rootId)
    }

    $pool = [RunspaceFactory]::CreateRunspacePool(1, $threads)
    $pool.Open()
    $runspaces = [System.Collections.ArrayList]@()

    $totalCounter = 0
    $pullCounter = 0
    $pushCounter = 0
    while ($runspaces.Count -gt 0 -or $queue.Count -gt 0) {

        if($runspaces.Count -eq 0 -and $queue.Count -gt 0) {
            $itemId = ""
            if($queue.TryDequeue([ref]$itemId) -and ![string]::IsNullOrEmpty($itemId)) {
                Write-Host "[Pull] $($itemId)" -ForegroundColor Green
                $runspaceProps = @{
                    ScriptBlock = $sourceScript
                    Pool = $pool
                    Session = $SourceSession
                    Arguments = @($itemId,$true,$serializeChildren,$recurseChildren,$includeEverything)
                }
                $runspace = New-PowerShellRunspace @runspaceProps
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
                        $runspaceProps = @{
                            ScriptBlock = $sourceScript
                            Pool = $pool
                            Session = $SourceSession
                            Arguments = @($itemId,$serializeParent,$serializeChildren,$recurseChildren)
                        }
                        $runspace = New-PowerShellRunspace @runspaceProps                   
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
                    [System.Threading.Interlocked]::Increment([ref] $pushCounter) > $null
                    if(![string]::IsNullOrEmpty($response)) {
                        $feedback = $response | ConvertFrom-Json
                        1..$feedback.TotalItems | % { [System.Threading.Interlocked]::Increment([ref] $totalCounter) } > $null
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
                if($currentRunspace.Operation -eq "Pull") {
                    [System.Threading.Interlocked]::Increment([ref] $pullCounter) > $null
                    if(![string]::IsNullOrEmpty($response)) {
                        $split = $response -split "<#split#>"
             
                        $yaml = $split[0]
                        [bool]$queueChildren = $false
                        if(![string]::IsNullOrEmpty($yaml)) {
                            Write-Host "[Push] $($currentRunspace.Id)" -ForegroundColor Green
                            $runspaceProps = @{
                                ScriptBlock = $destinationScript
                                Pool = $pool
                                Session = $DestinationSession
                                Arguments = @($yaml,$Overwrite.IsPresent)
                            }
                            $runspace = New-PowerShellRunspace @runspaceProps  
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

    Write-Host "[Done] Completed in $($totalSeconds) seconds" -ForegroundColor Yellow
    Write-Host "- Copied $($totalCounter) items"
    Write-Host "- Pull count: $($pullCounter)"
    Write-Host "- Push count: $($pushCounter)"
}

$copyProps = @{
    SourceSession = $sourceSession
    DestinationSession = $destinationSession 
}

$sourceSession = New-ScriptSession -user "admin" -pass "b" -conn "https://spe.dev.local"
$destinationSession = New-ScriptSession -user "admin" -pass "b" -conn "http://sc827"

# Content
$rootId = "{37D08F47-7113-4AD6-A5EB-0C0B04EF6D05}"

# Migrate a single item only if it's missing
#Copy-RainbowContent @copyProps -RootId $rootId

# Migrate a single item and overwrite if it exists
#Copy-RainbowContent @copyProps -RootId $rootId -Overwrite

# Migrate all items only if they are missing
#Copy-RainbowContent @copyProps -RootId $rootId -Recurse

# Migrate all items overwriting if they exist
Copy-RainbowContent @copyProps -RootId $rootId -Overwrite -Recurse

# Migrate all items skipping if they exist
#Copy-RainbowContent @copyProps -RootId $rootId -Everything

# Migrate all items overwriting if they exist
#Copy-RainbowContent @copyProps -RootId $rootId -Overwrite -Everything

# Images
$rootId = "{15451229-7534-44EF-815D-D93D6170BFCB}"

#Copy-RainbowContent @copyProps -RootId "{15451229-7534-44EF-815D-D93D6170BFCB}"

#Copy-RainbowContent @copyProps -RootId "{15451229-7534-44EF-815D-D93D6170BFCB}" -Overwrite -Recurse