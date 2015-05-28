@echo off
set SITECORE8=C:\inetpub\wwwroot\Sitecore8\
set SITECORE70=C:\inetpub\wwwroot\Sitecore70\
set SITECORE75=C:\inetpub\wwwroot\Sitecore75\
set PROJECT=C:\Projects\sitecorepowershell\Trunk\

rmdir "%SITECORE8%Data\serialization" /Q
mklink /J "%SITECORE8%Data\serialization" "%PROJECT%Cognifide.PowerShell.Sitecore8\Data\serialization"

rmdir "%SITECORE70%Data\serialization" /Q
mklink /J "%SITECORE70%Data\serialization" "%PROJECT%Cognifide.PowerShell.Sitecore7\Data\serialization"

rmdir "%SITECORE75%Data\serialization" /Q
mklink /J "%SITECORE75%Data\serialization" "%PROJECT%Cognifide.PowerShell.Sitecore7\Data\serialization"

rmdir "%SITECORE8%Website\sitecore modules\PowerShell" /Q
mklink /J "%SITECORE8%Website\sitecore modules\PowerShell" "%PROJECT%Cognifide.PowerShell\sitecore modules\PowerShell"

rmdir "%SITECORE70%Website\sitecore modules\PowerShell" /Q
mklink /J "%SITECORE70%Website\sitecore modules\PowerShell" "%PROJECT%Cognifide.PowerShell\sitecore modules\PowerShell"

rmdir "%SITECORE75%Website\sitecore modules\PowerShell" /Q
mklink /J "%SITECORE75%Website\sitecore modules\PowerShell" "%PROJECT%Cognifide.PowerShell\sitecore modules\PowerShell"

rmdir "%SITECORE8%Website\sitecore modules\Shell\PowerShell" /Q
mklink /J "%SITECORE8%Website\sitecore modules\Shell\PowerShell" "%PROJECT%Cognifide.PowerShell\sitecore modules\Shell\PowerShell"

rmdir "%SITECORE70%Website\sitecore modules\Shell\PowerShell" /Q
mklink /J "%SITECORE70%Website\sitecore modules\Shell\PowerShell" "%PROJECT%Cognifide.PowerShell\sitecore modules\Shell\PowerShell"

rmdir "%SITECORE75%Website\sitecore modules\Shell\PowerShell" /Q
mklink /J "%SITECORE75%Website\sitecore modules\Shell\PowerShell" "%PROJECT%Cognifide.PowerShell\sitecore modules\Shell\PowerShell"
