# This is where your git-hub repository is located
$projectPath = "C:\Projects\sitecorepowershell\Trunk"

# This is where your sitecore sites are
# The sites need to have the standard \Data \Web folders in them
$sites = @{Path = "C:\inetpub\wwwroot\Sitecore81"; Version="8"},
         @{Path = "C:\inetpub\wwwroot\Sitecore8";  Version="8"},
         @{Path = "C:\inetpub\wwwroot\Sitecore70"; Version="7"},
         @{Path = "C:\inetpub\wwwroot\Sitecore75"; Version="7"};

#Set the below to true to remove jusction points only and not set them back
$removeOnly = $false

# --------------------------------------------------------------
# Ignore everything after this - all you need to provide is above
# --------------------------------------------------------------





function Create-Junction{
    [cmdletbinding(
        DefaultParameterSetName = 'Directory',
        SupportsShouldProcess=$True
    )]
    Param (
        [string]$path,
        [string]$source
        )
    Write-Host "$path --> $source"
    if(Test-Path "$path"){
        cmd.exe /c "rmdir `"$path`" /Q /S" 
    }
    if(-not $removeOnly){
        cmd.exe /c "mklink /J `"$path`" `"$source`""
    }
}

function Create-ProjectJunctions{
    [cmdletbinding(
        DefaultParameterSetName = 'Directory',
        SupportsShouldProcess=$True
    )]
    Param (
        [string]$path, 
        [int]$version
        )

    Write-Host "--------------------------------------------------------------------------------------------"
    Write-Host "$project\$version --> $path"
    Write-Host "--------------------------------------------------------------------------------------------"

    Create-Junction "$path\Data\serialization" "$projectPath\Cognifide.PowerShell\Data\serialization"

    if(Test-Path "$path\Website\sitecore modules\PowerShell"){
        cmd.exe /c "rmdir `"$path\Website\sitecore modules\PowerShell`" /Q /S" 
    }
    if(-not (Test-Path "$path\Website\sitecore modules")){
        New-Item  "$path\Website" -Name "sitecore modules" -ItemType Directory | Out-Null
    }
	
    New-Item  "$path\Website\sitecore modules" -Name "PowerShell" -ItemType Directory 
    Create-Junction "$path\Website\sitecore modules\PowerShell\Assets" "$projectPath\Cognifide.PowerShell\sitecore modules\PowerShell\Assets"
    Create-Junction "$path\Website\sitecore modules\PowerShell\Layouts" "$projectPath\Cognifide.PowerShell\sitecore modules\PowerShell\Layouts"
    Create-Junction "$path\Website\sitecore modules\PowerShell\Scripts" "$projectPath\Cognifide.PowerShell\sitecore modules\PowerShell\Scripts"
    Create-Junction "$path\Website\sitecore modules\PowerShell\Services" "$projectPath\Cognifide.PowerShell\sitecore modules\PowerShell\Services"
    Create-Junction "$path\Website\sitecore modules\PowerShell\Styles" "$projectPath\Cognifide.PowerShell.Sitecore$version\sitecore modules\PowerShell\Styles"

    if(-not (Test-Path "$path\Website\sitecore modules\Shell")){
        New-Item  "$path\Website\sitecore modules" -Name "Shell" -ItemType Directory | Out-Null
    }
    Create-Junction "$path\Website\sitecore modules\Shell\PowerShell" "$projectPath\Cognifide.PowerShell\sitecore modules\Shell\PowerShell"
}

foreach($sitecoreSite in $sites){
	Create-ProjectJunctions @sitecoreSite
}