#Requires -Modules SPE
 
function Copy-RainbowContent {
    [CmdletBinding()]
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

        [switch]$Recurse,

        [switch]$RemoveNotInSource,

        [ValidateSet("SkipExisting", "Overwrite", "CompareRevision")]
        [string]$CopyBehavior,

        [switch]$CheckDependencies,

        [switch]$Detailed,

        [switch]$ShowProgress
    )

    function Write-Message {
        param(
            [string]$Message,
            [System.ConsoleColor]$ForegroundColor = [System.ConsoleColor]::White,
            [switch]$Hide
        )

        if(!$Hide) {
            Write-Host $Message -ForegroundColor $ForegroundColor
        }
    }
   
    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    $recurseChildren = $Recurse.IsPresent
    $skipExisting = $CopyBehavior -eq "SkipExisting"
    $compareRevision = $CopyBehavior -eq "CompareRevision"
    $overwrite = $CopyBehavior -eq "Overwrite"
    $bulkCopy = $true

    $dependencyScript = {       
        $result = (Test-Path -Path "$($AppPath)\bin\Unicorn.dll") -and (Test-Path -Path "$($AppPath)\bin\Rainbow.dll")
        if($result) {
            $result = $result -band (@(Get-Command -Name "Import-RainbowItem").Count -gt 0)
        }

        $result
    }

    if($CheckDependencies) {
        Write-Message "[Check] Testing connection with remote servers" -ForegroundColor Green
        Write-Message "- Validating source $($SourceSession.Connection[0].BaseUri)"
        if(-not(Test-RemoteConnection -Session $SourceSession -Quiet)) {
            Write-Message " - Unable to connect to $($SourceSession.Connection[0].BaseUri)"
            return
        }
        Write-Message "- Validating destination $($DestinationSession.Connection[0].BaseUri)"
        if(-not(Test-RemoteConnection -Session $DestinationSession -Quiet)) {
            Write-Message " - Unable to connect to $($DestinationSession.Connection[0].BaseUri)"
            return
        }

        Write-Message "[Check] Verifying prerequisites are installed" -ForegroundColor Green
        $isReady = Invoke-RemoteScript -ScriptBlock $dependencyScript -Session $SourceSession

        if($isReady) {
            $isReady = Invoke-RemoteScript -ScriptBlock $dependencyScript -Session $DestinationSession
        }

        if(!$isReady) {
            Write-Message "- Missing required installation of Rainbow and Unicorn"
            return
        } else {
            Write-Message "- All systems are go!"
        }
    }

    if($bulkCopy) {
        $checkIsMediaScript = {
            $rootId = "{ROOT_ID}"
            $db = Get-Database -Name "master"
            $item = $db.GetItem($rootId)
            if($item) {
                $item.Paths.Path.StartsWith("/sitecore/media library/")
            } else {
                $false
            }
        }
        $checkIsMediaScript = [scriptblock]::Create($checkIsMediaScript.ToString().Replace("{ROOT_ID}", $RootId))
        $bulkCopy = !(Invoke-RemoteScript -ScriptBlock $checkIsMediaScript -Session $SourceSession)
    }

    Write-Message "[Running] Transfer from $($SourceSession.Connection[0].BaseUri) to $($DestinationSession.Connection[0].BaseUri)" -ForegroundColor Green
    Write-Message "[Options] RootId = $($RootId); CopyBehavior = $($CopyBehavior); Recurse = $($Recurse); RemoveNotInSource = $($RemoveNotInSource)"

    class ShallowItem {
        [string]$ItemId
        [string]$RevisionId
        [string]$ParentId
    }

    $compareScript = {
        $rootId = "{ROOT_ID}"
        $recurseChildren = [bool]::Parse("{RECURSE_CHILDREN}")
        Import-Function -Name Invoke-SqlCommand
        $connection = [Sitecore.Configuration.Settings]::GetConnectionString("master")

        $revisionFieldId = "{8CDC337E-A112-42FB-BBB4-4143751E123F}"
        if($recurseChildren) {
            $query = "
                WITH [ContentQuery] AS (SELECT [ID], [Name], [ParentID] FROM [dbo].[Items] WHERE ID='$($rootId)' UNION ALL SELECT  i.[ID], i.[Name], i.[ParentID] FROM [dbo].[Items] i INNER JOIN [ContentQuery] ci ON ci.ID = i.[ParentID])
                SELECT cq.[ID], vf.[Value] AS [Revision], cq.[ParentID] FROM [ContentQuery] cq INNER JOIN dbo.[VersionedFields] vf ON cq.[ID] = vf.[ItemId] WHERE vf.[FieldId] = '$($revisionFieldId)' AND vf.[Language] != '' AND vf.[Version] = (SELECT MAX(vf2.[Version]) FROM dbo.[VersionedFields] vf2 WHERE vf2.[ItemId] = cq.[Id])
            "
        } else {
            $query = "
                WITH [ContentQuery] AS (SELECT [ID], [Name], [ParentID] FROM [dbo].[Items] WHERE ID='$($rootId)')
                SELECT cq.[ID], vf.[Value] AS [Revision], cq.[ParentID] FROM [ContentQuery] cq INNER JOIN dbo.[VersionedFields] vf ON cq.[ID] = vf.[ItemId] WHERE vf.[FieldId] = '$($revisionFieldId)' AND vf.[Language] != '' AND vf.[Version] = (SELECT MAX(vf2.[Version]) FROM dbo.[VersionedFields] vf2 WHERE vf2.[ItemId] = cq.[Id])
            "
        }
        $records = Invoke-SqlCommand -Connection $connection -Query $query
        if($records) {
            $itemIds = $records | ForEach-Object { "I:{$($_.ID)}+R:{$($_.Revision)}+P:{$($_.ParentID)}" }
            $itemIds -join "|"
        }
    }
    $compareScript = [scriptblock]::Create($compareScript.ToString().Replace("{ROOT_ID}", $RootId).Replace("{RECURSE_CHILDREN}", $recurseChildren))

    Write-Message "- Querying item list from source"
    $s1 = [System.Diagnostics.Stopwatch]::StartNew()
    $sourceTree = [System.Collections.Generic.Dictionary[string,[System.Collections.Generic.List[ShallowItem]]]]([StringComparer]::OrdinalIgnoreCase)
    $sourceTree.Add($RootId, [System.Collections.Generic.List[ShallowItem]]@())
    $sourceRecordsString = Invoke-RemoteScript -Session $SourceSession -ScriptBlock $compareScript -Raw
    if([string]::IsNullOrEmpty($sourceRecordsString)) {
        Write-Message "- No items found in source"
        return
    }
    
    $sourceItemsHash = [System.Collections.Generic.HashSet[string]]([StringComparer]::OrdinalIgnoreCase)
    $sourceItemRevisionLookup = @{}
    $RootParentId = ""
    foreach($sourceRecord in $sourceRecordsString.Split("|".ToCharArray(), [System.StringSplitOptions]::RemoveEmptyEntries)) {
        $shallowItem = [ShallowItem]@{
            "ItemId"=$sourceRecord.Substring(2,38)
            "RevisionId"=$sourceRecord.Substring(43,38)
            "ParentId"=$sourceRecord.Substring(84,38)
        }
        if($sourceItemsHash.Contains($shallowItem.ItemId)) {
            Write-Message " - Detected duplicate item $($shallowItem.ItemId)" -ForegroundColor Yellow
            Write-Message "  - Revision $($shallowItem.RevisionId)" -ForegroundColor Yellow
            Write-Message "  - Revision $($sourceItemRevisionLookup[$shallowItem.ItemId])" -ForegroundColor Yellow
        }
        $sourceItemsHash.Add($shallowItem.ItemId) > $null
        $sourceItemRevisionLookup[$shallowItem.ItemId] = $shallowItem.RevisionId
        if(!$sourceTree.ContainsKey($shallowItem.ItemId)) {
            $sourceTree[$shallowItem.ItemId] = [System.Collections.Generic.List[ShallowItem]]@()
        }
        $childCollection = $sourceTree[$shallowItem.ParentId]
        if(!$childCollection) {
            $childCollection = [System.Collections.Generic.List[ShallowItem]]@()
        }
        $childCollection.Add($shallowItem) > $null
        $sourceTree[$shallowItem.ParentId] = $childCollection
        if([string]::IsNullOrEmpty($RootParentId) -and $shallowItem.ItemId -eq $RootId) {
            $RootParentId = $shallowItem.ParentId
        }
    }
    $sourceShallowItemsCount = $sourceItemsHash.Count
    $s1.Stop()
    Write-Message " - Found $($sourceShallowItemsCount) item(s) in $($s1.ElapsedMilliseconds / 1000) seconds"

    $skipItemsHash = [System.Collections.Generic.HashSet[string]]([StringComparer]::OrdinalIgnoreCase)
    $destinationItemsHash = [System.Collections.Generic.HashSet[string]]([StringComparer]::OrdinalIgnoreCase)
    $destinationItemRevisionLookup = @{}
    if(!$overwrite -or $RemoveNotInSource) {
        Write-Message "- Querying item list from destination"
        $d1 = [System.Diagnostics.Stopwatch]::StartNew()
        $destinationRecordsString = Invoke-RemoteScript -Session $DestinationSession -ScriptBlock $compareScript -Raw
        
        if(![string]::IsNullOrEmpty($destinationRecordsString)) {
            foreach($destinationRecord in $destinationRecordsString.Split("|".ToCharArray(), [System.StringSplitOptions]::RemoveEmptyEntries)) {
                $shallowItem = [ShallowItem]@{
                    "ItemId"=$destinationRecord.Substring(2,38)
                    "RevisionId"=$destinationRecord.Substring(43,38)
                    "ParentId"=$destinationRecord.Substring(84,38)
                }
                if($destinationItemsHash.Contains($shallowItem.ItemId)) {
                    Write-Message " - Detected duplicate item $($shallowItem.ItemId)" -ForegroundColor Yellow
                    Write-Message "  - Revision $($shallowItem.RevisionId)" -ForegroundColor Yellow
                    Write-Message "  - Revision $($destinationItemRevisionLookup[$shallowItem.ItemId])" -ForegroundColor Yellow
                }
                $destinationItemsHash.Add($shallowItem.ItemId) > $null
                $destinationItemRevisionLookup[$shallowItem.ItemId] = $shallowItem.RevisionId
                if(($compareRevision -and $sourceItemRevisionLookup[$shallowItem.ItemId] -eq $shallowItem.RevisionId) -or
                    ($skipExisting -and $sourceTree.ContainsKey($shallowItem.ItemId))) {
                    $skipItemsHash.Add($shallowItem.ItemId) > $null
                }            
            }
        }
        $destinationShallowItemsCount = $destinationItemsHash.Count
        $d1.Stop()
        Write-Message " - Found $($destinationShallowItemsCount) item(s) in $($d1.ElapsedMilliseconds / 1000) seconds"
    }

    $pullCounter = 0
    $pushCounter = 0
    $errorCounter = 0
    $updateCounter = 0
    
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

    $sourceScript = {
        param(
            $Session,
            [string]$ItemIdListString
        )

        $script = {
            $sd = New-Object Sitecore.SecurityModel.SecurityDisabler
            $builder = New-Object System.Text.StringBuilder

            $db = Get-Database -Name "master"
            $itemIdList = $itemIdListString.Split("|".ToCharArray(), [System.StringSplitOptions]::RemoveEmptyEntries)
            foreach($itemId in $itemIdList) {
                $item = $db.GetItem([ID]$itemId)

                $itemYaml = $item | ConvertTo-RainbowYaml  
                $builder.Append($itemYaml) > $null
                $builder.Append("<#item#>") > $null
            }

            $builder.ToString()
            $sd.Dispose() > $null  
        }

        $scriptString = $script.ToString()
        $scriptString = "`$itemIdListString = '$($ItemIdListString)';`n" + $scriptString
        $script = [scriptblock]::Create($scriptString)

        Invoke-RemoteScript -ScriptBlock $script -Session $Session -Raw
    }

    $destinationScript = {
        param(
            $Session,
            [string]$Yaml,
            [hashtable]$RevisionLookup
        )

        $rainbowYaml = $Yaml
        # The '__Revision' field and IDs in Unicorn/Rainbow yaml are stored without curly braces.
        # Trimming it here to simplify the logic running on the remote server.
        $revisionLookupString = New-Object System.Text.StringBuilder
        $revisionLookupString.Append("@{") > $null
        foreach($key in $RevisionLookup.Keys) {
            $revisionLookupString.Append("`"$($key.TrimStart('{').TrimEnd('}'))`"=`"$($RevisionLookup[$key].TrimStart('{').TrimEnd('}'))`";") > $null
        }
        $revisionLookupString.Append("}") > $null

        $script = {
            $sd = New-Object Sitecore.SecurityModel.SecurityDisabler
            $ed = New-Object Sitecore.Data.Events.EventDisabler
            $buc = New-Object Sitecore.Data.BulkUpdateContext

            $rainbowYamlBytes = [System.Convert]::FromBase64String($rainbowYamlBase64)
            $rainbowYaml = [System.Text.Encoding]::UTF8.GetString($rainbowYamlBytes)
            $rainbowItems = $rainbowYaml -split '<#item#>' | Where-Object { ![string]::IsNullOrEmpty($_) } | ConvertFrom-RainbowYaml

            $totalItems = $rainbowItems.Count
            $importedItems = 0
            $errorCount = 0
            $errorMessages = New-Object System.Text.StringBuilder
            $db = Get-Database -Name "master"

            $rainbowItems | ForEach-Object {
                $currentRainbowItem = $_
                $itemId = "$($currentRainbowItem.Id)"             
                try {
                    Import-RainbowItem -Item $currentRainbowItem
                                       
                    if($revisionLookup.ContainsKey($itemId)) {                    
                        $currentItem = $db.GetItem($itemId)
                        $currentItem.Editing.BeginEdit()
                        $currentItem.Fields["__Revision"].Value = $revisionLookup[$itemId]
                        $currentItem.Editing.EndEdit($false, $true)
                    } else {
                        $errorCount++
                        $errorMessages.Append("No matching item found in lookup table.") > $null
                        $errorMessages.Append("$($itemId)") > $null
                    }
                    $importedItems++
                } catch {
                    $errorCount++
                    Write-Log "Importing $($itemId) failed with error $($Error[0].Exception.Message)"
                    Write-Log ($currentRainbowItem | Select-Object -Property Id,ParentId,Path | ConvertTo-Json)
                    $errorMessages.Append("$($itemId) Failed") > $null
                }
            } > $null

            $buc.Dispose() > $null
            $ed.Dispose() > $null
            $sd.Dispose() > $null

            "{ TotalItems: $($totalItems), ImportedItems: $($importedItems), ErrorCount: $($errorCount), ErrorMessages: '$($errorMessages.ToString())' }"
        }

        $scriptString = $script.ToString()
        $trueFalseHash = @{$true="`$true";$false="`$false"}
        $rainbowYamlBytes = [System.Text.Encoding]::UTF8.GetBytes($rainbowYaml)
        $scriptString = "`$rainbowYamlBase64 = '$([System.Convert]::ToBase64String($rainbowYamlBytes))';`n`$revisionLookup = $($revisionLookupString.ToString());" + $scriptString
        $script = [scriptblock]::Create($scriptString)

        Invoke-RemoteScript -ScriptBlock $script -Session $Session -Raw
    }

    $pushedLookup = @{}
    $pullPool = [RunspaceFactory]::CreateRunspacePool(1, 4)
    $pullPool.Open()
    $pullRunspaces = [System.Collections.Generic.List[PSCustomObject]]@()
    $pushPool = [RunspaceFactory]::CreateRunspacePool(1, 4)
    $pushPool.Open()
    $pushRunspaces = [System.Collections.Generic.List[PSCustomObject]]@()

    class QueueItem {
        [int]$Level
        [string]$Yaml
        [hashtable]$RevisionLookup
    }

    $treeLevels = [System.Collections.Generic.List[System.Collections.Generic.List[ShallowItem]]]@()
    $treeLevelQueue = [System.Collections.Generic.Queue[ShallowItem]]@()
    $treeLevelQueue.Enqueue($sourceTree[$RootParentId][0])
    Write-Message "- Tree Level Counts" -Hide:(!$Detailed)
    while($treeLevelQueue.Count -gt 0) {
        if($bulkCopy) {
            $currentLevelItems = [System.Collections.Generic.List[ShallowItem]]@()
            while($treeLevelQueue.Count -gt 0 -and ($currentDequeued = $treeLevelQueue.Dequeue())) {
                $currentLevelItems.Add($currentDequeued) > $null
            }
            $treeLevels.Add($currentLevelItems) > $null
            foreach($currentLevelItem in $currentLevelItems) {
                $currentLevelChildren = $sourceTree[$currentLevelItem.ItemId]
                foreach($currentLevelChild in $currentLevelChildren) {
                    $treeLevelQueue.Enqueue($currentLevelChild)
                }
            }
            Write-Message " - Level $($treeLevels.Count - 1) : $($currentLevelItems.Count)" -Hide:(!$Detailed)
        } else {
            while($treeLevelQueue.Count -gt 0 -and ($currentDequeued = $treeLevelQueue.Dequeue())) {
                $singleLevelItem = [System.Collections.Generic.List[ShallowItem]]@()
                $singleLevelItem.Add($currentDequeued) > $null
                $treeLevels.Add($singleLevelItem) > $null
                $singleLevelChildren = $sourceTree[$singleLevelItem.ItemId]
                foreach($singleLevelChild in $singleLevelChildren) {
                    $treeLevelQueue.Enqueue($singleLevelChild)
                }
            }
            Write-Message " - Levels 0 to $($treeLevels.Count - 1) : 1" -Hide:(!$Detailed)
        }
    }

    Write-Message "Spinning up jobs to transfer content" -ForegroundColor Yellow -Hide:(!$Detailed)
    
    $processedItemsHash = [System.Collections.Generic.HashSet[string]]([StringComparer]::OrdinalIgnoreCase)
    $skippedItemsHash = [System.Collections.Generic.HashSet[string]]([StringComparer]::OrdinalIgnoreCase)
    $pullLookup = @{}
    $currentLevel = 0
    $totalLevels = $treeLevels.Count
    $keepProcessing = $true
    while($keepProcessing) {
        if($ShowProgress) {
            Write-Progress -Activity "Transfer of $($RootId) from $($SourceSession.Connection[0].BaseUri) to $($DestinationSession.Connection[0].BaseUri)" -Status "Pull $($pullCounter), Push $($pushCounter), Level $($currentLevel)" -PercentComplete ([Math]::Min(($updateCounter + $skippedItemsHash.Count) * 100 / $sourceShallowItemsCount, 100))
        }
        if($currentLevel -lt $treeLevels.Count) {
            $itemIdList = [System.Collections.Generic.List[string]]@()
            $levelItems = $treeLevels[$currentLevel]
            $pushedLookup.Add($currentLevel, [System.Collections.Generic.List[QueueItem]]@()) > $null
            foreach($levelItem in $levelItems) {
                $itemId = $levelItem.ItemId
                $processedItemsHash.Add($itemId) > $null

                if(($skipExisting -and $skipItemsHash.Contains($itemId)) -or 
                    ($compareRevision -and $destinationItemsHash.Contains($itemId) -and 
                    $sourceItemRevisionLookup[$itemId] -eq $destinationItemRevisionLookup[$itemId])) {
                    Write-Message "[Skip] $($itemId)" -ForegroundColor Cyan -Hide:(!$Detailed)
                    $skippedItemsHash.Add($itemId) > $null
                } else {
                    $itemIdList.Add($itemId) > $null
                }
            }
            $pullLookup[$currentLevel] = $itemIdList
            if($itemIdList.Count -gt 0) {
                if($bulkCopy) {
                    Write-Message "[Pull] Level $($currentLevel) with $($itemIdList.Count) item(s)"
                }             
                Write-Message "[Pull] $($currentLevel)" -ForegroundColor Green -Hide:(!$Detailed)
                $runspaceProps = @{
                    ScriptBlock = $sourceScript
                    Pool = $pullPool
                    Session = $SourceSession
                    Arguments = @(($itemIdList -join "|"))
                }
                $runspace = New-PowerShellRunspace @runspaceProps
                $pullRunspaces.Add([PSCustomObject]@{
                    Operation = "Pull"
                    Pipe = $runspace
                    Status = $runspace.BeginInvoke()
                    Level = $currentLevel
                    Time = [datetime]::Now
                }) > $null
            } else {
                if($bulkCopy) {
                    Write-Message "[Skip] Level $($currentLevel)" -ForegroundColor Cyan
                } 
                if($pushedLookup.Contains($currentLevel) -and $pushedLookup[$currentLevel].Count -eq 0) {
                    $pushedLookup.Remove($currentLevel)
                }
            }
            $currentLevel++             
            $percentComplete = ($currentLevel * 100 / $totalLevels)
            if($percentComplete % 5 -eq 0) {
                Write-Message "[Pull] $($percentComplete)% complete"
            }
        }

        $currentRunspaces = $pushRunspaces.ToArray() + $pullRunspaces.ToArray()
        foreach($currentRunspace in $currentRunspaces) {
            if(!$currentRunspace.Status.IsCompleted) { continue }

            $response = $currentRunspace.Pipe.EndInvoke($currentRunspace.Status)

            if($currentRunspace.Operation -eq "Pull") {
                [System.Threading.Interlocked]::Increment([ref] $pullCounter) > $null
            } elseif ($currentRunspace.Operation -eq "Push") {
                [System.Threading.Interlocked]::Increment([ref] $pushCounter) > $null
            }
            Write-Message "[$($currentRunspace.Operation)] $($currentRunspace.Level) completed" -ForegroundColor Gray -Hide:(!$Detailed)
            Write-Message "- Processed in $(([datetime]::Now - $currentRunspace.Time))" -ForegroundColor Gray -Hide:(!$Detailed)

            if($currentRunspace.Operation -eq "Pull") {                
                if(![string]::IsNullOrEmpty($response) -and [regex]::IsMatch($response,"^---")) {               
                    $yaml = $response
                    $revisionLookup = @{}
                    $pulledIdList = $pullLookup[$currentRunspace.Level]
                    foreach($pulledItemId in $pulledIdList) {
                        $revisionLookup.Add($pulledItemId, $sourceItemRevisionLookup[$pulledItemId]) > $null
                    }
                    if($pushedLookup.Contains(($currentRunspace.Level - 1))) {
                        Write-Message "[Queue] $($currentRunspace.Level)" -ForegroundColor Cyan -Hide:(!$Detailed)
                        $pushedLookup[($currentRunspace.Level - 1)].Add([QueueItem]@{"Level"=$currentRunspace.Level;"Yaml"=$yaml;"RevisionLookup"=$revisionLookup;}) > $null
                    } else {               
                        Write-Message "[Push] $($currentRunspace.Level)" -ForegroundColor Gray -Hide:(!$Detailed)
                        $runspaceProps = @{
                            ScriptBlock = $destinationScript
                            Pool = $pushPool
                            Session = $DestinationSession
                            Arguments = @($yaml,$revisionLookup)
                        }
                        $runspace = New-PowerShellRunspace @runspaceProps  
                        $pushRunspaces.Add([PSCustomObject]@{
                            Operation = "Push"
                            Pipe = $runspace
                            Status = $runspace.BeginInvoke()
                            Level = $currentRunspace.Level
                            Time = [datetime]::Now
                        }) > $null
                    }
                }

                $currentRunspace.Pipe.Dispose()
                $pullRunspaces.Remove($currentRunspace) > $null
            }

            if($currentRunspace.Operation -eq "Push") {
                if(![string]::IsNullOrEmpty($response)) {
                    $feedback = $response | ConvertFrom-Json
                    Write-Message "- Imported $($feedback.ImportedItems)/$($feedback.TotalItems)" -ForegroundColor Gray -Hide:(!$Detailed)
                    1..$feedback.ImportedItems | ForEach-Object { [System.Threading.Interlocked]::Increment([ref] $updateCounter) > $null }
                    if($feedback.ErrorCount -gt 0) {
                        [System.Threading.Interlocked]::Increment([ref] $errorCounter) > $null
                        Write-Message "- Errored $($feedback.ErrorCount)" -ForegroundColor Red -Hide:(!$Detailed)
                        Write-Message " - $($feedback.ErrorMessages)" -ForegroundColor Red -Hide:(!$Detailed)
                    }
                }

                $queuedItems = [System.Collections.Generic.List[QueueItem]]@()
                if($pushedLookup.ContainsKey($currentRunspace.Level)) {
                    $queuedItems.AddRange($pushedLookup[$currentRunspace.Level])
                    $pushedLookup.Remove($currentRunspace.Level) > $null
                    if($bulkCopy) {
                        Write-Message "[Pull] Level $($currentRunspace.Level) completed" -ForegroundColor Gray
                    }
                }
                if($queuedItems.Count -gt 0) {
                    foreach($queuedItem in $queuedItems) {
                        $level = $queuedItem.Level
                        Write-Message "[Dequeue] $($level)" -ForegroundColor Cyan -Hide:(!$Detailed)
                        Write-Message "[Push] $($level)" -ForegroundColor Green -Hide:(!$Detailed)
                        $runspaceProps = @{
                            ScriptBlock = $destinationScript
                            Pool = $pushPool
                            Session = $DestinationSession
                            Arguments = @($queuedItem.Yaml,$queuedItem.RevisionLookup)
                        }

                        $runspace = New-PowerShellRunspace @runspaceProps  
                        $pushRunspaces.Add([PSCustomObject]@{
                            Operation = "Push"
                            Pipe = $runspace
                            Status = $runspace.BeginInvoke()
                            Level = $level
                            Time = [datetime]::Now
                        }) > $null
                    }
                }
                
                $currentRunspace.Pipe.Dispose()
                $pushRunspaces.Remove($currentRunspace) > $null
            }
        }

        $keepProcessing = ($currentLevel -lt $treeLevels.Count -or $pullRunspaces.Count -gt 0 -or $pushRunspaces.Count -gt 0)
    }

    $pullPool.Close() 
    $pullPool.Dispose()
    $pushPool.Close() 
    $pushPool.Dispose()

    if($RemoveNotInSource) {
        $removeItemsHash = [System.Collections.Generic.HashSet[string]]([StringComparer]::OrdinalIgnoreCase)
        $removeItemsHash.UnionWith($destinationItemsHash)
        $removeItemsHash.ExceptWith($sourceItemsHash)

        if($removeItemsHash.Count -gt 0) {
            Write-Message "- Removing items from destination not in source"
            $itemsNotInSource = $removeItemsHash -join "|"
            $removeNotInSourceScript = {
                $sd = New-Object Sitecore.SecurityModel.SecurityDisabler
                $ed = New-Object Sitecore.Data.Events.EventDisabler
                $itemsNotInSource = "{ITEM_IDS}"
                $itemsNotInSourceIds = ($itemsNotInSource).Split("|", [System.StringSplitOptions]::RemoveEmptyEntries)
                $db = Get-Database -Name "master"
                foreach($itemId in $itemsNotInSourceIds) {
                    $db.GetItem($itemId) | Remove-Item -Recurse -ErrorAction 0
                }
                $ed.Dispose() > $null
                $sd.Dispose() > $null
            }
            $removeNotInSourceScript = [scriptblock]::Create($removeNotInSourceScript.ToString().Replace("{ITEM_IDS}", $itemsNotInSource))
            Invoke-RemoteScript -ScriptBlock $removeNotInSourceScript -Session $DestinationSession -Raw
            Write-Message " - Removed $($removeItemsHash.Count) item(s) from the destination"
        }
    }

    $watch.Stop()
    $totalSeconds = $watch.ElapsedMilliseconds / 1000
    Write-Message "[Done] Completed in $($totalSeconds) seconds" -ForegroundColor Green
    Write-Progress -Activity "[Done] Completed in $($totalSeconds) seconds" -Completed
    if($processedItemsHash.Count -gt 0) {
        Write-Message "- Processed count: $($processedItemsHash.Count)"
        Write-Message " - Update count: $($updateCounter)"
        Write-Message " - Skip count: $($skippedItemsHash.Count)"
        Write-Message " - Error count: $($errorCounter)"
        Write-Message " - Pull count: $($pullCounter)"
        Write-Message " - Push count: $($pushCounter)"
    }
}
