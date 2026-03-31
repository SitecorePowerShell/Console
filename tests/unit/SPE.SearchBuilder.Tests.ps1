# Unit tests for SearchBuilder extension library
# Requires TestRunner.ps1 to be dot-sourced first
#
# These tests validate the builder functions in isolation without a Sitecore instance.
# The SearchBuilder functions are loaded by dot-sourcing the script content from the
# serialized YAML item.

# Load SearchBuilder functions from the YAML script block
$yamlPath = "$PSScriptRoot\..\..\serialization\modules\serialization\SPE\SPE\Extensions\Search Builder\Functions\SearchBuilder.yml"
if (-not (Test-Path $yamlPath)) {
    Write-Host "  [SKIP] SearchBuilder YAML not found at: $yamlPath" -ForegroundColor Yellow
    return
}

# Extract the PowerShell script content from the YAML Value block
$yamlContent = Get-Content $yamlPath -Raw
$scriptStart = $yamlContent.IndexOf("  Value: |") + "  Value: |".Length
$scriptEnd = $yamlContent.IndexOf("`nLanguages:")
if ($scriptEnd -lt 0) { $scriptEnd = $yamlContent.IndexOf("`r`nLanguages:") }
$scriptBlock = $yamlContent.Substring($scriptStart, $scriptEnd - $scriptStart)
# Remove the 4-space YAML indentation from each line
$scriptLines = $scriptBlock -split "`r?`n" | ForEach-Object {
    if ($_.StartsWith("    ")) { $_.Substring(4) } else { $_ }
}
$cleanScript = $scriptLines -join "`n"

# Define a mock New-PSObject for unit tests (no Sitecore dependency)
function global:New-PSObject {
    param([Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true)][hashtable]$Property)
    $result = New-Object PSObject
    foreach ($key in $Property.Keys) {
        $result | Add-Member -MemberType NoteProperty -Name $key -Value $Property[$key]
    }
    return $result
}

# Execute the script to define all functions
Invoke-Expression $cleanScript

# ============================================================
# New-SearchBuilder
# ============================================================
Write-Host "`n  [New-SearchBuilder]" -ForegroundColor White

$builder = New-SearchBuilder -Index "sitecore_master_index"
Assert-True ($builder -is [hashtable]) "Returns a hashtable"
Assert-Equal $builder._Index "sitecore_master_index" "Index is set"
Assert-Equal $builder._First 0 "First defaults to 0"
Assert-Equal $builder._Skip 0 "Skip defaults to 0"
Assert-Equal $builder._Last 0 "Last defaults to 0"
Assert-Equal $builder._MaxResults 0 "MaxResults defaults to 0"
Assert-Equal $builder._Strict $false "Strict defaults to false"
Assert-Equal $builder._IncludeMetadata $false "IncludeMetadata defaults to false"
Assert-Equal $builder._PageNumber 0 "PageNumber starts at 0"
Assert-Equal $builder._Criteria.Count 0 "Criteria list starts empty"
Assert-True $builder._IsSearchBuilder "IsSearchBuilder flag is set"

$builder2 = New-SearchBuilder -Index "test_index" -Path "/sitecore/content" -OrderBy "title" -First 25 -Skip 10 -MaxResults 100 -Strict -IncludeMetadata
Assert-Equal $builder2._Path "/sitecore/content" "Path is set"
Assert-Equal $builder2._OrderBy "title" "OrderBy is set"
Assert-Equal $builder2._First 25 "First is set"
Assert-Equal $builder2._Skip 10 "Skip is set"
Assert-Equal $builder2._MaxResults 100 "MaxResults is set"
Assert-Equal $builder2._Strict $true "Strict is set"
Assert-Equal $builder2._IncludeMetadata $true "IncludeMetadata is set"

# ============================================================
# Add-SearchFilter
# ============================================================
Write-Host "`n  [Add-SearchFilter]" -ForegroundColor White

$builder = New-SearchBuilder -Index "test_index"
Add-SearchFilter -SearchBuilder $builder -Field "Title" -Filter "Contains" -Value "Welcome"
Assert-Equal $builder._Criteria.Count 1 "One criterion added"
Assert-Equal $builder._Criteria[0].Field "Title" "Field is set"
Assert-Equal $builder._Criteria[0].Filter "Contains" "Filter is set"
Assert-Equal $builder._Criteria[0].Value "Welcome" "Value is set"
Assert-True (-not $builder._Criteria[0].ContainsKey('Invert')) "Invert not set when omitted"
Assert-True (-not $builder._Criteria[0].ContainsKey('Boost')) "Boost not set when 0"

