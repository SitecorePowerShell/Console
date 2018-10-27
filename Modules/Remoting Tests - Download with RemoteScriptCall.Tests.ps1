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
            $filename = "readme.txt"
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
            $filename = "kitten.jpg"
            $pathFolder = Join-Path -Path $PSScriptRoot -ChildPath "spe-test"
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
    Context "Advanced/mixed scenarios" {
        It "Download first 3 log files" {
            $files = Invoke-RemoteScript -Session $session -ScriptBlock { 
                Get-ChildItem -Path "$($SitecoreLogFolder)" | Where-Object { !$_.PSIsContainer } | Select-Object -Expand Name -First 3
            } 
            
            $files | 
                ForEach-Object { 
                    $destination = Join-Path -Path $destinationMediaPath -ChildPath $_
                    Receive-RemoteItem -Session $session -Destination $destination -Path $_ -RootPath Log
                    Test-Path -Path $destination | Should Be $true
                }
        }

        It "Download all SPE log files as ZIP" {
            $archiveFileName = Invoke-RemoteScript -Session $session -ScriptBlock { 
                Import-Function -Name Compress-Archive
                
                Get-ChildItem -Path "$($SitecoreLogFolder)" -File | Where-Object { $_.Name -match "spe.log." } | 
                    Compress-Archive -DestinationPath "$($SitecoreTempFolder)\archived.SPE.logs.zip" | Select-Object -Expand FullName
            } 

            $archiveFileName | Should Not Be $null

            $destination = Join-Path -Path $destinationMediaPath -ChildPath (Split-Path -Path $archiveFileName -Leaf)
            Receive-RemoteItem -Session $session -Destination $destination -Path $archiveFileName 

            Test-Path -Path $destination | Should Be $true

            Invoke-RemoteScript -Session $session -ScriptBlock { 
                Remove-Item -Path "$($using:archiveFileName)"
                Test-Path "$($using:archiveFileName)"
            } | Should Be $false
        }
        It "Download first 10 Media Items from Media Library" {
            $mediaItemNames = Invoke-RemoteScript -Session $session -ScriptBlock { 
                Get-ChildItem -Path "master:/sitecore/media library/" -Recurse | Where-Object { $_.Size -gt 0 } | 
                Select-Object -First 10 | Foreach-Object { "$($_.ItemPath).$($_.Extension)" } 
            } 

            $mediaItemNames | Foreach-Object { 
                $source= Join-Path ([System.IO.Path]::GetDirectoryName($_)) ([System.IO.Path]::GetFileNameWithoutExtension($_))
                $destination = Join-Path -Path $destinationMediaPath -ChildPath $_
                Receive-RemoteItem -Session $session -Destination $destination -Path $source -Database master
                Test-Path -Path $destination | Should Be $true
            }
        }
    }
}