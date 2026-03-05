Clear-Host

Import-Module -Name SPE -Force

$session = New-ScriptSession -Username "sitecore\admin" -Password "b" -ConnectionUri "https://spe.dev.local"
$watch = [System.Diagnostics.Stopwatch]::StartNew()

$tasks = New-Object System.Collections.Generic.List[System.Threading.Tasks.Task]
$initialScript = {
    #$rootId = "{371EEE15-B6F3-423A-BB25-0B5CED860EEA}"
    $rootId = "{37D08F47-7113-4AD6-A5EB-0C0B04EF6D05}"
    $builder = New-Object System.Text.StringBuilder
                
    $parentItem = Get-Item -Path "master:" -ID $rootId
    $parentYaml = $parentItem | ConvertTo-RainbowYaml
    $builder.AppendLine($parentYaml) > $null
                
    $childItems = Get-ChildItem -Path "master:" -ID $rootId
    foreach($childItem in $childItems) {
        $childYaml = $childItem | ConvertTo-RainbowYaml
        $builder.AppendLine($childYaml) > $null
    }
                   
    $itemIds = $childItems | Where-Object { $_.HasChildren } | Select-Object -ExpandProperty ID
    if($itemIds) {
        $builder.Append("<#split#>") > $null
        $builder.Append($itemIds -join "|") > $null
    }

    $builder.ToString()
}

$taskRoot = Invoke-RemoteScriptAsync -Session $session -ScriptBlock $initialScript -Raw
$tasks.Add($taskRoot) > $null

function Process-Response {
    param(
        [string]$Response
    )

    if([string]::IsNullOrEmpty($response)) { return }
    if(([regex]::Matches($response, "<#split#>" )).count -gt 1) {
        Write-Host "Skipping because I can't parse this" -ForegroundColor Red
        Write-Host $Response
        return
    }

    $split = $response -split "<#split#>"
    $yaml = $split[0]

    if($split.Length -eq 1 -or [string]::IsNullOrEmpty($split[1])) { return }

    $itemIdsToProcess = $split[1].Split("|", [System.StringSplitOptions]::RemoveEmptyEntries)
    if($itemIdsToProcess -and $itemIdsToProcess.Count -gt 0) {
        $tasksToAdd = New-Object System.Collections.Generic.List[System.Threading.Tasks.Task]            
        Write-Host "[Pull] Processing $($itemIdsToProcess.Count) items" -ForegroundColor Green
        foreach($itemIdToProcess in $itemIdsToProcess) {
            Write-Host "- $($itemIdToProcess)"

            $script = {

                $builder = New-Object System.Text.StringBuilder
                               
                $childItems = Get-ChildItem -Path "master:" -ID $rootId
                foreach($childItem in $childItems) {
                    $childYaml = $childItem | ConvertTo-RainbowYaml
                    $builder.AppendLine($childYaml) > $null
                }
                   
                $itemIds = @($childItems | Where-Object { $_.HasChildren } | Select-Object -ExpandProperty ID)
                if($itemIds -and $itemIds.Count -gt 0) {
                    $builder.Append("<#split#>") > $null
                    $builder.Append($itemIds -join "|") > $null
                }

                $builder.ToString()
            }

            $scriptString = "`$rootId = ""$($itemIdToProcess)""`n" + $script.ToString()
            $script = [scriptblock]::Create($scriptString)
            $taskToAdd = Invoke-RemoteScriptAsync -Session $session -ScriptBlock $script -Raw
            $tasksToAdd.Add($taskToAdd) > $null
        }
        
        ,$tasksToAdd         
    }
}

while($tasks.Count -gt 0) {
    $taskCompleted = [System.Threading.Tasks.Task]::WhenAny($tasks.ToArray())
    foreach($task in $tasks.ToArray()) {
        if($task.Status -ne [System.Threading.Tasks.TaskStatus]::RanToCompletion) { continue }
        $tasks.Remove($task) > $null
        $newTasks = Process-Response -Response $task.Result
        if($newTasks) {
            foreach($newTask in $newTasks) {
                $tasks.Add($newTask) > $null
            }
        }
    }
}

$watch.Stop()
$watch.ElapsedMilliseconds / 1000