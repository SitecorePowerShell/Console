#Trace-Command -Name ParameterBinding -PSHost -Expression { "michael" | Get-Session }
$currentDirectory = "C:\inetpub\wwwroot\Console72"
$documentation = Join-Path -Path $currentDirectory -ChildPath "Documentation"
$help = Join-Path -Path $currentDirectory -ChildPath "Console\Assets"
$moduleLibraryPath = (Join-Path -Path $currentDirectory -ChildPath "Website\bin\Cognifide.PowerShell.dll")
if(!(Test-Path -Path $moduleLibraryPath)) {
    Write-Error "Module Library Path not found"
}
$helpLibraryPath = (Join-Path -Path $currentDirectory -ChildPath "Libraries\PowerShell.MamlGenerator.dll")
if(!(Test-Path -Path $helpLibraryPath)) {
    Write-Error "Help Library Path not found"
}

$files = [System.IO.Directory]::GetFiles($documentation, "*.ps1")
Add-Type -Path $helpLibraryPath

[PowerShell.MamlGenerator.CmdletHelpGenerator]::GenerateHelp($moduleLibraryPath, $help, $files)