Add-SearchFilter -SearchBuilder $builder -Field "Status" -Filter "Equals" -Value "Published" -Invert -Boost 5 -CaseSensitive
Assert-Equal $builder._Criteria.Count 2 "Second criterion added"
Assert-Equal $builder._Criteria[1].Invert $true "Invert is set"
Assert-Equal $builder._Criteria[1].Boost 5 "Boost is set"
Assert-Equal $builder._Criteria[1].CaseSensitive $true "CaseSensitive is set"

# Pipeline syntax
$builder3 = New-SearchBuilder -Index "test_index"
$builder3 | Add-SearchFilter -Field "Name" -Filter "StartsWith" -Value "Test"
Assert-Equal $builder3._Criteria.Count 1 "Pipeline syntax adds criterion"

# Validation
Assert-Throw { Add-SearchFilter -SearchBuilder @{} -Field "x" -Filter "Equals" -Value "y" } "Did you create it with New-SearchBuilder?" "Rejects non-builder hashtable"

# ============================================================
# Add-TemplateFilter
# ============================================================
Write-Host "`n  [Add-TemplateFilter]" -ForegroundColor White

$builder = New-SearchBuilder -Index "test_index"
$builder | Add-TemplateFilter -Name "Article"
Assert-Equal $builder._Criteria[0].Field "_templatename" "ByName uses _templatename field"
Assert-Equal $builder._Criteria[0].Filter "Equals" "ByName uses Equals filter"
Assert-Equal $builder._Criteria[0].Value "Article" "ByName value is set"

$builder | Add-TemplateFilter -Id "{76036F5E-CBCE-46D1-AF0A-4143F9B557AA}"
Assert-Equal $builder._Criteria[1].Field "_templates" "ById uses _templates field"
Assert-Equal $builder._Criteria[1].Filter "Contains" "ById uses Contains filter"
Assert-Equal $builder._Criteria[1].Value "{76036F5E-CBCE-46D1-AF0A-4143F9B557AA}" "ById value is set"

# ============================================================
# Add-FieldContains
# ============================================================
Write-Host "`n  [Add-FieldContains]" -ForegroundColor White

$builder = New-SearchBuilder -Index "test_index"
$builder | Add-FieldContains -Field "Title" -Value "Hello"
Assert-Equal $builder._Criteria[0].Filter "Contains" "Uses Contains filter"
Assert-Equal $builder._Criteria[0].Field "Title" "Field is set"
Assert-Equal $builder._Criteria[0].Value "Hello" "Value is set"

# ============================================================
# Add-FieldEquals
# ============================================================
Write-Host "`n  [Add-FieldEquals]" -ForegroundColor White

$builder = New-SearchBuilder -Index "test_index"
$builder | Add-FieldEquals -Field "Language" -Value "en" -CaseSensitive
Assert-Equal $builder._Criteria[0].Filter "Equals" "Uses Equals filter"
Assert-Equal $builder._Criteria[0].CaseSensitive $true "CaseSensitive passed through"

# ============================================================
# ConvertFrom-RelativeDate
# ============================================================
Write-Host "`n  [ConvertFrom-RelativeDate]" -ForegroundColor White

$now = [DateTime]::Now

$result = ConvertFrom-RelativeDate -RelativeDate "7d"
$expected = $now.AddDays(-7)
$diff = [Math]::Abs(($result - $expected).TotalSeconds)
Assert-True ($diff -lt 2) "7d parses to ~7 days ago"

$result = ConvertFrom-RelativeDate -RelativeDate "2w"
$expected = $now.AddDays(-14)
$diff = [Math]::Abs(($result - $expected).TotalSeconds)
Assert-True ($diff -lt 2) "2w parses to ~14 days ago"

$result = ConvertFrom-RelativeDate -RelativeDate "3m"
$expected = $now.AddMonths(-3)
$diff = [Math]::Abs(($result - $expected).TotalSeconds)
Assert-True ($diff -lt 2) "3m parses to ~3 months ago"

$result = ConvertFrom-RelativeDate -RelativeDate "1y"
$expected = $now.AddYears(-1)
$diff = [Math]::Abs(($result - $expected).TotalSeconds)
Assert-True ($diff -lt 2) "1y parses to ~1 year ago"

