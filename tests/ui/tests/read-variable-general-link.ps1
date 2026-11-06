# UI-test-only: Read-Variable "generallink" editor for a General Link field value.
$root = "master:\content\spe-test"
if (-not (Test-Path $root)) {
    New-Item -Path "master:\content" -Name "spe-test" -ItemType "Common/Folder" | Out-Null
}
$target = Get-Item "$root\LinkTarget" -ErrorAction SilentlyContinue
if (-not $target) { $target = New-Item -Path $root -Name "LinkTarget" -ItemType "Common/Folder" }

$fieldValue = "<link linktype=`"internal`" id=`"$($target.ID)`" text=`"target link`" />"
$result = Read-Variable -Title "GENERAL_LINK_EDITOR" -Parameters @(
    @{ Name = "fieldValue"; Title = "General Link field"; Editor = "generallink" }
)
Write-Host "RESULT=$result"
Write-Host "FIELD=$fieldValue"
