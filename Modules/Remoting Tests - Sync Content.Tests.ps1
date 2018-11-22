param(
    [Parameter()]
    [string]$protocolHost = "https://spe.dev.local"
)

Import-Module -Name SPE -Force

# Download single file
Describe "Download with RemoteScriptCall" {
    BeforeEach {
        $destinationMediaPath = "C:\temp\spe-test\"
        if(Test-Path -Path $destinationMediaPath) {
            Remove-Item -Path $destinationMediaPath -Recurse
        }
        New-Item -Path $destinationMediaPath -ItemType Directory | Out-Null

        $session = New-ScriptSession -Username "sitecore\admin" -Password "b" -ConnectionUri $protocolHost
    }
    AfterEach {
        Stop-ScriptSession -Session $session
    }
    Context "Single File" {
        It "download from the App root path" {
            $filename = "default.js"
            $destination = Join-Path -Path $destinationMediaPath -ChildPath $filename
            Receive-RemoteItem -Session $session -Destination $destination -Path $filename -RootPath App
            Test-Path -Path $destination | Should Be $true
        }
        It "download from the Data root path" {
            $filename = "license.xml"
            $destination = Join-Path -Path $destinationMediaPath -ChildPath $filename
            Receive-RemoteItem -Session $session -Destination $destination -Path $filename -RootPath Data
            Test-Path -Path $destination | Should Be $true
        }
        It "download from the Debug root path" {
            $filename = "readme.txt"
            $destination = Join-Path -Path $destinationMediaPath -ChildPath $filename
            Receive-RemoteItem -Session $session -Destination $destination -Path $filename -RootPath Debug
            Test-Path -Path $destination | Should Be $true
        }
        It "download from the Index root path" {
            $filename = "sitecore_master_index\segments.gen"
            $destination = Join-Path -Path $destinationMediaPath -ChildPath $filename
            Receive-RemoteItem -Session $session -Destination $destination -Path $filename -RootPath Index
            Test-Path -Path $destination | Should Be $true
        }
        It "download from the Layout root path" {
            $filename = "xmlcontrol.aspx"
            $destination = Join-Path -Path $destinationMediaPath -ChildPath $filename
            Receive-RemoteItem -Session $session -Destination $destination -Path $filename -RootPath Layout
            Test-Path -Path $destination | Should Be $true
        }
        It "download from the Log root path" {
            $filename = "SPE.log.$(Get-Date -Format 'yyyyMMdd').txt"
            $destination = Join-Path -Path $destinationMediaPath -ChildPath $filename
            Receive-RemoteItem -Session $session -Destination $destination -Path $filename -RootPath Log
            Test-Path -Path $destination | Should Be $true
        }
        It "download from the Media root path" {
            $filename = "readme.txt"
            $destination = Join-Path -Path $destinationMediaPath -ChildPath $filename
            Receive-RemoteItem -Session $session -Destination $destination -Path $filename -RootPath Media
            Test-Path -Path $destination | Should Be $true
        }
        It "download from the Package root path" {
            $filename = "readme.txt"
            $destination = Join-Path -Path $destinationMediaPath -ChildPath $filename
            Receive-RemoteItem -Session $session -Destination $destination -Path $filename -RootPath Package
            Test-Path -Path $destination | Should Be $true
        }
        It "download from the Serialization root path" {
            $filename = "master\sitecore\templates\Modules.item"
            $destination = Join-Path -Path $destinationMediaPath -ChildPath $filename
            Receive-RemoteItem -Session $session -Destination $destination -Path $filename -RootPath Serialization
            Test-Path -Path $destination | Should Be $true
        }
        It "download from the Temp root path" {
            $filename = "readme.txt"
            $destination = Join-Path -Path $destinationMediaPath -ChildPath $filename
            Receive-RemoteItem -Session $session -Destination $destination -Path $filename -RootPath Temp
            Test-Path -Path $destination | Should Be $true
        }
    }
}

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