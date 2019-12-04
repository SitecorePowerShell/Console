#
# Copies contents of _Deploy folders into the various deployment sites, as specified within deploy_user.json
#
Param([string]$projectfilter)

# Load dependents scripts
. "$PSScriptRoot\Deploy_Functions.ps1"

# Load configuration files
$deployConfig = Get-Content "$PSScriptRoot\deploy.json" | ConvertFrom-Json
$userConfig = Get-Content "$PSScriptRoot\deploy.user.json" | ConvertFrom-Json

# Set source folder
$sourceFolder = $PSScriptRoot

# Convert the variables into a hashtable for replacement
$variables = @{ 
    "%%sourceFolder%%" = $sourceFolder 
}

Write-Host -ForegroundColor Cyan "*******************************************"
Write-Host -ForegroundColor Cyan " Sitecore PowerShell Extensions Deployment"
Write-Host -ForegroundColor Cyan "*******************************************"
Write-Host

if ($projectFilter) {
    Write-Host "Applying project filter: $projectFilter"
}

Write-Host
Write-Host -ForegroundColor Blue "Building user configuration with variable replacements"
Write-Host   

# First copy user configuration to a deploy folder
$userConfigPath = Join-Path $sourceFolder $deployConfig.userConfigurationFolder
$deployFolderFullPath = Join-Path $sourceFolder $deployConfig.deployFolder
$deployUserConfigPath = Join-Path $deployFolderFullPath $deployConfig.userConfigurationFolder

Get-ChildItem $userConfigPath -Recurse | ? { $_.Extension -eq ".config" } | % {
    $targetFile = $deployUserConfigPath + $_.FullName.SubString($userConfigPath.Length);
    
    Copy-ItemWithStructure $_.FullName $targetFile

    Write-Host "--- Copied $targetFile"
}

# Perform variable replacement on configuration files in deploy folder
$variablesRegexKeys = $variables.keys | % { [System.Text.RegularExpressions.Regex]::Escape($_) }
$variablesRegex = [regex]($variablesRegexKeys -join '|')

$regexCallback = { $variables[$args[0].Value] }

Get-ChildItem $deployUserConfigPath -Recurse -Include *.config | % {
    $file = [System.IO.File]::ReadAllText($_.FullName)
    $file = $variablesRegex.Replace($file, $regexCallback)
    Set-Content -Path $_ -Value $file
}

Write-Host
Write-Host -ForegroundColor Blue "User configuration build complete"
Write-Host -ForegroundColor Blue "Processing sites"
Write-Host  


# Loop over the sites to deploy
foreach ( $site in $userConfig.sites )
{
    $site = Update-FromDefaultSite $site 

    # Get folders to deploy to from configuration and based on the destination site's version
    $deployProjects = $deployConfig.deployProjects | Filter-ProjectsForSite -version $site.version -projectFilter $projectFilter 
  
    if ($deployProjects)
    {
        Write-Host
        Write-Host -ForegroundColor Cyan "Deploying $(@($deployProjects).Count) projects to $($site.path)"   
        Write-Host

        foreach ($deployProject in $deployProjects)
        {           
            $deployFolder = Get-DeployFolder $sourceFolder $deployConfig $deployProject.project

            if (!(Test-Path $deployFolder))
            {
                Write-Error "Cannot find deploy folder for project: $deployFolder"
                continue
            }

            $projectFolder = Get-ProjectFolder $sourceFolder $deployProject.project
        
            Write-Host
            Write-Host -ForegroundColor Green "Copying from $deployFolder to $($site.path)"
            Write-Host

            $filesToCopy = Get-ChildItem $deployFolder -Recurse | ? { $_.PSIsContainer -eq $False }

            if ($site.junction -and $deployProject.junctionPoints -ne $null) {
                # Deploy any files that are not included in junction-folders
                $filesToCopy = $filesToCopy | Filter-JunctionPoints $deployProject
                
                # Create the junction points
                $deployProject.junctionPoints | % { 
                    $sourcePath = Join-Path $projectFolder $_
                    $sitePath = Join-Path $site.path $_ 
                
                    Create-Junction $sitePath $sourcePath

                    Write-Host "--- Created junction at $sitePath"                
                }
            }

            $filesToCopy | % {
                $targetFile = Join-Path $site.path $_.FullName.SubString($deployFolder.Length);
                
                Copy-ItemWithStructure $_.FullName $targetFile

                Write-Host "--- Copied $targetFile"
            }
        }
    }
    else 
    {
        Write-Host
        Write-Host -ForegroundColor Cyan "No valid deployment sites for this project"
        Write-Host       
    }

    # Deploy UserConfiguration

    Write-Host
    Write-Host -ForegroundColor Blue "Copying user configuration to $($site.path)"
    Write-Host   
    
    Get-ChildItem $deployUserConfigPath -Recurse | ? { $_.Extension -eq ".config" } | % {
        $targetFile = Join-Path $site.path $_.FullName.SubString($deployUserConfigPath.Length);
        
        Copy-ItemWithStructure $_.FullName $targetFile

        Write-Host "--- Copied $targetFile"
    }
}

