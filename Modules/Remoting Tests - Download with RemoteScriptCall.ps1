Import-Module -Name SPE -Force
Import-Module -Name Pester -Force

# Download single file
Describe "Download with RemoteScriptCall" {
    BeforeEach {
        $destinationMediaPath = "C:\temp\spe-test\"
        if(Test-Path -Path $destinationMediaPath) {
            Remove-Item -Path $destinationMediaPath -Recurse
        }
        New-Item -Path $destinationMediaPath -ItemType Directory | Out-Null

        $session = New-ScriptSession -Username "sitecore\admin" -Password "b" -ConnectionUri "https://spe.dev.local"
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
            $filename = "SPE Remoting-3.2.zip"
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
    Context "Single fully qualified file" {
        It "download fully qualified file" {
            $filename = "sc-ee.js"
            $pathFolder = "C:\Websites\spe.dev.local\Data\tools\phantomjs\"
            $path = Join-Path -Path $pathFolder -ChildPath $filename
            $destination = Join-Path -Path $destinationMediaPath -ChildPath $filename
            Receive-RemoteItem -Session $session -Path $path -Destination $destination
            Test-Path -Path $destination | Should Be $true            
        }
    }
    Context "Media item" {
        It "download from relative media path in master" {
            $filename = "cover.jpg"
            $mediaitem = "/Default Website/cover"
            $destination = Join-Path -Path $destinationMediaPath -ChildPath $filename
            Receive-RemoteItem -Session $session -Path $mediaitem -Destination $destinationMediaPath -Database master
            Test-Path -Path $destination | Should Be $true
        }
        It "download from fully qualified media path in master" {
            $filename = "cover.jpg"
            $mediaitem = "/sitecore/media library/Default Website/cover/"
            $destination = Join-Path -Path $destinationMediaPath -ChildPath $filename
            Receive-RemoteItem -Session $session -Path $mediaitem -Destination $destinationMediaPath -Database master
            Test-Path -Path $destination | Should Be $true
        }
        It "download from media path in master maintaining structure" {
            $filename = "\Default Website\cover.jpg"
            $mediaitem = "/Default Website/cover/"
            $destination = Join-Path -Path $destinationMediaPath -ChildPath $filename
            Receive-RemoteItem -Session $session -Path $mediaitem -Destination $destinationMediaPath -Database master -Container
            Test-Path -Path $destination | Should Be $true
        }
        It "download from media path with GUID in master changing filename" {
            $filename = "cover1.jpg"
            $mediaitem = "{04DAD0FD-DB66-4070-881F-17264CA257E1}"
            $destination = Join-Path -Path $destinationMediaPath -ChildPath $filename
            $destinationChanged = Join-Path -Path $destinationMediaPath -ChildPath $filename
            Receive-RemoteItem -Session $session -Path $mediaitem -Destination $destinationChanged -Database master
            Test-Path -Path $destination | Should Be $true
        }
    }
}

exit
# Download multiple files using the filename.
Invoke-RemoteScript @props -ScriptBlock { 
    Get-ChildItem -Path "$($SitecoreLogFolder)" | Where-Object { !$_.PSIsContainer } | Select-Object -Expand Name 
} | Receive-RemoteItem @receiveProps -RootPath Log

# Download single zip file using the fully qualified name.
Invoke-RemoteScript @props -ScriptBlock {
    Import-Function -Name Compress-Archive
    Get-ChildItem -Path "$($SitecoreLogFolder)" | Where-Object { !$_.PSIsContainer } | 
        Compress-Archive -DestinationPath "$($SitecoreDataFolder)archived.zip" | Select-Object -Expand FullName
} | Receive-RemoteItem @receiveProps

# Download multiple media items
Invoke-RemoteScript @props -ScriptBlock { 
    Get-ChildItem -Path "master:/sitecore/media library/" -Recurse | Where-Object { $_.Size -gt 0 } | 
        Select-Object -First 10 | Select-Object -Expand ItemPath 
} | Receive-RemoteItem @receiveProps -Database master