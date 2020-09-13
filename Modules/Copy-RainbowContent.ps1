#Requires -Modules SPE
 
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

        [Parameter(ParameterSetName='Partial')]
        [Parameter(ParameterSetName='SingleRequest')]
        [switch]$RemoveNotInSource,

        [switch]$Overwrite,

        [Parameter(ParameterSetName='SingleRequest')]
        [switch]$SingleRequest
    )
    
    $threads = $env:NUMBER_OF_PROCESSORS

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

    Write-Host "[Running] Transfer from $($SourceSession.Connection[0].BaseUri) to $($DestinationSession.Connection[0].BaseUri)" -ForegroundColor Yellow

    $sourceScript = {
        param(
            $Session,
            [string]$RootId,
            [bool]$IncludeParent = $true,
            [bool]$IncludeChildren = $false,
            [bool]$RecurseChildren = $false,
            [bool]$SingleRequest = $false
        )

        $serializeParent = $IncludeParent
        $serializeChildren = $IncludeChildren
        $recurseChildren = $RecurseChildren

        $scriptSingleRequest = {
            $parentItem = Get-Item -Path "master:" -ID "{ROOT_ID}"
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
        $scriptSingleRequest = [scriptblock]::Create($scriptSingleRequest.ToString().Replace("{ROOT_ID}",$RootId))

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

        if($SingleRequest) {
            Write-Host "- Performing a single bulk request"
            Invoke-RemoteScript -ScriptBlock $scriptSingleRequest  -Session $Session -Raw
        } else {
            $scriptString = $script.ToString()
            $trueFalseHash = @{$true="`$true";$false="`$false"}
            $scriptString = "`$parentId = '$($RootId)';`$serializeParent = $($trueFalseHash[$serializeParent]);`$serializeChildren = $($trueFalseHash[$serializeChildren]);`$recurseChildren = $($trueFalseHash[$recurseChildren]);" + $scriptString
            $script = [scriptblock]::Create($scriptString)

            Invoke-RemoteScript -ScriptBlock $script -Session $Session -Raw
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

            $rainbowYamlBytes = [System.Convert]::FromBase64String($rainbowYamlBase64)
            $rainbowYaml = [System.Text.Encoding]::UTF8.GetString($rainbowYamlBytes)
            $rainbowItems = [regex]::Split($rainbowYaml, "(?=---$)") | 
                Where-Object { ![string]::IsNullOrEmpty($_) } | ConvertFrom-RainbowYaml
        
            $totalItems = $rainbowItems.Count
            $itemsToImport = [System.Collections.ArrayList]@()
            $totalSkippedItems = 0
            foreach($rainbowItem in $rainbowItems) {
                <#
                # If the comparison of __Revision works then we would always overwrite.
                # Currently the Yaml does not contain __Revision
                if($checkExistingItem) {
                    if((Test-Path -Path "$($rainbowItem.DatabaseName):{$($rainbowItem.Id)}")) { 
                        Write-Log "Skipping $($rainbowItem.Id)"
                        $totalSkippedItems++
                        continue
                    }
                }
                #>
                $itemsToImport.Add($rainbowItem) > $null
            }
            
            $errorMessages = @()
            $itemsToImport | ForEach-Object {                
                try {
                    Import-RainbowItem -Item $_
                } catch {
                    Write-Log "Importing $($_.Id) failed with error $($Error[0].Exception.Message)"
                    $errorMessages += "$($_.Id) Failed"
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
        $scriptString = "`$rainbowYamlBase64 = '$([System.Convert]::ToBase64String($rainbowYamlBytes))';`$checkExistingItem = $($trueFalseHash[!$shouldOverwrite]);" + $scriptString
        $script = [scriptblock]::Create($scriptString)

        Invoke-RemoteScript -ScriptBlock $script -Session $Session -Raw
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
    $compareScript = {
        $rootId = "{ROOT_ID}"
        $recurseChildren = [bool]::Parse("{RECURSE_CHILDREN}")
        <#
        $rootItem = Get-Item -Path "master:" -ID $rootId -ErrorAction 0
        if($rootItem) {
            $items = [System.Collections.ArrayList]@()
            $items.Add($rootItem) > $null
            if($recurseChildren) {
                $children = $rootItem.Axes.GetDescendants()
                if($children.Count -gt 0) {
                    $items.AddRange($children) > $null
                }
            }
            $itemIds = $items | ForEach-Object { "I:$($_.ID)+R:{$($_.Fields["__Revision"].Value)}" }
            $itemIds -join "|"
        }
        #>
        Import-Function -Name Invoke-SqlCommand
        $connection = [Sitecore.Configuration.Settings]::GetConnectionString("master")

        $revisionFieldId = "{8CDC337E-A112-42FB-BBB4-4143751E123F}"
        if($recurseChildren) {
            $query = "
                WITH [ContentQuery] AS (SELECT [ID], [Name] FROM [dbo].[Items] WHERE ID='$($rootId)' UNION ALL SELECT  i.[ID], i.[Name] FROM [dbo].[Items] i INNER JOIN [ContentQuery] ci ON ci.ID = i.[ParentID])
                SELECT cq.[ID], vf.[Value] AS [Revision] FROM [ContentQuery] cq INNER JOIN dbo.[VersionedFields] vf ON cq.[ID] = vf.[ItemId] WHERE vf.[FieldId] = '$($revisionFieldId)'
            "
        } else {
            $query = "
                WITH [ContentQuery] AS (SELECT [ID], [Name] FROM [dbo].[Items] WHERE ID='$($rootId)')
                SELECT cq.[ID], vf.[Value] AS [Revision] FROM [ContentQuery] cq INNER JOIN dbo.[VersionedFields] vf ON cq.[ID] = vf.[ItemId] WHERE vf.[FieldId] = '$($revisionFieldId)'
            "
        }
        $records = Invoke-SqlCommand -Connection $connection -Query $query
        if($records) {
            $itemIds = $records | ForEach-Object { "I:{$($_.ID)}+R:{$($_."Revision")}" }
            $itemIds -join "|"
        }
    }
    $compareScript = [scriptblock]::Create($compareScript.ToString().Replace("{ROOT_ID}", $RootId).Replace("{RECURSE_CHILDREN}", $recurseChildren -or $RemoveNotInSource))
    function Parse-Id {
        param(
            [string]$Text
        )

        $guidPattern = "{[0-9A-Fa-f]{8}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{12}}"
        if([regex]::IsMatch($Text, "^$($guidPattern)$")) {
            $Text
        } else {
            $pattern = "^I:(?<guid>$($guidPattern))"
            $matchedPattern = [regex]::Match($Text, $pattern)
            if($matchedPattern.Success) {
                $matchedPattern.Groups["guid"].Value
            }
        }
    }
    function Compare-Id {
        param(
            [string]$ReferenceString,
            [string]$DifferenceString,
            [switch]$IgnoreRevision
        )

        $referenceIds = $ReferenceString.Split("|", [System.StringSplitOptions]::RemoveEmptyEntries)
        $differenceIds = $DifferenceString.Split("|", [System.StringSplitOptions]::RemoveEmptyEntries)
        if($IgnoreRevision) {
            $referenceIds = $referenceIds | ForEach-Object { Parse-Id -Text $_ }
            $differenceIds = $differenceIds | ForEach-Object { Parse-Id -Text $_ }
        }
        $queueIds = Compare-Object -ReferenceObject $referenceIds -DifferenceObject $differenceIds | 
            Where-Object { $_.SideIndicator -eq "<=" } | Select-Object -ExpandProperty InputObject |
            ForEach-Object { Parse-Id -Text $_ } | Where-Object { ![string]::IsNullOrEmpty($_) }
        ,$queueIds
    }

    if(!$SingleRequest.IsPresent -and !$Overwrite.IsPresent) {
        Write-Host "- Preparing to compare source and destination instances using ID $($RootId)"

        Write-Host " - Getting list of IDs from source"
        $sourceItemIds = Invoke-RemoteScript -Session $SourceSession -ScriptBlock $compareScript -Raw

        $queueIds = @()
        if($sourceItemIds) {
            Write-Host " - Getting list of IDs from destination"
            $destinationItemIds = Invoke-RemoteScript -Session $DestinationSession -ScriptBlock $compareScript -Raw
            if($destinationItemIds) {
                Write-Host " - Comparing source with destination items"
                $queueIds = Compare-Id -ReferenceString $sourceItemIds -DifferenceString $destinationItemIds

                foreach($queueId in $queueIds) {
                    $queue.Enqueue($queueId)
                }

                if($queue.Count -ge 1) {
                    $serializeParent = $true
                    $serializeChildren = $false
                    $recurseChildren = $false

                    $threads = 1
                } else {
                    Write-Host "- No items need to be transferred because they already exist"
                }
            } else {
                Write-Host " - Queueing $($RootId) as no destination items previously exist"
                $queue.Enqueue($rootId)
            }
        } else {
            Write-Host " - Skipping $($RootId) as no source item exists with that Id" -ForegroundColor White -BackgroundColor Red
        }
    } else {
        Write-Host " - Queueing $($RootId)"
        $queue.Enqueue($rootId)
    }

    if($RemoveNotInSource.IsPresent) {
        Write-Host "- Checking destination for items not in source"
        $sourceItemIds = Invoke-RemoteScript -Session $SourceSession -ScriptBlock $compareScript -Raw
        if($sourceItemIds) {
            $destinationItemIds = Invoke-RemoteScript -Session $DestinationSession -ScriptBlock $compareScript -Raw
            if($destinationItemIds) {
                $itemsNotInSourceIds = Compare-Id -ReferenceString $destinationItemIds -DifferenceString $sourceItemIds -IgnoreRevision
                
                if($itemsNotInSourceIds) {
                    Write-Host "- Removing items from destination not in source"
                    $itemsNotInSource = $itemsNotInSourceIds -join "|"
                    $removeNotInSourceScript = {
                        $itemsNotInSource = "{ITEM_IDS}"
                        $itemsNotInSourceIds = ($itemsNotInSource).Split("|", [System.StringSplitOptions]::RemoveEmptyEntries)
                        foreach($itemId in $itemsNotInSourceIds) {
                            Get-Item -Path "master:" -ID $itemId -ErrorAction 0 | Remove-Item -Recurse
                        }
                    }
                    $removeNotInSourceScript = [scriptblock]::Create($removeNotInSourceScript.ToString().Replace("{ITEM_IDS}", $itemsNotInSource))
                    Invoke-RemoteScript -ScriptBlock $removeNotInSourceScript -Session $DestinationSession -Raw
                    Write-Host "- Removed $($itemsNotInSourceIds.Count) item(s) from the destination"
                }
            }
        }

        Write-Host " - Removal complete"
    }

    $processedAny = $false
    if($queue.Count -gt 0) {
        $processedAny = $true
        Write-Host "Spinning up jobs to transfer content" -ForegroundColor Yellow
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
                    Arguments = @($itemId,$true,$serializeChildren,$recurseChildren,$SingleRequest)
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
    }
    $watch.Stop()
    $totalSeconds = $watch.ElapsedMilliseconds / 1000

    Write-Host "[Done] Completed in $($totalSeconds) seconds" -ForegroundColor Yellow
    if($processedAny) {
        Write-Host "- Copied $($totalCounter) items"
        Write-Host "- Pull count: $($pullCounter)"
        Write-Host "- Push count: $($pushCounter)"
    }
}
