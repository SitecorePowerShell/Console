$Invocation = Resolve-Path ($MyInvocation.MyCommand.Definition)
$documentation = Split-Path $Invocation
$rootDirectory = Split-Path $documentation -Parent

$help = Join-Path -Path $rootDirectory -ChildPath "sitecore modules\PowerShell\Assets"
$moduleLibraryPath = (Join-Path -Path $rootDirectory -ChildPath "bin\Debug\Cognifide.PowerShell.dll")
if(!(Test-Path -Path $moduleLibraryPath)) {
    Write-Error "Module Library Path not found"
}
$helpLibraryPath = (Join-Path -Path $rootDirectory -ChildPath "Libraries\PowerShell.MamlGenerator.dll")
if(!(Test-Path -Path $helpLibraryPath)) {
    Write-Error "Help Library Path not found"
}

$files = [System.IO.Directory]::GetFiles($documentation, "*.ps1")
Add-Type -Path $helpLibraryPath

[PowerShell.MamlGenerator.commandHelpGenerator]::GenerateHelp($moduleLibraryPath, $help, $files)
Remove-Item "$help/*.maml" -Force
Get-Item "$help/*.xml" | Rename-Item -NewName { $_.name -replace '\.xml','.maml' }