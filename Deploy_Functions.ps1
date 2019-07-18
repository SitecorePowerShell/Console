function Update-FromDefaultSite
{
    Param($site)

    $defaultSite = $deployConfig.sitesDefault.PsObject.Copy()

    foreach ( $property in ($site | Get-Member -MemberType NoteProperty) )
    {
        $defaultSite | Add-Member -MemberType NoteProperty -Name $property.Name -Value ($site | Select-Object -ExpandProperty $property.Name) -Force
    }

    return $defaultSite
}
function Create-Junction{
    [cmdletbinding(
        DefaultParameterSetName = 'Directory',
        SupportsShouldProcess=$True
    )]
    Param (
        [string]$path,
        [string]$source
        )
    New-Item -Path $path -Force -ItemType Directory | Out-Null
    if(Test-Path "$path"){
        cmd.exe /c "rmdir `"$path`" /Q /S" | Out-Null
    }
    if(-not $removeOnly){
        cmd.exe /c "mklink /J `"$path`" `"$source`"" | Out-Null
    }
}

function Test-ReparsePoint([string]$path) {
  $file = Get-Item $path -Force -ea SilentlyContinue
  return [bool]($file.Attributes -band [IO.FileAttributes]::ReparsePoint)
}

function Get-DeployFolder {
    Param (
        [string]$sourceFolder,    
        $deployConfig,    
        [string]$projectFolder
    )

    $deployFolderBase = Join-Path $sourceFolder $deployConfig.deployFolder

    return Join-Path $deployFolderBase $projectFolder
}
function Get-ProjectFolder {
    Param (
        [string]$sourceFolder,    
        [string]$projectFolder
    )

    return Join-Path $sourceFolder $projectFolder
}

function Filter-ProjectsForSite {
    Param (
        [Parameter(ValueFromPipeline=$True)]
        [Object]$deployProject,
        [decimal]$version,
        [string]$projectFilter
    )

    process {
        # Include project only if version is >= minVersion, and <= maxVersion (if a maxVersion is specified)
        if ( ($version -ge $deployProject.minVersion) -and (!$deployProject.maxVersion -or ($version -le $deployProject.maxVersion)) )  {    
            
            # If a projectFilter is set, include it only if it matches
            if (!$projectFilter -or $projectFilter -eq $deployProject.project ) {
                $deployProject
            }
        }    
    }
}

filter Filter-JunctionPoints {
    param(
        $deployProject
    )

    foreach ( $junctionPoint in $deployProject.junctionPoints )
    {
        if ($_.FullName.Contains($junctionPoint)) {
            return
        }
    }

    $_
}

function Copy-ItemWithStructure {
    param (
        $sourceFilePath,
        $destinationFilePath
    )
    
    if (!(Test-Path $destinationFilePath )) {
        New-Item -ItemType File -Path $destinationFilePath -Force | Out-Null
    }
    Copy-Item $sourceFilePath -destination $destinationFilePath
}