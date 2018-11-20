Import-Module -Name SPE -Force

$protocolHost = "https://spe.dev.local"

$watch = [System.Diagnostics.Stopwatch]::StartNew()
$session = New-ScriptSession -Username "sitecore\admin" -Password "b" -ConnectionUri $protocolHost

$pool = [RunspaceFactory]::CreateRunspacePool(1, [int]$env:NUMBER_OF_PROCESSORS)
#$pool.ApartmentState = "MTA"
$pool.Open()
$runspaces = [System.Collections.ArrayList]@()

$scriptblock = {
    param(
        $Session
    )
    Invoke-RemoteScript -Session $session -ScriptBlock { 
        #$itemYaml = Get-ChildItem -Path "master:\content\home" | ConvertTo-RainbowYaml
        #$rainbowItem = [regex]::Split($itemYaml, "(?=---)") | 
        #    Where-Object { ![string]::IsNullOrEmpty($_) } | ConvertFrom-RainbowYaml
        Get-Location
    }
}

function New-Runspace {

    $runspace = [PowerShell]::Create()
    $null = $runspace.AddScript($scriptblock)
    $null = $runspace.AddArgument($Session)
    $runspace.RunspacePool = $pool
    $runspace
}

$initialRunspace = New-Runspace

$runspaces.Add([PSCustomObject]@{ Pipe = $initialRunspace; Status = $initialRunspace.BeginInvoke() }) > $null

$count = 0
while ($runspaces.Count -gt 0) {
    $currentRunspaces = $runspaces.ToArray()
    $currentRunspaces | ForEach-Object { 
        $currentRunspace = $_
        if($currentRunspace.Status.IsCompleted) {
            if($count -lt 10) {
                $runspace = New-Runspace
                $count++
                $runspaces.Add([PSCustomObject]@{ Pipe = $runspace; Status = $runspace.BeginInvoke() }) > $null
            }
            $results = $currentRunspace.Pipe.EndInvoke($currentRunspace.Status)
            $currentRunspace.Pipe.Dispose()
            $runspaces.Remove($currentRunspace)
            $results
        }
    }
}
    
$pool.Close() 
$pool.Dispose()

$watch.Stop()
$watch.ElapsedMilliseconds / 1000