#Requires -Version 7
param(
    [int]$Milestone = 40,
    [string]$Version = '9.0-RC-1',
    [string]$PreviousTag = '8.0',
    [string]$Branch = 'release/9.0',
    [string]$Intro = 'Release candidate 1 for SPE 9.0. Please try it on a test environment and report anything that breaks in [Issues](https://github.com/SitecorePowerShell/Console/issues) before the final release.'
)
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8
Set-Location (Split-Path $PSScriptRoot)
$repo = 'SitecorePowerShell/Console'
$tag = $Version.ToLowerInvariant()

$query = 'query($owner:String!,$name:String!,$ms:Int!,$after:String){repository(owner:$owner,name:$name){milestone(number:$ms){title issues(first:50,after:$after){pageInfo{hasNextPage endCursor} nodes{number title stateReason issueType{name} labels(first:20){nodes{name}} assignees(first:15){nodes{login}}}}}}}'
$all = @(); $cursor = $null
do {
    $after = if ($cursor) { @('-f', "after=$cursor") } else { @() }
    $m = (gh api graphql -f query=$query -f owner=SitecorePowerShell -f name=Console -F ms=$Milestone @after | ConvertFrom-Json).data.repository.milestone
    $p = $m.issues; $all += $p.nodes; $cursor = $p.pageInfo.endCursor
} while ($p.pageInfo.hasNextPage)
$ms = $m.title

# Per-release overrides. 9.0: #1361 was implemented by #1460; credit its reporter there instead of listing it twice.
$extraCredit = @{ 1460 = @('peplau'); 1476 = @('iamandycohen') }
$notes = @{ 1460 = ' (requested in #1361)' }
# #1417 merged to master and was moved into the release branch by hand, so GitHub doesn't count it as a first contribution here.
$newContributors = @{ avwolferen = 1417 }
$issues = $all | Where-Object { $_.stateReason -eq 'COMPLETED' -and $_.number -ne 1361 }

# PRs merged into the release branch that no listed issue covers (no closing reference, no #issue in the title).
$listed = @($issues.number)
$prs = gh pr list --repo $repo --state merged --base $Branch --limit 500 --json number,title,author,closingIssuesReferences | ConvertFrom-Json |
    Where-Object { $pr = $_
        -not (@($pr.closingIssuesReferences.number) + @([regex]::Matches($pr.title, '#(\d+)') | ForEach-Object { [int]$_.Groups[1].Value }) | Where-Object { $listed -contains $_ })
    } | Sort-Object number -Descending

# Label names carry emoji; resolve them by suffix so this file stays ASCII.
$repoLabels = gh label list --repo $repo --limit 500 --json name | ConvertFrom-Json | ForEach-Object name
function Label([string]$suffix) { $repoLabels | Where-Object { $_ -like "*$suffix" } | Select-Object -First 1 }
$hl = Label 'release-highlight'; $vuln = Label 'vulnerability-fix'; $sec = 'area-security'; $impr = Label 'improvement'; $brk = Label 'impact-breaking-change'

function Has($i, $label) { @($i.labels.nodes.name) -contains $label }
function Line($i) {
    $who = @($i.assignees.nodes.login) + @($i.author.login) + @($extraCredit[[int]$i.number]) | Where-Object { $_ } | Sort-Object -Unique | Sort-Object { $_.ToLowerInvariant() }
    $title = ($i.title -replace '[\u2013\u2014]', '-').Trim()
    "- #$($i.number) : $title$($notes[[int]$i.number]) - " + (($who | ForEach-Object { "@$_" }) -join ' ')
}
function Query([string]$q) { "https://github.com/$repo/issues?q=" + [uri]::EscapeDataString("is:issue state:closed milestone:`"$ms`" $q") }

$left = [System.Collections.Generic.List[object]]::new(); $issues | Sort-Object number -Descending | ForEach-Object { $left.Add($_) }
function Take([scriptblock]$filter) { $hit = @($left | Where-Object $filter); $hit | ForEach-Object { [void]$left.Remove($_) }; $hit }

$highlights   = Take { Has $_ $hl }
$security     = Take { (Has $_ $vuln) -or (Has $_ $sec) }
$bugs         = Take { $_.issueType.name -eq 'Bug' }
$improvements = Take { $_.issueType.name -eq 'Feature' -or (Has $_ $impr) }
$maintenance  = @($left)
$breaking     = @($issues | Where-Object { Has $_ $brk } | Sort-Object number -Descending)

$sb = [System.Text.StringBuilder]::new()
function W([string]$s = '') { [void]$sb.AppendLine($s) }

W $Intro
W
W "> **Before upgrading:** SPE 9.0 requires Sitecore 10.0 or later and .NET Framework 4.8. Sitecore 8.x and 9.x are no longer supported. This release was tested on Sitecore 10.4 and 10.5."
W
foreach ($section in @(
    @{ Name = 'Breaking Changes'; Items = $breaking; Q = "label:`"$brk`"" },
    @{ Name = 'Release Highlights'; Items = $highlights; Q = "label:`"$hl`"" },
    @{ Name = 'Security'; Items = $security; Q = "-label:`"$hl`" label:`"$sec`"" },
    @{ Name = 'Improvements'; Items = $improvements; Q = "-label:`"$hl`" -label:`"$sec`" -type:Bug" },
    @{ Name = 'Bugs Fixed'; Items = $bugs; Q = "-label:`"$hl`" -label:`"$sec`" type:Bug" },
    @{ Name = 'Maintenance'; Items = $maintenance; Q = "-label:`"$hl`" -label:`"$sec`" type:Task" })) {
    if (-not $section.Items) { continue }
    W "## [$($section.Name)]($(Query $section.Q))"
    W
    $section.Items | ForEach-Object { W (Line $_) }
    W
}
if ($prs) {
    W "## [Pull Requests](https://github.com/$repo/pulls?q=$([uri]::EscapeDataString("is:pr is:merged base:$Branch")))"
    W
    $prs | ForEach-Object { W (Line $_) }
    W
}

if ($newContributors.Count) {
    W "## New Contributors"
    $newContributors.GetEnumerator() | Sort-Object Value | ForEach-Object { W "* @$($_.Key) made their first contribution in https://github.com/$repo/pull/$($_.Value)" }
    W
}

W "**Full Changelog**: https://github.com/$repo/compare/$PreviousTag...$tag"
W
W "## File Hashes"
foreach ($pkg in "Sitecore.PowerShell.Extensions-$Version-IAR", "Sitecore.PowerShell.Extensions-$Version") {
    W "### $pkg"
    foreach ($f in "$pkg.zip", "$pkg.scwdp.zip") {
        W "SHA256 for $f"
        W (Get-FileHash "_output\$f" -Algorithm SHA256).Hash
        W
    }
}
W "SHA256 for SPE.Minimal-$Version.zip"
W (Get-FileHash "_output\SPE.Minimal-$Version.zip" -Algorithm SHA256).Hash

$out = "_output\release-notes-$tag.md"
[IO.File]::WriteAllText((Join-Path (Get-Location) $out), $sb.ToString(), (New-Object Text.UTF8Encoding $false))
"sections: breaking=$($breaking.Count) highlights=$($highlights.Count) security=$($security.Count) improvements=$($improvements.Count) bugs=$($bugs.Count) maintenance=$($maintenance.Count) prs=$(@($prs).Count) total=$($issues.Count)"
"written $out"
