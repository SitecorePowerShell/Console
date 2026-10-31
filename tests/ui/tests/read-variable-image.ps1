# UI-test-only: Read-Variable "image" editor for an image field value.
$root = "master:\media library\spe-test"
if (-not (Test-Path $root)) {
    New-Item -Path "master:\media library" -Name "spe-test" -ItemType "Common/Folder" | Out-Null
}
$first = Get-Item "$root\ImageOne" -ErrorAction SilentlyContinue
if (-not $first) { $first = New-Item -Path $root -Name "ImageOne" -ItemType "System/Media/Unversioned/Image" }
$second = Get-Item "$root\ImageTwo" -ErrorAction SilentlyContinue
if (-not $second) { $second = New-Item -Path $root -Name "ImageTwo" -ItemType "System/Media/Unversioned/Image" }
# the media browser lists from the search index, so wait until the new items are in it
$first, $second | Initialize-SearchIndexItem
$index = [Sitecore.ContentSearch.ContentSearchManager]::GetIndex([Sitecore.ContentSearch.SitecoreIndexableItem]$second).Name
$criteria = @{ Filter = "Equals"; Field = "_group"; Value = $second.ID.ToShortID().ToString().ToLower() }
$deadline = (Get-Date).AddSeconds(30)
while (-not (Find-Item -Index $index -Criteria $criteria) -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 500 }

$fieldValue = "<image mediaid=`"$($first.ID)`" alt=`"custom alt`" />"
$result = Read-Variable -Title "IMAGE_EDITOR" -Parameters @(
    @{ Name = "fieldValue"; Title = "Image field"; Editor = "image"; Source = "/sitecore/media library/spe-test" }
)
Write-Host "RESULT=$result"
Write-Host "FIELD=$fieldValue"
Write-Host "SECOND=$($second.ID)"
