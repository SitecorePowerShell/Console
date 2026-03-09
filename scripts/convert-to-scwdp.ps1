param(
    [Parameter(Mandatory)]
    [string]$Path,

    [Parameter(Mandatory)]
    [string]$Destination
)

<#
.SYNOPSIS
    Converts a Sitecore package (.zip) into a Web Deploy Package (.scwdp.zip).

.DESCRIPTION
    Self-contained replacement for SAT's ConvertTo-SCModuleWebDeployPackage.
    Builds the SCWDP structure with core.dacpac, MSDeploy metadata, and
    re-rooted website files from the nested package.zip.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName "System.IO.Compression"
Add-Type -AssemblyName "System.IO.Compression.FileSystem"

$scriptRoot = $PSScriptRoot
$projectRoot = (Resolve-Path "$scriptRoot/..").Path
$templateDir = Join-Path $projectRoot "_output\scwdp-template"

# Derive output filename: input "Foo-1.0-IAR.zip" -> "Foo-1.0-IAR.scwdp.zip"
$inputName = [System.IO.Path]::GetFileNameWithoutExtension($Path)
$scwdpName = "$inputName.scwdp.zip"
$scwdpPath = Join-Path $Destination $scwdpName

if (-not (Test-Path $Destination)) {
    New-Item -Path $Destination -ItemType Directory -Force | Out-Null
}

# Create temp working directory
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) "scwdp-$([Guid]::NewGuid())"
New-Item -Path $tempDir -ItemType Directory -Force | Out-Null

try {
    # --- Step 1: Build core.dacpac ---
    $dacpacPath = Join-Path $tempDir "core.dacpac"
    $dacpacTemp = Join-Path $tempDir "dacpac-staging"
    New-Item -Path $dacpacTemp -ItemType Directory -Force | Out-Null

    # Copy template files into dacpac staging, renaming Content_Types.xml -> [Content_Types].xml
    Copy-Item (Join-Path $templateDir "model.xml") $dacpacTemp
    Copy-Item (Join-Path $templateDir "DacMetadata.xml") $dacpacTemp
    Copy-Item (Join-Path $templateDir "Origin.xml") $dacpacTemp
    Copy-Item (Join-Path $templateDir "postdeploy.sql") $dacpacTemp
    Copy-Item (Join-Path $templateDir "Content_Types.xml") (Join-Path $dacpacTemp "[Content_Types].xml")

    [System.IO.Compression.ZipFile]::CreateFromDirectory($dacpacTemp, $dacpacPath)

    # --- Step 2: Generate MSDeploy metadata ---
    $archiveXml = @"
<?xml version="1.0" encoding="utf-8"?>
<sitemanifest>
  <dbDacFx path="core.dacpac" />
  <iisApp path="Website">
    <createApp path="Website" />
    <contentPath path="Website" />
  </iisApp>
</sitemanifest>
"@

    $parametersXml = @"
<?xml version="1.0" encoding="utf-8"?>
<parameters>
  <parameter name="Application Path" tags="iisapp">
    <parameterEntry type="ProviderPath" scope="iisapp" match="WebSite" />
  </parameter>
  <parameter name="Core Admin Connection String" tags="Hidden, SQLConnectionString, NoStore">
    <parameterEntry type="ProviderPath" scope="dbDacFx" match="core.dacpac" />
  </parameter>
</parameters>
"@

    $systemInfoXml = "<systemInfo />"

    Set-Content -Path (Join-Path $tempDir "archive.xml") -Value $archiveXml -Encoding UTF8
    Set-Content -Path (Join-Path $tempDir "parameters.xml") -Value $parametersXml -Encoding UTF8
    Set-Content -Path (Join-Path $tempDir "SystemInfo.xml") -Value $systemInfoXml -Encoding UTF8

    # --- Step 3: Extract files/* from nested package.zip -> Content/Website/ ---
    $websiteDir = Join-Path $tempDir "Content\Website"
    New-Item -Path $websiteDir -ItemType Directory -Force | Out-Null

    # Open the outer Sitecore package zip
    $outerZip = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $packageEntry = $outerZip.Entries | Where-Object { $_.Name -eq "package.zip" }
        if (-not $packageEntry) {
            throw "No package.zip found inside '$Path'"
        }

        # Open the nested package.zip from the stream
        $packageStream = $packageEntry.Open()
        $innerZip = New-Object System.IO.Compression.ZipArchive($packageStream, [System.IO.Compression.ZipArchiveMode]::Read)
        try {
            foreach ($entry in $innerZip.Entries) {
                # Only extract entries under "files/"
                if ($entry.FullName.StartsWith("files/", [StringComparison]::OrdinalIgnoreCase)) {
                    $relativePath = $entry.FullName.Substring(6) # strip "files/"
                    if ([string]::IsNullOrEmpty($relativePath)) { continue }

                    $destPath = Join-Path $websiteDir $relativePath

                    # Directory entries end with /
                    if ($entry.FullName.EndsWith("/")) {
                        New-Item -Path $destPath -ItemType Directory -Force | Out-Null
                    } else {
                        $parentDir = [System.IO.Path]::GetDirectoryName($destPath)
                        if (-not (Test-Path $parentDir)) {
                            New-Item -Path $parentDir -ItemType Directory -Force | Out-Null
                        }
                        $entryStream = $entry.Open()
                        try {
                            $fileStream = [System.IO.File]::Create($destPath)
                            try {
                                $entryStream.CopyTo($fileStream)
                            } finally {
                                $fileStream.Dispose()
                            }
                        } finally {
                            $entryStream.Dispose()
                        }
                    }
                }
            }
        } finally {
            $innerZip.Dispose()
        }
    } finally {
        $outerZip.Dispose()
    }

    # --- Step 4: Remove dacpac staging dir before final zip ---
    Remove-Item -Path $dacpacTemp -Recurse -Force

    # --- Step 5: Create the .scwdp.zip ---
    if (Test-Path $scwdpPath) {
        Remove-Item $scwdpPath -Force
    }
    [System.IO.Compression.ZipFile]::CreateFromDirectory($tempDir, $scwdpPath)

    Write-Host "Created SCWDP: $scwdpPath"
    return $scwdpPath

} finally {
    # Clean up temp directory
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force
    }
}
