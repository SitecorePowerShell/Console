Import-Module -Name "SPE" -Force

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

    Write-Host "Preparing to transfer items between $($Source) and $($Destination)" -ForegroundColor Yellow
    $localSession = New-ScriptSession -user $Username -pass $Password -conn $Source
    $remoteSession = New-ScriptSession -user $Username -pass $Password -conn $Destination

    $parentId = $RootId
    $shouldRecurse = $Recurse.IsPresent
    $shouldOverwrite =$Overwrite.IsPresent

    Write-Host "- Fetching items from source" -ForegroundColor Green
    Write-Host " - Recurse items: $($shouldRecurse)"
    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    $rainbowYaml = Invoke-RemoteScript -ScriptBlock {
        if($using:shouldRecurse) {
            Get-ChildItem -Path "master:" -ID $using:parentId -Recurse -WithParent | ConvertTo-RainbowYaml
        } else {
            Get-Item -Path "master:" -ID $using:parentId | ConvertTo-RainbowYaml
        }        
    } -Session $localSession -Raw
    $watch.Stop()
    Write-Host " - Exported items from source: [$($watch.ElapsedMilliseconds / 1000) seconds]"

    Write-Host "- Sending items to destination" -ForegroundColor Green
    Write-Host " - Overwrite items: $($shouldOverwrite)"
    $watch.Restart()
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
    } -Session $remoteSession
    $watch.Stop()
    Write-Host " - Imported $($feedback.ImportedItems) items out of $($feedback.TotalItems) to destination: [$($watch.ElapsedMilliseconds / 1000) seconds]"
    Write-Host "Completed transferring items between source and destination instances" -ForegroundColor Gray
    Write-Host "---"
}

$copyProps = @{
    Source = "https://spe.dev.local"
    Destination = "http://sc827"
    Username = "admin"
    Password = "b"    
}

# Copy single item
# Copy item with children, recursively
# Copy while checking for revision
# Copy items with updates to specific fields
# Copy items, transform rainbow before import

# Home\Delete Me

# Migrate a single item only if it's missing
Copy-RainbowContent @copyProps -RootId "{A6649F02-B4B6-4985-8FD5-7D40CA9E829F}"

# Migrate all items only if they are missing
#Copy-RainbowContent @copyProps -RootId "{A6649F02-B4B6-4985-8FD5-7D40CA9E829F}" -Recurse

# Migrate a single item and overwrite if it exists
#Copy-RainbowContent @copyProps -RootId "{A6649F02-B4B6-4985-8FD5-7D40CA9E829F}" -Overwrite

# Migrate all items overwriting if they exist
#Copy-RainbowContent @copyProps -RootId "{A6649F02-B4B6-4985-8FD5-7D40CA9E829F}" -Overwrite -Recurse

# Images
#Copy-RainbowContent @copyProps -RootId "{15451229-7534-44EF-815D-D93D6170BFCB}"