<#
.SYNOPSIS
    Gets all the subfolders that exist under the provided path which are listed in the SITECORE_DEVELOPMENT_TRANSFORMS environment variable.
.DESCRIPTION
    Splits the folder names in SITECORE_DEVELOPMENT_TRANSFORMS and finds matching patch folders under the provided path.
.PARAMETER Path
    Specifies the path to search.
.EXAMPLE
    PS C:\> .\Get-PatchFolders.ps1 -Path c:\tools\dev-patches
.INPUTS
    None
.OUTPUTS
    None
#>
Function Get-PatchFolders {
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateScript( { Test-Path $_ -PathType Container })]
        [string]$Path
    )
    
    $folders = @()
    
    # Example: SITECORE_DEVELOPMENT_PATCHES=CustomErrorsOn,DebugOn,OptimizeCompilationsOn
    $folderNames = $env:SITECORE_DEVELOPMENT_PATCHES
    if (-not $folderNames) {
        return $folders
    }
    
    $illegalCharacters = [System.IO.Path]::GetInvalidFileNameChars()
    $folderNames.Split(",") | ForEach-Object {
        $patchFolder = $_

        $validName = $true
        $illegalCharacters | ForEach-Object {
            if ($patchFolder.IndexOf($_) -gt 0) {
                Write-Host "** Sitecore development patch folder name $patchFolder is invalid"
                $validName = $false
                return
            }
        }
        if (-not $validName) {
            return
        }

        $folder = Join-Path $Path $patchFolder
        if (-not (Test-Path $folder)) {
            Write-Host "** Sitecore development patch folder $patchFolder not found in $Path"
            return
        }
        $folders += (Get-Item $folder)
    }
    $folders
}