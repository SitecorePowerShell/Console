cd /D "%~dp0"
dotnet tool restore
dotnet sitecore plugin list
dotnet sitecore itemres create -o _out/spe --overwrite -i Spe.*
del /f .\_out\items.web.spe.dat 2>nul
xcopy .\_out\items.master.spe.dat .\_out\items.web.spe.dat* /Y