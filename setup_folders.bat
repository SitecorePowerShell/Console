set SITECORE=C:\inetpub\wwwroot\Sitecore8\
set PROJECT=C:\Projects\sitecorepowershell\Trunk\

rmdir "%SITECORE%Data\serialization /Q
mklink /J %SITECORE%Data\serialization %PROJECT%Data\serialization

rmdir "%SITECORE%Website\sitecore modules\PowerShell" /Q
mklink /J "%SITECORE%Website\sitecore modules\PowerShell" "%PROJECT%sitecore modules\PowerShell"

rmdir "%SITECORE%Website\sitecore modules\Shell\PowerShell" /Q
mklink /J "%SITECORE%Website\sitecore modules\Shell\PowerShell" "%PROJECT%sitecore modules\Shell\PowerShell"
