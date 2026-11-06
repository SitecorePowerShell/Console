# Unit tests for Resolve-ReceivedFilePath (extracted from Receive-RemoteItem.ps1).
# The helper is dot-sourced so its private function becomes callable in this scope.

$moduleRoot = "$PSScriptRoot\..\..\Modules\SPE"
. "$moduleRoot\Receive-RemoteItem.ps1"

Add-Type -AssemblyName System.Net.Http

function New-MockResponse {
    param([string]$ContentDisposition)
    $msg = [System.Net.Http.HttpResponseMessage]::new()
    $msg.Content = [System.Net.Http.ByteArrayContent]::new([byte[]]::new(1))
    if ($null -ne $ContentDisposition) {
        $null = $msg.Content.Headers.TryAddWithoutValidation('Content-Disposition', $ContentDisposition)
    }
    return $msg
}

$tempRoot = Join-Path $env:TEMP ("spe-tests-" + [guid]::NewGuid().ToString("N").Substring(0,8))
New-Item -Path $tempRoot -ItemType Directory -Force | Out-Null

try {
    Write-Host "`n  [Resolve-ReceivedFilePath - Destination with extension]" -ForegroundColor White

    $dest = Join-Path $tempRoot "explicit.jpg"
    $resp = New-MockResponse -ContentDisposition 'attachment; filename="server-choice.jpg"'
    $out = Resolve-ReceivedFilePath -ResponseMessage $resp -Destination $dest `
        -Path "/sitecore/media library/images/cover" -Container $false -IsMediaItem $true
    Assert-Equal $out $dest "Destination with extension overrides Content-Disposition filename"

    Write-Host "`n  [Resolve-ReceivedFilePath - Destination is a directory]" -ForegroundColor White

    $destDir = Join-Path $tempRoot "downloads\"
    $resp = New-MockResponse -ContentDisposition 'attachment; filename="cover.jpg"'
    $out = Resolve-ReceivedFilePath -ResponseMessage $resp -Destination $destDir `
        -Path "/sitecore/media library/images/cover" -Container $false -IsMediaItem $true
    $expected = Join-Path ($destDir.TrimEnd('\')) "cover.jpg"
    Assert-Equal $out $expected "Directory destination joins server filename (media item path without extension)"
    Assert-True (Test-Path $destDir -PathType Container) "Destination directory is created when missing"

    Write-Host "`n  [Resolve-ReceivedFilePath - Container mode preserves media structure]" -ForegroundColor White

    $destDir = Join-Path $tempRoot "mirror\"
    $resp = New-MockResponse -ContentDisposition 'attachment; filename="cover.jpg"'
    $out = Resolve-ReceivedFilePath -ResponseMessage $resp -Destination $destDir `
        -Path "/sitecore/media library/Default Website/cover" -Container $true -IsMediaItem $true
    Assert-Like $out "*\Default Website\cover.jpg" "Container mode preserves media library subdirectory"
    Assert-True ($out -notlike "*sitecore\media library*") "Container mode strips the media-library prefix"

    Write-Host "`n  [Resolve-ReceivedFilePath - Destination ends with filename-without-extension]" -ForegroundColor White

    $destDir = Join-Path $tempRoot "stripname"
    New-Item -Path $destDir -ItemType Directory | Out-Null
    $destWithName = Join-Path $destDir "cover"
    $resp = New-MockResponse -ContentDisposition 'attachment; filename="cover.jpg"'
    $out = Resolve-ReceivedFilePath -ResponseMessage $resp -Destination $destWithName `
        -Path "/sitecore/media library/images/cover" -Container $false -IsMediaItem $true
    # Current behavior: destination endswith "cover" (name without ext) and Path basename is "cover",
    # so the suffix strip fires and the final path joins filename from the response.
    Assert-Like $out "*cover.jpg" "Stripping repeated basename rejoins filename from Content-Disposition"

    Write-Host "`n  [Resolve-ReceivedFilePath - Non-media / file mode (no prefix strip)]" -ForegroundColor White

    $destDir = Join-Path $tempRoot "filemode\"
    $resp = New-MockResponse -ContentDisposition 'attachment; filename="default.js"'
    $out = Resolve-ReceivedFilePath -ResponseMessage $resp -Destination $destDir `
        -Path "default.js" -Container $false -IsMediaItem $false
    # Destination already has .js extension via Content-Disposition resolution -> joined filename.
    Assert-Like $out "*default.js" "File-mode download resolves to destination\filename"

    Write-Host "`n  [Resolve-ReceivedFilePath - Content-Disposition without filename]" -ForegroundColor White

    $destDir = Join-Path $tempRoot "nofilename\"
    $resp = New-MockResponse -ContentDisposition 'attachment'
    # The helper is a plain function (no CmdletBinding), so caller -ErrorVariable
    # does not attach. Runner sets $ErrorActionPreference='Stop' globally, which
    # turns the helper's Write-Error into a terminating exception -- so catch
    # that and assert the error message.
    $caught = $null
    try {
        $null = Resolve-ReceivedFilePath -ResponseMessage $resp -Destination $destDir `
            -Path "/sitecore/media library/images/cover" -Container $false -IsMediaItem $true
    } catch {
        $caught = $_
    }
    Assert-NotNull $caught "Missing Content-Disposition filename surfaces an error"
    Assert-Like "$caught" "*extension*" "Error message mentions the missing file extension"

    Write-Host "`n  [Resolve-ReceivedFilePath - Quoted filename with spaces]" -ForegroundColor White

    $destDir = Join-Path $tempRoot "spaces\"
    $resp = New-MockResponse -ContentDisposition 'attachment; filename="my cover shot.jpg"'
    $out = Resolve-ReceivedFilePath -ResponseMessage $resp -Destination $destDir `
        -Path "/sitecore/media library/images/cover" -Container $false -IsMediaItem $true
    Assert-Like $out "*my cover shot.jpg" "Filename with spaces is preserved through the resolver"

    Write-Host "`n  [Resolve-ReceivedFilePath - File-mode destination with extension]" -ForegroundColor White

    # File-mode, Destination is a full path with extension -> early return path.
    $dest = Join-Path $tempRoot "archived.zip"
    $resp = New-MockResponse -ContentDisposition 'attachment; filename="archived.zip"'
    $out = Resolve-ReceivedFilePath -ResponseMessage $resp -Destination $dest `
        -Path "C:\inetpub\wwwroot\Website\temp\archived.zip" -Container $false -IsMediaItem $false
    Assert-Equal $out $dest "File-mode Destination with extension returns unchanged"
}
finally {
    Remove-Item -Path $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
