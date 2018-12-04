Clear-Host

Import-Module -Name SPE -Force
Add-Type -AssemblyName System.Net.Http

$session = New-ScriptSession -Username "sitecore\admin" -Password "b" -ConnectionUri "https://spe.dev.local"
$watch = [System.Diagnostics.Stopwatch]::StartNew()

function Invoke-RemoteScriptAsync {
    param(
        [pscustomobject]$Session,
        [scriptblock]$ScriptBlock,
        [switch]$Raw
    )

    $Username = $Session.Username
    $Password = $Session.Password
    $SessionId = $Session.SessionId
    $Credential = $Session.Credential
    $UseDefaultCredentials = $Session.UseDefaultCredentials
    $ConnectionUri = $Session | ForEach-Object { $_.Connection.BaseUri }
    $PersistentSession = $Session.PersistentSession

    $serviceUrl = "/-/script/script/?"
    $serviceUrl += "user=" + $Username + "&password=" + $Password + "&sessionId=" + $SessionId + "&rawOutput=" + $Raw.IsPresent + "&persistentSession=" + $PersistentSession

    $handler = New-Object System.Net.Http.HttpClientHandler
    $handler.AutomaticDecompression = [System.Net.DecompressionMethods]::GZip -bor [System.Net.DecompressionMethods]::Deflate
           
    if ($Credential) {
        $handler.Credentials = $Credential
    }

    if($UseDefaultCredentials) {
        $handler.UseDefaultCredentials = $UseDefaultCredentials
    }

    $client = New-Object -TypeName System.Net.Http.Httpclient $handler

    $content = New-Object System.Net.Http.StringContent("$($ScriptBlock.ToString())<#$($SessionId)#>$($localParams)", [System.Text.Encoding]::UTF8)
    foreach($uri in $ConnectionUri) {
        $url = $uri.AbsoluteUri.TrimEnd("/") + $serviceUrl

        $taskPost = $client.PostAsync($url, $content)
        $continuation = New-RunspacedDelegate ([Func[System.Threading.Tasks.Task[System.Net.Http.HttpResponseMessage], PSObject]] { 
            param($t) 
            $t.Result.Content.ReadAsStringAsync().Result
        })
        Invoke-GenericMethod -InputObject $taskPost -MethodName ContinueWith -GenericType PSObject -ArgumentList $continuation
    }
}

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
    $split = $response -split "<#split#>"
    $yaml = $split[0]

    if($split.Length -eq 1 -or [string]::IsNullOrEmpty($split[1])) { return }

    $itemIdsToProcess = $split[1].Split("|", [System.StringSplitOptions]::RemoveEmptyEntries)
    if($itemIdsToProcess -and $itemIdsToProcess.Count -gt 0) {
        $tasksToAdd = New-Object System.Collections.Generic.List[System.Threading.Tasks.Task]            
        Write-Host "[Pull] Processing $($itemIdsToProcess.Count) items" -ForegroundColor Green
        foreach($itemIdToProcess in $itemIdsToProcess) {
            Write-Host "- $($itemIdToProcess)"
            $parsedGuid = [guid]::Empty
            if(![guid]::TryParse($itemIdToProcess,[ref]$parsedGuid)) {
                Write-Error $itemIdToProcess
            }

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
            $tasks.AddRange($newTasks) > $null
        }
    }
}

$watch.Stop()
$watch.ElapsedMilliseconds / 1000