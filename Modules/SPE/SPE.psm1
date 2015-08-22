$assemblies = @("\Libraries\Cognifide.PowerShell.dll", "\Libraries\Sitecore.Kernel.dll")
foreach($assembly in $assemblies) {
    $path = Join-Path -Path $PSScriptRoot -ChildPath $assembly
    if(Test-Path -Path $path) {
        [System.Reflection.Assembly]::LoadFile($path)
    }
}