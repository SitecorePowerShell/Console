# This is where your git-hub repository is located
$projectPath = "C:\Projects\sitecorepowershell\Trunk"

# This is where your sitecore sites are
# The sites need to have the standard \Data \Web folders in them
$sites = @{Path = "C:\inetpub\wwwroot\Sitecore8";  Version="8"},
         @{Path = "C:\inetpub\wwwroot\Sitecore70"; Version="7"},
         @{Path = "C:\inetpub\wwwroot\Sitecore75"; Version="7"};




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
	if(Test-Path "$path"){
	  cmd.exe /c "rmdir `"$path`" /Q" 
	}
	cmd.exe /c "mklink /J `"$path`" `"$source`""
		
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
	
	Create-Junction "$path\Data\serialization" "$projectPath\Cognifide.PowerShell.Sitecore$version\Data\serialization"
	Create-Junction  "$path\Website\sitecore modules\PowerShell" "$projectPath\Cognifide.PowerShell\sitecore modules\PowerShell"
	Create-Junction  "$path\Website\sitecore modules\Shell\PowerShell" "$projectPath\Cognifide.PowerShell\sitecore modules\Shell\PowerShell"
}

foreach($sitecoreSite in $sitecoreSites){
	Create-ProjectJunctions @sitecoreSite
}
