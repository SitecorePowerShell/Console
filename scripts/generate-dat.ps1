# Generates .dat files from serialized content using Sitecore CLI
Push-Location (Join-Path $PSScriptRoot "..\serialization")
try {
    dotnet tool restore
    dotnet sitecore plugin list
    dotnet sitecore itemres create -o _out/spe --overwrite -i Spe.*
    Remove-Item -Path ".\_out\items.web.spe.dat" -Force -ErrorAction SilentlyContinue
    Copy-Item ".\_out\items.master.spe.dat" ".\_out\items.web.spe.dat"
}
finally {
    Pop-Location
}
