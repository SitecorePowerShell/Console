Import-Module -Name SPE -Force

$props = @{
    Username = "admin"
    Password = "b"
    ConnectionUri = "http://console"
    Verbose = $true
}

$receiveProps = @{
    Destination = "C:\temp"
    Force = $true    
}

$receiveProps += $props

# Download single file
Receive-RemoteItem @receiveProps -Path "default.js" -RootPath App
Receive-RemoteItem @receiveProps -Path "license.xml" -RootPath Data
Receive-RemoteItem @receiveProps -Path "readme.txt" -RootPath Debug
Receive-RemoteItem @receiveProps -Path "sitecore_master_index\segments.gen" -RootPath Index
Receive-RemoteItem @receiveProps -Path "xmlcontrol.aspx" -RootPath Layout
Receive-RemoteItem @receiveProps -Path "readme.txt" -RootPath Log
Receive-RemoteItem @receiveProps -Path "readme.txt" -RootPath Media
Receive-RemoteItem @receiveProps -Path "SPE Remoting-3.2.zip" -RootPath Package
Receive-RemoteItem @receiveProps -Path "master\sitecore\templates\Modules.item" -RootPath Serialization
Receive-RemoteItem @receiveProps -Path "earth_link32x32.png" -RootPath Temp

# Download single file using full qualified path
Receive-RemoteItem @receiveProps -Path "C:\inetpub\wwwroot\Console\Data\tools\phantomjs\sc-ee.js"

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

# Download media items
Receive-RemoteItem @receiveProps -Path "/sitecore/media library/Default Website/cover/" -Database master -Container
Receive-RemoteItem @receiveProps -Path "/Default Website/cover" -Database master
Receive-RemoteItem @receiveProps -Path "{04DAD0FD-DB66-4070-881F-17264CA257E1}" -Database master

# Download multiple media items
Invoke-RemoteScript @props -ScriptBlock { 
    Get-ChildItem -Path "master:/sitecore/media library/" -Recurse | Where-Object { $_.Size -gt 0 } | 
        Select-Object -First 10 | Select-Object -Expand ItemPath 
} | Receive-RemoteItem @receiveProps -Database master