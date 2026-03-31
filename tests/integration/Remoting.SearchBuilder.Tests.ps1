# Integration tests for SearchBuilder extension library
# Requires a running Sitecore instance with SPE and a populated search index.
# Run via: Run-RemotingTests.ps1 -TestFile Remoting.SearchBuilder.Tests.ps1

Write-Host "`n  [SearchBuilder - Setup]" -ForegroundColor White

$session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $global:sharedSecret -ConnectionUri $protocolHost

# Verify SearchBuilder can be loaded
$loadResult = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name SearchBuilder
    $sb = New-SearchBuilder -Index "sitecore_master_index"
    if ($sb._IsSearchBuilder) { "OK" } else { "FAIL" }
} -Raw 2>$null

if ($loadResult -ne "OK") {
    Skip-Test "SearchBuilder integration tests" "Import-Function -Name SearchBuilder failed (not deployed?)"
    Stop-ScriptSession -Session $session
    return
}

# ============================================================
# Basic search - Find-Item via Invoke-Search
# ============================================================
Write-Host "`n  [Invoke-Search - basic query]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name SearchBuilder

    $search = New-SearchBuilder -Index "sitecore_master_index" -First 5
    $search | Add-TemplateFilter -Name "Template"
    $results = $search | Invoke-Search

    @{
        ItemCount  = $results.Items.Count
        HasMore    = $results.HasMore
        PageNumber = $results.PageNumber
        PageSize   = $results.PageSize
        IndexName  = $results.IndexName
    }
}

Assert-True ($result.ItemCount -gt 0) "Basic query returns items"
Assert-True ($result.ItemCount -le 5) "Results capped at First=5"
Assert-Equal $result.PageNumber 1 "First page is 1"
Assert-Equal $result.PageSize 5 "PageSize matches First"
Assert-Equal $result.IndexName "sitecore_master_index" "IndexName is set"

# ============================================================
# Pagination - auto-advance
# ============================================================
Write-Host "`n  [Invoke-Search - pagination]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name SearchBuilder

    $search = New-SearchBuilder -Index "sitecore_master_index" -First 3
    $search | Add-TemplateFilter -Name "Template"

    $page1 = $search | Invoke-Search
    $page2 = $search | Invoke-Search

    @{
        Page1Count  = $page1.Items.Count
        Page1Number = $page1.PageNumber
        Page2Count  = $page2.Items.Count
        Page2Number = $page2.PageNumber
        Page1HasMore = $page1.HasMore
        Skip        = $search._Skip
    }
}

Assert-Equal $result.Page1Number 1 "First page number is 1"
Assert-Equal $result.Page2Number 2 "Second page number is 2"
Assert-True ($result.Page1Count -gt 0) "Page 1 has items"
Assert-True ($result.Skip -gt 0) "Skip auto-advanced"

# ============================================================
# Pagination - Reset
# ============================================================
Write-Host "`n  [Reset-SearchBuilder]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name SearchBuilder

    $search = New-SearchBuilder -Index "sitecore_master_index" -First 3
    $search | Add-TemplateFilter -Name "Template"

    $search | Invoke-Search > $null
    $search | Invoke-Search > $null
    $skipBefore = $search._Skip
    $pageBefore = $search._PageNumber

    $search | Reset-SearchBuilder
    @{
        SkipBefore  = $skipBefore
        PageBefore  = $pageBefore
        SkipAfter   = $search._Skip
        PageAfter   = $search._PageNumber
    }
}

Assert-True ($result.SkipBefore -gt 0) "Skip was advanced before reset"
Assert-Equal $result.SkipAfter 0 "Skip is 0 after reset"
Assert-Equal $result.PageAfter 0 "PageNumber is 0 after reset"

# ============================================================
# OR group - template filter group
# ============================================================
Write-Host "`n  [Add-SearchFilterGroup - OR templates]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name SearchBuilder

    $search = New-SearchBuilder -Index "sitecore_master_index" -First 10
    $group = New-SearchFilterGroup -Operation Or
    $group | Add-TemplateFilter -Name "Template"
    $group | Add-TemplateFilter -Name "Template Folder"
    $search | Add-SearchFilterGroup -Group $group
    $results = $search | Invoke-Search

    @{
        ItemCount = $results.Items.Count
        HasItems  = $results.Items.Count -gt 0
    }
}

Assert-True $result.HasItems "OR group query returns items"

# ============================================================
# MaxResults - truncation signal
# ============================================================
Write-Host "`n  [Invoke-Search - MaxResults truncation]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name SearchBuilder

    $search = New-SearchBuilder -Index "sitecore_master_index" -MaxResults 2
    $search | Add-TemplateFilter -Name "Template"
    $results = $search | Invoke-Search

    @{
        ItemCount = $results.Items.Count
        Truncated = $results.Truncated
        HasMore   = $results.HasMore
        MaxResults = $results.MaxResults
    }
}

