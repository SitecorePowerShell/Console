Clear-Host

Import-Module -Name SPE -Force

$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
& $scriptDir\Copy-RainbowContent.ps1

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