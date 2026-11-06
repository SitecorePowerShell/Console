Clear-Host
$watch = [System.Diagnostics.Stopwatch]::StartNew()

$Username = "admin"
$Password = "b"
$Source = "https://spe.dev.local"
$Destination = "http://sc827"

$localSession = New-ScriptSession -user $Username -pass $Password -conn $Source
$remoteSession = New-ScriptSession -user $Username -pass $Password -conn $Destination


$rootId = "{37D08F47-7113-4AD6-A5EB-0C0B04EF6D05}"

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

$watch.Stop()
$totalSeconds = $watch.ElapsedMilliseconds / 1000
Write-Host "$($totalSeconds) seconds" -ForegroundColor Yellow