Assert-True ($result.ItemCount -le 2) "Items capped at MaxResults"
Assert-Equal $result.MaxResults 2 "MaxResults reported in result"
# Truncated should be true if there are more than 2 templates in the index
if ($result.ItemCount -eq 2) {
    Assert-True $result.Truncated "Truncated is true when results hit MaxResults"
    Assert-Equal $result.HasMore $false "HasMore is false when truncated"
}

# ============================================================
# Get-SearchFilter - runtime discovery
# ============================================================
Write-Host "`n  [Get-SearchFilter]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name SearchBuilder

    $filters = @(Get-SearchFilter)
    @{
        Count = $filters.Count
        Names = ($filters | ForEach-Object { $_.Name }) -join ","
        HasDescriptions = ($filters | Where-Object { -not [string]::IsNullOrEmpty($_.Description) }).Count
    }
}

Assert-Equal $result.Count 14 "Returns 14 filter types"
Assert-True ($result.Names -match "Equals") "Contains Equals filter"
Assert-True ($result.Names -match "Contains") "Contains Contains filter"
Assert-True ($result.Names -match "InclusiveRange") "Contains InclusiveRange filter"
Assert-Equal $result.HasDescriptions 14 "All 14 filters have descriptions"

# ============================================================
# Get-SearchIndexField - index field discovery
# ============================================================
Write-Host "`n  [Get-SearchIndexField]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name SearchBuilder

    $fields = @(Get-SearchIndexField -Index "sitecore_master_index")
    @{
        Count     = $fields.Count
        HasFields = $fields.Count -gt 0
        HasTemplateName = ($fields | Where-Object { $_.FieldName -eq "_templatename" }).Count -gt 0
        HasPath   = ($fields | Where-Object { $_.FieldName -eq "_path" }).Count -gt 0
        IndexName = if ($fields.Count -gt 0) { $fields[0].IndexName } else { "" }
    }
}

Assert-True $result.HasFields "Index has fields"
Assert-True ($result.Count -gt 10) "Index has many fields"
Assert-True $result.HasTemplateName "_templatename field exists in index"
Assert-True $result.HasPath "_path field exists in index"
Assert-Equal $result.IndexName "sitecore_master_index" "IndexName set on field objects"

# ============================================================
# Date range filter
# ============================================================
Write-Host "`n  [Add-DateRangeFilter - relative date]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name SearchBuilder

    # Search for items updated in last year (should find something)
    $search = New-SearchBuilder -Index "sitecore_master_index" -First 5
    $search | Add-DateRangeFilter -Field "__smallupdateddate" -Last "1y"
    $results = $search | Invoke-Search

    @{
        ItemCount = $results.Items.Count
        HasItems  = $results.Items.Count -gt 0
    }
}

Assert-True $result.HasItems "Date range filter returns items updated in last year"

# ============================================================
# Query summary
# ============================================================
Write-Host "`n  [Invoke-Search - Query summary]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name SearchBuilder

    $search = New-SearchBuilder -Index "sitecore_master_index" -First 1
    $search | Add-TemplateFilter -Name "Template"
    $search | Add-FieldContains -Field "Title" -Value "Test"
    $results = $search | Invoke-Search
    $results.Query
} -Raw 2>$null

Assert-True (-not [string]::IsNullOrEmpty($result)) "Query summary is not empty"
Assert-Like $result "*_templatename*" "Query summary contains template field"
Assert-Like $result "*AND*" "Query summary contains AND"

# ============================================================
# Paged accumulation loop
# ============================================================
Write-Host "`n  [Invoke-Search - paged accumulation loop]" -ForegroundColor White

$result = Invoke-RemoteScript -Session $session -ScriptBlock {
    Import-Function -Name SearchBuilder

    $search = New-SearchBuilder -Index "sitecore_master_index" -First 3 -MaxResults 9
    $search | Add-TemplateFilter -Name "Template"

    $totalItems = 0
    $pageCount = 0
    do {
        $results = $search | Invoke-Search
        $totalItems += $results.Items.Count
        $pageCount++
    } while ($results.HasMore -and $pageCount -lt 10)

    @{
        TotalItems = $totalItems
        PageCount  = $pageCount
        Truncated  = $results.Truncated
    }
}

Assert-True ($result.TotalItems -gt 0) "Accumulation loop collected items"
Assert-True ($result.PageCount -ge 1) "At least one page processed"
Assert-True ($result.TotalItems -le 9) "Total items capped at MaxResults=9"

# ============================================================
# Cleanup
# ============================================================
Stop-ScriptSession -Session $session