Assert-Throw { ConvertFrom-RelativeDate -RelativeDate "abc" } "Invalid relative date format" "Rejects invalid format"
Assert-Throw { ConvertFrom-RelativeDate -RelativeDate "7x" } "Invalid relative date format" "Rejects invalid suffix"
Assert-Throw { ConvertFrom-RelativeDate -RelativeDate "" } $null "Rejects empty string"

# ============================================================
# Add-DateRangeFilter
# ============================================================
Write-Host "`n  [Add-DateRangeFilter]" -ForegroundColor White

$builder = New-SearchBuilder -Index "test_index"
$builder | Add-DateRangeFilter -Field "__Updated" -Last "7d"
Assert-Equal $builder._Criteria[0].Filter "InclusiveRange" "Uses InclusiveRange filter"
Assert-Equal $builder._Criteria[0].Field "__Updated" "Field is set"
Assert-True ($builder._Criteria[0].Value -is [array]) "Value is an array"
Assert-Equal $builder._Criteria[0].Value.Count 2 "Value has two elements (from, to)"
$fromDate = $builder._Criteria[0].Value[0]
$toDate = $builder._Criteria[0].Value[1]
Assert-True ($fromDate -is [DateTime]) "From is DateTime"
Assert-True ($toDate -is [DateTime]) "To is DateTime"
Assert-True ($fromDate -lt $toDate) "From is before To"

# Absolute range
$builder2 = New-SearchBuilder -Index "test_index"
$from = [DateTime]::new(2025, 1, 1)
$to = [DateTime]::new(2025, 6, 30)
$builder2 | Add-DateRangeFilter -Field "__Created" -From $from -To $to
Assert-Equal $builder2._Criteria[0].Value[0] $from "Absolute From is set"
Assert-Equal $builder2._Criteria[0].Value[1] $to "Absolute To is set"

# Absolute range without To (defaults to now)
$builder3 = New-SearchBuilder -Index "test_index"
$builder3 | Add-DateRangeFilter -Field "__Created" -From $from
$autoTo = $builder3._Criteria[0].Value[1]
$diffToNow = [Math]::Abs(($autoTo - [DateTime]::Now).TotalSeconds)
Assert-True ($diffToNow -lt 5) "To defaults to approximately now"

# ============================================================
# New-SearchFilterGroup / Add-SearchFilterGroup
# ============================================================
Write-Host "`n  [New-SearchFilterGroup / Add-SearchFilterGroup]" -ForegroundColor White

$group = New-SearchFilterGroup -Operation Or
Assert-True ($group -is [hashtable]) "Group is a hashtable"
Assert-True $group._IsFilterGroup "IsFilterGroup flag is set"
Assert-True $group._IsSearchBuilder "IsSearchBuilder flag allows Add-* functions"
Assert-Equal $group._Criteria.Count 0 "Group criteria starts empty"

$group | Add-TemplateFilter -Name "Article"
$group | Add-TemplateFilter -Name "Blog Post"
Assert-Equal $group._Criteria.Count 2 "Two criteria in group"

$builder = New-SearchBuilder -Index "test_index"
$builder | Add-SearchFilterGroup -Group $group
Assert-Equal $builder._Criteria.Count 1 "One group entry in builder"
Assert-Equal $builder._Criteria[0]._Operation "Or" "Group operation is Or"
Assert-Equal $builder._Criteria[0]._Criteria.Count 2 "Group has 2 nested criteria"
Assert-Equal $builder._Criteria[0]._Criteria[0].Value "Article" "First nested criterion value"
Assert-Equal $builder._Criteria[0]._Criteria[1].Value "Blog Post" "Second nested criterion value"

# Validation
Assert-Throw { $builder | Add-SearchFilterGroup -Group @{} } "Did you create it with New-SearchFilterGroup?" "Rejects non-group hashtable"

# Mixed: group + flat criteria
$builder | Add-FieldContains -Field "Title" -Value "Welcome"
Assert-Equal $builder._Criteria.Count 2 "Group + flat criterion coexist"
Assert-True $builder._Criteria[0].ContainsKey('_Operation') "First entry is a group"
Assert-True (-not $builder._Criteria[1].ContainsKey('_Operation')) "Second entry is flat"

# ============================================================
# Reset-SearchBuilder
# ============================================================
Write-Host "`n  [Reset-SearchBuilder]" -ForegroundColor White

$builder = New-SearchBuilder -Index "test_index" -First 25
$builder._Skip = 50
$builder._PageNumber = 3
$builder | Reset-SearchBuilder
Assert-Equal $builder._Skip 0 "Skip reset to 0"
Assert-Equal $builder._PageNumber 0 "PageNumber reset to 0"
Assert-Equal $builder._First 25 "First preserved after reset"
Assert-Equal $builder._Criteria.Count 0 "Criteria preserved after reset"

