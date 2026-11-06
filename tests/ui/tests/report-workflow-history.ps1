Clear-Host

# Runs the Recent workflow history report against fixture items with its dialogs stubbed out.
$testRoot = "master:\content\spe-test"
if (Test-Path $testRoot) {
    Remove-Item -Path $testRoot -Permanently -Recurse
}
New-Item -Path "master:\content" -Name "spe-test" -ItemType "Common/Folder" | Out-Null

function Read-Variable {
    foreach ($name in $global:wfhAnswers.Keys) {
        Set-Variable -Name $name -Value $global:wfhAnswers[$name] -Scope 1
    }
    "ok"
}
function Show-ListView { $global:wfhRows = @($input) }
function Show-Alert { $global:wfhAlert = "$args" }
function Close-Window { }

function Invoke-Report([hashtable]$Answers) {
    $global:wfhAnswers = $Answers
    $global:wfhRows = @()
    $global:wfhAlert = $null
    Invoke-Script -Item $report | Out-Null
}

function Add-Event($Item, $OldState, $NewState, $Text) {
    New-ItemWorkflowEvent -Item $Item -OldState $OldState -NewState $NewState -Text $Text
    # Keeps the entries apart so their order is deterministic
    Start-Sleep -Milliseconds 50
}

try {
    $stateA = New-Item -Path $testRoot -Name "State A" -ItemType "Common/Folder"
    $stateB = New-Item -Path $testRoot -Name "State B" -ItemType "Common/Folder"
    $page = New-Item -Path $testRoot -Name "Page" -ItemType "Common/Folder"
    $child = New-Item -Path "$testRoot\Page" -Name "Child" -ItemType "Common/Folder"

    Add-Event $page "" $stateA.ID "page v1 draft"
    Add-Event $page $stateA.ID $stateB.ID "page v1 approved"
    $version2 = Add-ItemVersion -Path "$testRoot\Page" | Select-Object -Last 1
    $version2.Editing.BeginEdit()
    $version2["__Hide version"] = "1"
    $version2.Editing.EndEdit() | Out-Null
    Add-Event $version2 "" $stateA.ID "page v2 draft"
    $version3 = Add-ItemVersion -Path "$testRoot\Page" | Select-Object -Last 1
    Add-Event $version3 "" $stateA.ID "page v3 draft"
    Add-Event $child "" $stateA.ID "child draft"
    Remove-ItemVersion -Path "$testRoot\Page" -Language $page.Language.Name -Version 3

    $report = Get-Item -Path "master:" -ID "{53402CF4-BCFF-437D-83E6-B7A6D1BE2BAD}"
    $PSScript = $report
    $page = Get-Item -Path "$testRoot\Page"

    Invoke-Report @{ root = $page; includeDescendants = $false; limitPeriod = $false }
    "ItemNewestFirst=" + ((($global:wfhRows | ForEach-Object { $_.Comments }) -join ",") -eq "page v3 draft,page v2 draft,page v1 approved,page v1 draft")
    $removed = @($global:wfhRows | Where-Object { $_.Version -eq 3 -and -not $_.VersionExists })
    "RemovedVersionListed=" + ($removed.Count -eq 1)
    "ExistingVersionFlagged=" + (@($global:wfhRows | Where-Object { $_.Version -eq 1 -and $_.VersionExists }).Count -eq 2)
    "StateNames=" + ("$($global:wfhRows[2].OldState)>$($global:wfhRows[2].NewState)" -eq "State A>State B")
    # Bookkeeping fields like __Updated and __Revision differ between versions too but are left out
    "ChangedFields=" + ((($global:wfhRows | ForEach-Object { "$($_.ChangedFields)" }) -join "|") -eq "|__Hide version|(first version)|(first version)")

    Invoke-Report @{ root = $page; includeDescendants = $true; limitPeriod = $false }
    "BranchNewestFirst=" + ((($global:wfhRows | ForEach-Object { $_.Comments }) -join ",") -eq "child draft,page v3 draft,page v2 draft,page v1 approved,page v1 draft")

    Invoke-Report @{ root = $page; includeDescendants = $true; limitPeriod = $true; fromDate = [datetime]"2000-01-01"; toDate = [datetime]"2000-01-02" }
    "PastPeriodEmpty=" + ($global:wfhRows.Count -eq 0 -and $global:wfhAlert -like "There is no workflow history for*")

    # A window around local now only matches if the period is converted to UTC like the stored dates
    Invoke-Report @{ root = $page; includeDescendants = $true; limitPeriod = $true; fromDate = [datetime]::Now.AddMinutes(-10); toDate = [datetime]::Now.AddMinutes(10) }
    "CurrentPeriodCount=" + $global:wfhRows.Count
} finally {
    Remove-Item -Path Function:\Read-Variable, Function:\Show-ListView, Function:\Show-Alert, Function:\Close-Window -ErrorAction SilentlyContinue
    Remove-Variable -Name wfhAnswers, wfhRows, wfhAlert -Scope Global -ErrorAction SilentlyContinue
    $fixtureIds = @($page, $child) | Where-Object { $_ } | ForEach-Object { "'$($_.ID.Guid)'" }
    if ($fixtureIds) {
        # Deleting the items leaves their workflow history behind
        Import-Function -Name Invoke-SqlCommand
        $connection = [Sitecore.Configuration.Settings]::GetConnectionString("master")
        Invoke-SqlCommand -Connection $connection -Query "DELETE FROM WorkflowHistory WHERE ItemID IN ($($fixtureIds -join ','))" -As NonQuery | Out-Null
    }
    if (Test-Path $testRoot) {
        Remove-Item -Path $testRoot -Permanently -Recurse
    }
}

"Done!"
