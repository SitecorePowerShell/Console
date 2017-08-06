
# Adds the SPE Modules path to the PSModulePath Environment Variable, for use with SPE Remoting.

$speModulePath = Join-Path $PSScriptRoot "Modules"

$envModulePath = [Environment]::GetEnvironmentVariable("PSModulePath", "Machine")

if($envModulePath -notlike "*$($speModulePath)*") {
    
    [Environment]::SetEnvironmentVariable("PSModulePath", $envModulePath.TrimEnd(";") + ";$($speModulePath)", "Machine")

    $env:PSModulePath = $env:PSModulePath.TrimEnd(";") + ";$($speModulePath)"

    Write-Host "PSModulePath environment variable updated with $speModulePath"
}
else
{
    Write-Host "PSModulePath environment variable already contains $speModulePath"
}

