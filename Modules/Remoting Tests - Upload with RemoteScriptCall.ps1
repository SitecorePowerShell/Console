Import-Module -Name SPE -Force

$props = @{
    Session = (New-ScriptSession -Username "admin" -Password "b" -ConnectionUri "http://console")
    Verbose = $true
}

# Upload single file
Get-Item -Path C:\temp\data.xml | Send-RemoteItem @props -RootPath App
Get-Item -Path C:\temp\data.xml | Send-RemoteItem @props -RootPath Package -Destination "\"
Get-Item -Path C:\temp\largeimage.jpg | Send-RemoteItem @props -RootPath App -Destination "\upload\images\"
Get-Item -Path C:\image.png | Send-RemoteItem @props -RootPath Media -Destination "Images/"
Get-Item -Path C:\image.png | Send-RemoteItem @props -RootPath Media -Destination "/sitecore/media library/Images/image2.png"
Get-Item -Path C:\temp\cover.jpg | Send-RemoteItem @props -Destination "{04DAD0FD-DB66-4070-881F-17264CA257E1}"

# Upload single file using full qualified path
Send-RemoteItem @props -Path "C:\temp\data.xml" -Destination "C:\inetpub\wwwroot\Console\Website\upload\data1.xml"

Get-Item -Path C:\temp\data.zip | Send-RemoteItem @props -RootPath App -Destination "\upload"

# Upload multiple files while maintaining directory structure