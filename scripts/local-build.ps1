# Build the SPE solution using the locally installed MSBuild.
# Uses vswhere to find MSBuild. Downloads nuget.exe on demand to
# restore packages.config packages (cached in .tools/).

$repoRoot = (Get-Item "$PSScriptRoot\..").FullName
$sln = "$repoRoot\Spe.sln"
$nugetConfig = "$repoRoot\src\NuGet.config"
$toolsDir = "$repoRoot\.tools"
$nugetExe = "$toolsDir\nuget.exe"

# Find MSBuild via vswhere
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vswhere)) {
    Write-Error "vswhere not found at $vswhere - is Visual Studio installed?"
    exit 1
}

$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild `
    -find 'MSBuild\**\Bin\MSBuild.exe' 2>$null | Select-Object -First 1

if (-not $msbuild) {
    Write-Error "MSBuild not found. Install Visual Studio with the MSBuild workload."
    exit 1
}

# Download nuget.exe on first use
if (-not (Test-Path $nugetExe)) {
    Write-Host "Downloading nuget.exe..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Path $toolsDir -Force | Out-Null
    Invoke-WebRequest -Uri 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe' -OutFile $nugetExe
}

# Restore NuGet packages
& $nugetExe restore $sln -ConfigFile $nugetConfig -Verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Error "NuGet restore failed."
    exit $LASTEXITCODE
}

# Rebuild (Clean + Build) to avoid stale obj cache issues.
# MSBuild's incremental build skips recompilation when obj timestamps
# are newer than source files, which can happen after git operations
# (stash, checkout) or post-edit hooks that rewrite files.
Write-Host "Using $msbuild" -ForegroundColor Cyan
& $msbuild $sln -t:Rebuild -p:Configuration=Debug -verbosity:minimal
exit $LASTEXITCODE