Assert-Throw { Reset-SearchBuilder -SearchBuilder @{} } "Did you create it with New-SearchBuilder?" "Rejects non-builder"

# ============================================================
# Get-SearchFilter
# ============================================================
Write-Host "`n  [Get-SearchFilter]" -ForegroundColor White

$filters = @(Get-SearchFilter)
Assert-Equal $filters.Count 14 "Returns all 14 filter types"

$filterNames = $filters | ForEach-Object { $_.Name }
Assert-True ($filterNames -contains "Equals") "Contains Equals"
Assert-True ($filterNames -contains "Contains") "Contains Contains"
Assert-True ($filterNames -contains "StartsWith") "Contains StartsWith"
Assert-True ($filterNames -contains "EndsWith") "Contains EndsWith"
Assert-True ($filterNames -contains "ContainsAll") "Contains ContainsAll"
Assert-True ($filterNames -contains "ContainsAny") "Contains ContainsAny"
Assert-True ($filterNames -contains "DescendantOf") "Contains DescendantOf"
Assert-True ($filterNames -contains "Fuzzy") "Contains Fuzzy"
Assert-True ($filterNames -contains "InclusiveRange") "Contains InclusiveRange"
Assert-True ($filterNames -contains "ExclusiveRange") "Contains ExclusiveRange"
Assert-True ($filterNames -contains "MatchesRegex") "Contains MatchesRegex"
Assert-True ($filterNames -contains "MatchesWildcard") "Contains MatchesWildcard"
Assert-True ($filterNames -contains "GreaterThan") "Contains GreaterThan"
Assert-True ($filterNames -contains "LessThan") "Contains LessThan"

# Every filter has a non-empty description
$allHaveDesc = $true
foreach ($f in $filters) {
    if ([string]::IsNullOrEmpty($f.Description)) { $allHaveDesc = $false; break }
}
Assert-True $allHaveDesc "All filters have descriptions"

# ============================================================
# ConvertTo-QuerySummary
# ============================================================
Write-Host "`n  [ConvertTo-QuerySummary]" -ForegroundColor White

$builder = New-SearchBuilder -Index "test_index"
$summary = ConvertTo-QuerySummary -SearchBuilder $builder
Assert-Equal $summary "(no criteria)" "Empty builder returns '(no criteria)'"

$builder | Add-FieldEquals -Field "Title" -Value "Test"
$summary = ConvertTo-QuerySummary -SearchBuilder $builder
Assert-Like $summary "*Title*Equals*Test*" "Single criterion appears in summary"

$builder | Add-FieldContains -Field "Body" -Value "Hello"
$summary = ConvertTo-QuerySummary -SearchBuilder $builder
Assert-Like $summary "*AND*" "Multiple criteria joined with AND"

# Inverted criterion
$builder2 = New-SearchBuilder -Index "test_index"
$builder2 | Add-SearchFilter -Field "Status" -Filter "Equals" -Value "Draft" -Invert
$summary2 = ConvertTo-QuerySummary -SearchBuilder $builder2
Assert-Like $summary2 "*NOT*" "Inverted criterion shows NOT"

# Group in summary
$builder3 = New-SearchBuilder -Index "test_index"
$group = New-SearchFilterGroup -Operation Or
$group | Add-TemplateFilter -Name "Article"
$group | Add-TemplateFilter -Name "Blog"
$builder3 | Add-SearchFilterGroup -Group $group
$summary3 = ConvertTo-QuerySummary -SearchBuilder $builder3
Assert-Like $summary3 "*Or*" "Group shows Or operation"
Assert-Like $summary3 "*Article*" "Group shows first value"
Assert-Like $summary3 "*Blog*" "Group shows second value"

# ============================================================
# Builder mutation (reference type verification)
# ============================================================
Write-Host "`n  [Builder mutation - reference semantics]" -ForegroundColor White

$builder = New-SearchBuilder -Index "test_index"
$ref = $builder
$builder | Add-FieldEquals -Field "X" -Value "Y"
Assert-Equal $ref._Criteria.Count 1 "Mutation visible through reference"
Assert-Equal $ref._Criteria[0].Field "X" "Same object mutated"

# Cleanup mock
Remove-Item function:global:New-PSObject -ErrorAction SilentlyContinue
