param(
    [Parameter()]
    [string]$protocolHost = "https://spe.dev.local"
)

Import-Module -Name SPE -Force

if(!$protocolHost){
    $protocolHost = "https://spe.dev.local"
}

Describe "Upload with RemoteScriptCall" {
    BeforeEach {
        $sharedSecret = '7AF6F59C14A05786E97012F054D1FB98AC756A2E54E5C9ACBAEE147D9ED0E0DB'
        #$session = New-ScriptSession -Username "sitecore\admin" -Password "b" -ConnectionUri $protocolHost
        $session = New-ScriptSession -Username "sitecore\admin" -SharedSecret $sharedSecret -ConnectionUri $protocolHost
        $localFilePath = Join-Path -Path $PSScriptRoot -ChildPath "spe-test"
    }
    AfterEach {
        Invoke-RemoteScript -Session $session -ScriptBlock { Remove-Item -Path "master:\media library\images\spe-test\" -Recurse }
        Stop-ScriptSession -Session $session
    }
    Context "Upload files with RemoteScriptCall" {
        It "upload to the App root path" {
            $filename = "data.xml"
            Get-Item -Path "$($localFilePath)\$($filename)" | 
                Send-RemoteItem -Session $session -RootPath App
            Invoke-RemoteScript -Session $session -ScriptBlock { Test-Path -Path "$($AppPath)\$($using:filename)" } | Should Be $true
        }
        It "upload to the Package root path" {
            $filename = "data.xml"
            Get-Item -Path "$($localFilePath)\$($filename)" | 
                Send-RemoteItem -Session $session -RootPath Package -Destination "\"
            Invoke-RemoteScript -Session $session -ScriptBlock { Test-Path -Path "$($SitecorePackageFolder)\$($using:filename)" } | Should Be $true
        }
        It "upload to the Media Library" {
            $filename = "kitten.jpg"
            $filenameWithoutExtension = [System.IO.Path]::GetFileNameWithoutExtension($filename)
            Get-Item -Path "$($localFilePath)\$($filename)" | 
                Send-RemoteItem -Session $session -RootPath Media -Destination "Images/spe-test"
            Invoke-RemoteScript -Session $session -ScriptBlock { Test-Path -Path "master:\media library\images\spe-test\$($using:filenameWithoutExtension)" } | Should Be $true
        }
        It "upload to the Media Library with different name" {
            $filename = "kitten.jpg"
            $filenameWithoutExtension = [System.IO.Path]::GetFileNameWithoutExtension($filename)
            Get-Item -Path "$($localFilePath)\$($filename)" | 
                Send-RemoteItem -Session $session -RootPath Media -Destination "Images/spe-test/kitten1.jpg"
            Invoke-RemoteScript -Session $session -ScriptBlock { Test-Path -Path "master:\media library\images\spe-test\$($using:filenameWithoutExtension)1" } | Should Be $true
        }
        It "upload to the Media Library and replace using a guid" {
            $filename = "kitten.jpg"
            $filenameReplacement = "kitten-replacement.jpg"
            $filenameWithoutExtension = [System.IO.Path]::GetFileNameWithoutExtension($filename)
            Get-Item -Path "$($localFilePath)\$($filename)" | 
                Send-RemoteItem -Session $session -RootPath Media -Destination "Images/spe-test/"
            # Verify the file was uploaded
            Invoke-RemoteScript -Session $session -ScriptBlock { Test-Path -Path "master:\media library\images\spe-test\$($using:filenameWithoutExtension)" } | Should Be $true
            # Keep track of the current Id and Size
            $details = Invoke-RemoteScript -Session $session -ScriptBlock { 
                $item = Get-Item -Path "master:media library\images\spe-test\kitten"
                [PSCustomObject]@{
                    "Id" = $item.ID
                    "Size" = $item.Size
                }
            }

            # Verify that we can get details about the file
            $details | Should Not Be $null            
            Get-Item -Path "$($localFilePath)\$($filenameReplacement)" | Send-RemoteItem -Session $session -RootPath Media -Destination $details.Id
            # Verify that the file size has changed
            $details2 = Invoke-RemoteScript -Session $session -ScriptBlock { 
                $item = Get-Item -Path "master:media library\images\spe-test\kitten"
                [PSCustomObject]@{
                    "Id" = $item.ID
                    "Size" = $item.Size
                }
            }
            $details.Size | Should Not Be $details2.Size
        }
        It "upload to the Media Library a compressed archive" {
            $filename = "kittens.zip"
            Get-Item -Path "$($localFilePath)\$($filename)" | 
                Send-RemoteItem -Session $session -RootPath Media -Destination "Images/spe-test"
            Invoke-RemoteScript -Session $session -ScriptBlock { 
                Get-ChildItem -Path "master:\media library\images\spe-test\" -Recurse | Measure-Object | Select-Object -ExpandProperty Count
            } | Should Be 5
        }
    }
}

exit

# Upload single file using full qualified path
Send-RemoteItem @props -Path "C:\temp\data.xml" -Destination "C:\inetpub\wwwroot\Console\Website\upload\data1.xml"

Get-Item -Path C:\temp\data.zip | Send-RemoteItem @props -RootPath App -Destination "\upload"

# Upload multiple files in a flat structure
Get-ChildItem -Path "C:\temp\" -Filter "*.xml" | Send-RemoteItem -Session $session -RootPath Media -Destination "Files/" -Verbose

# Upload multiple files in a compressed zip to maintain directory structure
Get-Item -Path C:\temp\Kittens.zip | Send-RemoteItem @props -RootPath Media -Destination "Images/" -SkipExisting
