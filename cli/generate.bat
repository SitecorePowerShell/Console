cd /D "%~dp0"
dotnet tool restore
dotnet sitecore plugin list
dotnet sitecore itemres create -o _out/spe --overwrite -i Spe.*