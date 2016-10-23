$projectPath = "C:\Projects\sitecorepowershell\Trunk"
if(-not (Test-Path -Path $projectPath)) {
    $projectPath = "C:\Websites\spe.dev.local"

    if(-not(Test-Path -Path $projectPath)) {
        Write-Error "The project path defined does not exist."
        exit
    }
}

$modulePath = [Environment]::GetEnvironmentVariable("PSModulePath", "Machine")
if($modulePath -notlike "*$($projectPath)*") {
    [Environment]::SetEnvironmentVariable("PSModulePath", $modulePath + ";$($projectPath)\Modules", "Machine")
    $env:PSModulePath = $env:PSModulePath + ";$($projectPath)\Modules"
}

Import-Module -Name SPE

$props = @{
    Username = "admin"
    Password = "b"
    ConnectionUri = @("http://console")
}

$session = New-ScriptSession @props

$arguments = @{
    ProjectPath = $projectPath
}

Invoke-RemoteScript -Session $session -ScriptBlock {
    $root = "$($params.ProjectPath)\data\serialization\"

    Start-ScriptSession -ScriptBlock {
        Import-Item -Path "$($params.ProjectPath)\data\serialization\core\sitecore" -Root $root -Recurse
        Import-Item -Path "$($params.ProjectPath)\data\serialization\master\sitecore" -Root $root -Recurse
    }
} -Arguments $arguments


