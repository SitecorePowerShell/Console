<#
    .SYNOPSIS
        Send an object content to the client

    .DESCRIPTION
        The cmdlet allows to send content of an object (FileInfo, Stream, String, String[] or Byte[]) to the client. This is used for example by report scripts to send the report in HTML, Json or Excel without saving the content of the object to the disk drive.
	You can specify an object type and file name to make sure the downloaded file is interpreted properly by the browser.

    .PARAMETER InputObject
        Object content to be sent to the client. Object must be of one of the following types:
        - FileInfo, 
        - Stream, 
        - String, 
        - String[], 
        - Byte[] 

    .PARAMETER ContentType
        The MIME content type of the object. In most cases you can skip this parameter and still have the content type be deduced by the browser from the 

        Common examples (after Wikipedia)
	- application/json
	- application/x-www-form-urlencoded
	- application/pdf
	- application/octet-stream
	- multipart/form-data
	- text/html
	- image/png
	- image/jpg

    .PARAMETER Name
        Name of the file you want the user browser to save the object as.
    
    .INPUTS
        System.Object
    
    .OUTPUTS
        System.Boolean

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        #Send first log file to the user
        Get-Item "$SitecoreLogFolder\*.*" | select -first 1 | Out-Download

    .EXAMPLE
        #Send Hello World text file to the user
        "Hello World!" | Out-Download -Name hello-world.txt

    .EXAMPLE
        #Get a list of sitecore branches under root item in the master database and send the list to user as excel file
        Import-Function -Name ConvertTo-Xlsx

        [byte[]]$outobject = Get-ChildItem master:\ | 
            Select-Object -Property Name, ProviderPath, Language, Varsion | 
            ConvertTo-Xlsx 

        Out-Download -Name "report-$datetime.xlsx" -InputObject $outobject
#>
