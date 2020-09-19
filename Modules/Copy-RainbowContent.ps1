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

        [switch]$SkipDependencyCheck
    )
    
    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    $recurseChildren = $Recurse.IsPresent
    $skipExisting = $CopyBehavior -eq "SkipExisting"
    $compareRevision = $CopyBehavior -eq "CompareRevision"

    $dependencyScript = {
        $dependencies = Get-ChildItem -Path "$($AppPath)\bin" -Include "Unicorn.*","Rainbow.*" -Recurse | 
            Select-Object -ExpandProperty Name
        
        $result = $true
        if($dependencies -contains "Unicorn.dll" -and $dependencies -contains "Rainbow.dll") {
            $result = $result -band $true
        } else {
            $result = $result -band $false
        }

        if($result) {
            $result = $result -band (@(Get-Command -Noun "RainbowYaml").Count -gt 0)
        }

        $result
    }

    if(!$SkipDependencyCheck) {
        Write-Host "Verifying both systems have Rainbow and Unicorn"
        $isReady = Invoke-RemoteScript -ScriptBlock $dependencyScript -Session $SourceSession

        if($isReady) {
            $isReady = Invoke-RemoteScript -ScriptBlock $dependencyScript -Session $DestinationSession
        }

        if(!$isReady) {
            Write-Host "- Missing required installation of Rainbow and Unicorn"
            exit
        } else {
            Write-Host "- Verification complete"
        }
    }

    Write-Host "[Running] Transfer from $($SourceSession.Connection[0].BaseUri) to $($DestinationSession.Connection[0].BaseUri)" -ForegroundColor Yellow
    Write-Host "[Options] RootId = $($RootId); CopyBehavior = $($CopyBehavior); Recurse = $($Recurse); RemoveNotInSource = $($RemoveNotInSource)" -ForegroundColor Cyan

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
                SELECT cq.[ID], vf.[Value] AS [Revision], cq.[ParentID] FROM [ContentQuery] cq INNER JOIN dbo.[VersionedFields] vf ON cq.[ID] = vf.[ItemId] WHERE vf.[FieldId] = '$($revisionFieldId)' AND vf.[Version] = (SELECT MAX(vf2.[Version]) FROM dbo.[VersionedFields] vf2 WHERE vf2.[ItemId] = cq.[Id])
            "
        } else {
            $query = "
                WITH [ContentQuery] AS (SELECT [ID], [Name], [ParentID] FROM [dbo].[Items] WHERE ID='$($rootId)')
                SELECT cq.[ID], vf.[Value] AS [Revision], cq.[ParentID] FROM [ContentQuery] cq INNER JOIN dbo.[VersionedFields] vf ON cq.[ID] = vf.[ItemId] WHERE vf.[FieldId] = '$($revisionFieldId)' AND vf.[Version] = (SELECT MAX(vf2.[Version]) FROM dbo.[VersionedFields] vf2 WHERE vf2.[ItemId] = cq.[Id])
            "
        }
        $records = Invoke-SqlCommand -Connection $connection -Query $query
        if($records) {
            $itemIds = $records | ForEach-Object { "I:{$($_.ID)}+R:{$($_.Revision)}+P:{$($_.ParentID)}" }
            $itemIds -join "|"
        }
    }
    $compareScript = [scriptblock]::Create($compareScript.ToString().Replace("{ROOT_ID}", $RootId).Replace("{RECURSE_CHILDREN}", $recurseChildren))

    Write-Host "- Querying item list from source"
    $sourceTree = @{}
    $sourceTree.Add($RootId, [System.Collections.Generic.List[ShallowItem]]@())
    $sourceRecordsString = Invoke-RemoteScript -Session $SourceSession -ScriptBlock $compareScript -Raw
    $sourceShallowItemsCount = 0
    $sourceItemsHash = [System.Collections.Generic.HashSet[string]]([StringComparer]::OrdinalIgnoreCase)
    $sourceItemRevisionLookup = @{}
    foreach($sourceRecord in $sourceRecordsString.Split("|", [System.StringSplitOptions]::RemoveEmptyEntries)) {
        $sourceShallowItemsCount++
        $split = $sourceRecord.Split("+")
        $shallowItem = [ShallowItem]@{
            "ItemId"=$split[0].Replace("I:","")
            "RevisionId"=$split[1].Replace("R:","")
            "ParentId"=$split[2].Replace("P:","")
        }
        $sourceItemsHash.Add($shallowItem.ItemId) > $null
        if(!$sourceTree.ContainsKey($shallowItem.ParentId)) {
            $sourceTree[$shallowItem.ParentId] = [System.Collections.Generic.List[ShallowItem]]@()
        }
        $sourceTree[$shallowItem.ParentId].Add($shallowItem)
        if(!$sourceTree.ContainsKey($shallowItem.ItemId)) {
            $sourceTree[$shallowItem.ItemId] = [System.Collections.Generic.List[ShallowItem]]@()
        }
        $sourceItemRevisionLookup[$shallowItem.ItemId] = $shallowItem.RevisionId
    }
    Write-Host " - Found $($sourceShallowItemsCount) item(s)"

    $skipItemsHash = [System.Collections.Generic.HashSet[string]]([StringComparer]::OrdinalIgnoreCase)
    $destinationItemsHash = [System.Collections.Generic.HashSet[string]]([StringComparer]::OrdinalIgnoreCase)
    $destinationItemRevisionLookup = @{}
    if($CopyBehavior -ne "Overwrite" -or $RemoveNotInSource) {
        Write-Host "- Querying item list from destination"
        $destinationRecordsString = Invoke-RemoteScript -Session $DestinationSession -ScriptBlock $compareScript -Raw
        $destinationShallowItemsCount = 0
        foreach($destinationRecord in $destinationRecordsString.Split("|", [System.StringSplitOptions]::RemoveEmptyEntries)) {
            $destinationShallowItemsCount++
            $split = $destinationRecord.Split("+")
            $shallowItem = [ShallowItem]@{
                "ItemId"=$split[0].Replace("I:","")
                "RevisionId"=$split[1].Replace("R:","")
                "ParentId"=$split[2].Replace("P:","")
            }
            $destinationItemsHash.Add($shallowItem.ItemId) > $null
            if(($compareRevision -and $sourceItemRevisionLookup[$shallowItem.ItemId] -eq $shallowItem.RevisionId) -or
                ($skipExisting -and $sourceTree.ContainsKey($shallowItem.ItemId))) {
                $skipItemsHash.Add($shallowItem.ItemId) > $null
            }
            $destinationItemRevisionLookup[$shallowItem.ItemId] = $shallowItem.RevisionId
        }

        Write-Host " - Found $($destinationShallowItemsCount) item(s)"
    }

    $totalCounter = 0
    $pullCounter = 0
    $pushCounter = 0

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
            [string]$RootId
        )

        $script = {
            $sd = New-Object Sitecore.SecurityModel.SecurityDisabler
            $db = Get-Database -Name "master"
            $item = $db.GetItem([ID]$itemId)

            $itemYaml = $item | ConvertTo-RainbowYaml  
            $itemYaml       
        }

        $scriptString = $script.ToString()
        $scriptString = "`$itemId = '$($RootId)';" + $scriptString
        $script = [scriptblock]::Create($scriptString)

        Invoke-RemoteScript -ScriptBlock $script -Session $Session -Raw
    }

    $destinationScript = {
        param(
            $Session,
            [string]$Yaml,
            [string]$RevisionId
        )

        $rainbowYaml = $Yaml

        $script = {
            $sd = New-Object Sitecore.SecurityModel.SecurityDisabler
            $ed = New-Object Sitecore.Data.Events.EventDisabler
            $buc = New-Object Sitecore.Data.BulkUpdateContext

            $rainbowYamlBytes = [System.Convert]::FromBase64String($rainbowYamlBase64)
            $rainbowYaml = [System.Text.Encoding]::UTF8.GetString($rainbowYamlBytes)
            $rainbowItems = [regex]::Split($rainbowYaml, "(?=---$)") | 
                Where-Object { ![string]::IsNullOrEmpty($_) } | ConvertFrom-RainbowYaml
        
            $totalItems = $rainbowItems.Count
            $itemsToImport = [System.Collections.ArrayList]@()
            $totalSkippedItems = 0
            foreach($rainbowItem in $rainbowItems) {
                $itemsToImport.Add($rainbowItem) > $null
            }
            
            $errorMessages = @()
            $itemsToImport | ForEach-Object {
                $currentRainbowItem = $_               
                try {
                    Import-RainbowItem -Item $currentRainbowItem
                    if(![string]::IsNullOrEmpty($currentItemRevisionId)) {
                        $db = Get-Database -Name "master"
                        $currentItem = $db.GetItem($currentRainbowItem.Id)
                        $currentItem.Editing.BeginEdit()
                        $currentItem.Fields["__Revision"].Value = $currentItemRevisionId.TrimStart('{').TrimEnd('}')
                        $currentItem.Editing.EndEdit($false, $true)
                    }
                } catch {
                    Write-Log "Importing $($currentRainbowItem.Id) failed with error $($Error[0].Exception.Message)"
                    Write-Log ($currentRainbowItem | ConvertTo-Json)
                    $errorMessages += "$($currentRainbowItem.Id) Failed"
                }
            } > $null

            $buc.Dispose() > $null
            $ed.Dispose() > $null
            $sd.Dispose() > $null

            "{ TotalItems: $($totalItems), ImportedItems: $($itemsToImport.Count), TotalSkippedItems: $($totalSkippedItems), ErrorCount: $($errorMessages.Count) }"
        }

        $scriptString = $script.ToString()
        $trueFalseHash = @{$true="`$true";$false="`$false"}
        $rainbowYamlBytes = [System.Text.Encoding]::UTF8.GetBytes($rainbowYaml)
        $scriptString = "`$rainbowYamlBase64 = '$([System.Convert]::ToBase64String($rainbowYamlBytes))';`$currentItemRevisionId = '$($RevisionId)';" + $scriptString
        $script = [scriptblock]::Create($scriptString)

        Invoke-RemoteScript -ScriptBlock $script -Session $Session -Raw
    }

    $pushedLookup = @{}
    $pullPool = [RunspaceFactory]::CreateRunspacePool(1, 2)
    $pullPool.Open()
    $pullRunspaces = [System.Collections.ArrayList]@()
    $pushPool = [RunspaceFactory]::CreateRunspacePool(1, 4)
    $pushPool.Open()
    $pushRunspaces = [System.Collections.ArrayList]@()

    class QueueItem {
        [string]$ItemId
        [string]$RevisionId
        [string]$Yaml
    }

    if(($skipExisting -and $skipItemsHash.Contains($RootId)) -or ($compareRevision -and $destinationItemsHash.Contains($RootId) -and $sourceItemRevisionLookup[$RootId] -eq $destinationItemRevisionLookup[$RootId])) {
        Write-Host "[Skip] $($RootId)" -ForegroundColor Cyan
        $runspaceProps = @{
            ScriptBlock = {}
            Pool = $pullPool
            Session = $SourceSession
            Arguments = @($RootId)
        }
        $runspace = New-PowerShellRunspace @runspaceProps
        $pullRunspaces.Add([PSCustomObject]@{ Skip = $true; Operation = "Pull"; Pipe = $runspace; Status = [PSCustomObject]@{"IsCompleted"=$true}; Id = $RootId; ParentId = ""; RevisionId = $sourceItemRevisionLookup[$RootId]; Time = [datetime]::Now; }) > $null
    } else {
        Write-Host "Spinning up jobs to transfer content" -ForegroundColor Yellow
        Write-Host "[Pull] $($RootId)" -ForegroundColor Green
        $runspaceProps = @{
            ScriptBlock = $sourceScript
            Pool = $pullPool
            Session = $SourceSession
            Arguments = @($RootId)
        }
        $runspace = New-PowerShellRunspace @runspaceProps
        $pullRunspaces.Add([PSCustomObject]@{ Operation = "Pull"; Pipe = $runspace; Status = $runspace.BeginInvoke(); Id = $RootId; ParentId = ""; RevisionId = $sourceItemRevisionLookup[$RootId]; Time = [datetime]::Now; }) > $null
    }

    while($pullRunspaces.Count -gt 0 -or $pushRunspaces.Count -gt 0) {
        Write-Progress -Activity "Queued root $($RootId)" -Status "Pull $($pullCounter), Push $($pushCounter)" -PercentComplete ($totalCounter * 100 / $sourceShallowItemsCount)
        $currentRunspaces = $pushRunspaces.ToArray() + $pullRunspaces.ToArray()
        foreach($currentRunspace in $currentRunspaces) {
            if(!$currentRunspace.Status.IsCompleted) { continue }
                
            $response = $null#$currentRunspace.Pipe.EndInvoke($currentRunspace.Status)
            if($currentRunspace.Operation -eq "Pull" -and $currentRunspace.Skip) {
                [System.Threading.Interlocked]::Increment([ref] $totalCounter) > $null
                $currentRunspace.Pipe.Dispose()
                $pullRunspaces.Remove($currentRunspace)
            } else {
                $response = $currentRunspace.Pipe.EndInvoke($currentRunspace.Status)
                if($currentRunspace.Operation -eq "Pull") {
                    [System.Threading.Interlocked]::Increment([ref] $totalCounter) > $null
                    [System.Threading.Interlocked]::Increment([ref] $pullCounter) > $null
                } elseif ($currentRunspace.Operation -eq "Push") {
                    [System.Threading.Interlocked]::Increment([ref] $pushCounter) > $null
                }
                Write-Host "[$($currentRunspace.Operation)] $($currentRunspace.Id) completed" -ForegroundColor Gray
                Write-Host "- Processed in $(([datetime]::Now - $currentRunspace.Time))" -ForegroundColor Gray
            }
            if($currentRunspace.Operation -eq "Push") {
                if(![string]::IsNullOrEmpty($response)) {
                    $feedback = $response | ConvertFrom-Json
                    Write-Host "- Imported $($feedback.ImportedItems)/$($feedback.TotalItems) items in destination" -ForegroundColor Gray
                }
                if($pushedLookup.Contains($currentRunspace.Id)) {
                    $queuedItems = $pushedLookup[$currentRunspace.Id]
                    if($queuedItems.Count -gt 0) {
                        foreach($queuedItem in $queuedItems) {
                            $itemId = $queuedItem.ItemId
                            $revisionId = $queuedItem.RevisionId
                            Write-Host "[Push] $($itemId)" -ForegroundColor Green
                            $runspaceProps = @{
                                ScriptBlock = $destinationScript
                                Pool = $pushPool
                                Session = $DestinationSession
                                Arguments = @($yaml,$revisionId)
                            }

                            $runspace = New-PowerShellRunspace @runspaceProps  
                            $pushRunspaces.Add([PSCustomObject]@{ Operation = "Push"; Pipe = $runspace; Status = $runspace.BeginInvoke(); Id = $itemId; ParentId = $currentRunspace.Id; RevisionId = $revisionId; Time = [datetime]::Now; }) > $null
                            $pushedLookup.Add($itemId, [System.Collections.ArrayList]@()) > $null
                        }
                    }
                }
                $pushedLookup.Remove($currentRunspace.Id) > $null

                $currentRunspace.Pipe.Dispose()
                $pushRunspaces.Remove($currentRunspace)
            }

            if($currentRunspace.Operation -eq "Pull") {
                if(![string]::IsNullOrEmpty($response) -and [regex]::IsMatch($response,"^---")) {               
                    $yaml = $response
                    if($pushedLookup.Contains($currentRunspace.ParentId)) {
                        Write-Host "[Queue] $($currentRunspace.ParentId) children" -ForegroundColor Cyan
                        $pushedLookup[$currentRunspace.ParentId].Add([QueueItem]@{"ItemId"=$currentRunspace.Id;"RevisionId"=$currentRunspace.RevisionId;"Yaml"=$yaml;}) > $null
                    } else {                    
                        Write-Host "[Push] $($currentRunspace.Id)" -ForegroundColor Green
                        $runspaceProps = @{
                            ScriptBlock = $destinationScript
                            Pool = $pushPool
                            Session = $DestinationSession
                            Arguments = @($yaml,$currentRunspace.RevisionId)
                        }
                        $runspace = New-PowerShellRunspace @runspaceProps  
                        $pushRunspaces.Add([PSCustomObject]@{ Operation = "Push"; Pipe = $runspace; Status = $runspace.BeginInvoke(); Id = $currentRunspace.Id; ParentId = $currentRunspace.ParentId; RevisionId = $currentRunspace.RevisionId; Time = [datetime]::Now; }) > $null
                        $pushedLookup.Add($currentRunspace.Id, [System.Collections.ArrayList]@()) > $null
                    }
                }

                if($sourceTree.Contains($currentRunspace.Id)) {
                    $shallowItems = $sourceTree[$currentRunspace.Id]
                    foreach($shallowItem in $shallowItems) {
                        $itemId = $shallowItem.ItemId
                        $parentId = $shallowItem.ParentId
                        $revisionId = $shallowItem.RevisionId
                        if($skipExisting -and $skipItemsHash.Contains($itemId) -or 
                            ($compareRevision -and $destinationItemsHash.Contains($itemId) -and $sourceItemRevisionLookup[$itemId] -eq $destinationItemRevisionLookup[$itemId])) {
                            Write-Host "[Skip] $($itemId)" -ForegroundColor Cyan
                            $runspaceProps = @{
                                ScriptBlock = {}
                                Pool = $pullPool
                                Session = $SourceSession
                                Arguments = @($itemId)
                            }
                            $runspace = New-PowerShellRunspace @runspaceProps
                            $pullRunspaces.Add([PSCustomObject]@{ Skip = $true; Operation = "Pull"; Pipe = $runspace; Status = [PSCustomObject]@{"IsCompleted"=$true}; Id = $itemId; ParentId = $parentId; RevisionId = $revisionId; Time = [datetime]::Now; }) > $null
                        } else {                            
                            Write-Host "[Pull] $($itemId)" -ForegroundColor Green
                            $runspaceProps = @{
                                ScriptBlock = $sourceScript
                                Pool = $pullPool
                                Session = $SourceSession
                                Arguments = @($itemId)
                            }
                            $runspace = New-PowerShellRunspace @runspaceProps
                            $pullRunspaces.Add([PSCustomObject]@{ Operation = "Pull"; Pipe = $runspace; Status = $runspace.BeginInvoke(); Id = $itemId; ParentId = $parentId; RevisionId = $revisionId; Time = [datetime]::Now; }) > $null
                        }
                    }
                }

                $currentRunspace.Pipe.Dispose()
                $pullRunspaces.Remove($currentRunspace)
            }
        }
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
            Write-Host "- Removing items from destination not in source"
            $itemsNotInSource = $removeItemsHash -join "|"
            $removeNotInSourceScript = {
                $itemsNotInSource = "{ITEM_IDS}"
                $itemsNotInSourceIds = ($itemsNotInSource).Split("|", [System.StringSplitOptions]::RemoveEmptyEntries)
                foreach($itemId in $itemsNotInSourceIds) {
                    Get-Item -Path "master:" -ID $itemId -ErrorAction 0 | Remove-Item -Recurse
                }
            }
            $removeNotInSourceScript = [scriptblock]::Create($removeNotInSourceScript.ToString().Replace("{ITEM_IDS}", $itemsNotInSource))
            Invoke-RemoteScript -ScriptBlock $removeNotInSourceScript -Session $DestinationSession -Raw
            Write-Host "- Removed $($removeItemsHash.Count) item(s) from the destination"
        }
    }

    $watch.Stop()
    $totalSeconds = $watch.ElapsedMilliseconds / 1000
    Write-Host "[Done] Completed in $($totalSeconds) seconds" -ForegroundColor Yellow
    Write-Progress -Activity "[Done] Completed in $($totalSeconds) seconds" -Completed
    if($totalCounter -gt 0) {
        Write-Host "- Processed count: $($totalCounter)"
        Write-Host " - Pull count: $($pullCounter)"
        Write-Host " - Push count: $($pushCounter)"
    }
}
