# Remoting Tests - Sync Content (Download with RemoteScriptCall)
# Converted from Pester to custom assert format

Write-Host "`n  [Sync Content - Single File Download]" -ForegroundColor White

$destinationMediaPath = "C:\temp\spe-test\"
if(Test-Path -Path $destinationMediaPath) {
    Remove-Item -Path $destinationMediaPath -Recurse
}
New-Item -Path $destinationMediaPath -ItemType Directory | Out-Null

$session = New-ScriptSession -Username "sitecore\admin" -Password "b" -ConnectionUri $protocolHost

$rootPaths = @("App", "Data", "Debug", "Index", "Layout", "Log", "Media", "Package", "Serialization", "Temp")
$filenames = @{
    App           = "default.js"
    Data          = "license.xml"
    Debug         = "readme.txt"
    Index         = "sitecore_master_index\segments.gen"
    Layout        = "xmlcontrol.aspx"
    Log           = "SPE.log.$(Get-Date -Format 'yyyyMMdd').txt"
    Media         = "readme.txt"
    Package       = "readme.txt"
    Serialization = "master\sitecore\templates\Modules.item"
    Temp          = "readme.txt"
}

foreach ($rootPath in $rootPaths) {
    $filename = $filenames[$rootPath]
    $destination = Join-Path -Path $destinationMediaPath -ChildPath $filename
    Receive-RemoteItem -Session $session -Destination $destination -Path $filename -RootPath $rootPath
    Assert-True (Test-Path -Path $destination) "download from the $rootPath root path"
}

Stop-ScriptSession -Session $session

# --- Copy-RainbowContent section (non-test, utility usage) ---

$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
& "$scriptDir\..\Examples\Copy-RainbowContent.ps1"

$sourceSession = New-ScriptSession -user "admin" -pass "b" -conn "https://sc827.dev.local"
$destinationSession = New-ScriptSession -user "admin" -pass "b" -conn "https://sc826.dev.local"

$copyProps = @{
    SourceSession = $sourceSession
    DestinationSession = $destinationSession
}

# Content
$rootId = "{37D08F47-7113-4AD6-A5EB-0C0B04EF6D05}"

# Migrate a single item only if it's missing
#Copy-RainbowContent @copyProps -RootId $rootId

# Migrate a single item and overwrite if it exists
#Copy-RainbowContent @copyProps -RootId $rootId -Overwrite

# Migrate all items only if they are missing
#Copy-RainbowContent @copyProps -RootId $rootId -Recurse

# Migrate all items overwriting if they exist
#Copy-RainbowContent @copyProps -RootId $rootId -Overwrite -Recurse

# Migrate all items overwriting if they exist
Copy-RainbowContent @copyProps -RootId $rootId -Overwrite -Recurse -RemoveNotInSource

# Migrate all items skipping if they exist
#Copy-RainbowContent @copyProps -RootId $rootId -SingleRequest

# Migrate all items overwriting if they exist
#Copy-RainbowContent @copyProps -RootId $rootId -Overwrite -SingleRequest

# Images
$rootId = "{15451229-7534-44EF-815D-D93D6170BFCB}"

#Copy-RainbowContent @copyProps -RootId "{15451229-7534-44EF-815D-D93D6170BFCB}"

#Copy-RainbowContent @copyProps -RootId "{15451229-7534-44EF-815D-D93D6170BFCB}" -Overwrite -Recurse
