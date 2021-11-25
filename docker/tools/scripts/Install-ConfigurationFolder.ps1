<#
.SYNOPSIS
    Applies a directory of Sitecore configuration patches to a destination path / web root.
.DESCRIPTION
    Applies a directory of Sitecore configuration patches to a destination path / web root. Any .config
    files will be copied. The structure of the directory should match the structure of the intended destination in the web root.
.PARAMETER Path
    Specifies the path of the target web root.
.PARAMETER ConfigurationPath
    Specifies the path of the configuration patch collection.
.EXAMPLE
    PS C:\> .\Install-ConfigurationFolder.ps1 -Path 'C:\inetpub\wwwroot' -PatchPath 'C:\tools\dev-patches\CustomErrorsOff'
.INPUTS
    None
.OUTPUTS
    None
#>
[CmdletBinding()]
Param (
    [Parameter(Mandatory = $true)]
    [ValidateScript( { Test-Path $_ -PathType Container })]
    [string]$Path,

    [Parameter(Mandatory = $true)]
    [ValidateScript( { Test-Path $_ -PathType Container })]
    [string]$PatchPath
)

# We need to iterate children, otherwise Copy-Item includes the folder itself when copying
Get-ChildItem $PatchPath | Copy-Item -Destination $Path -Filter *.config -Recurse -Force -Container