<#
    .SYNOPSIS
        Deserializes sitecore item from server disk drive.

    .DESCRIPTION
        Deserialization of items with Sitecore Powershell Extensions uses Deserialize-Item command. The simplest syntax requires 2 parameters:
        -Path - which is a path to the item on the drive but without .item extension. If the item does not exist in the Sitecore tree yet, you need to pass the parent item path.
	-Root - the directory which is the root of serialization. Trailing slash \ character is required, 
	
	e.g.:
	
	Deserialize-Item -Path "c:\project\data\serialization\master\sitecore\content\articles" -Root "c:\project\data\serialization\"

    .PARAMETER Database
        Database to contain the item to be deserialized.

    .PARAMETER Item
        The item to be serialized.

    .PARAMETER Preset
        Name of the preset to be deserialized.

    .PARAMETER Path
        Path to the item on the drive but without .item extension. If the item does not exist in the Sitecore tree yet, you need to pass the parent item path.

    .PARAMETER Recurse
        If included in the execution - dederializes both the item and all of its children.

    .PARAMETER Root
        The directory which is the root of serialization. Trailing slash \ character is required. if not specified the default root will be used.

    .PARAMETER UseNewId
        Tells Sitecore if each of the items should be created with a newly generated ID, e.g.
        Deserialize-Item -path "c:\project\data\serialization\master\sitecore\content\articles" -root "c:\project\data\serialization\" -usenewid -recurse

    .PARAMETER DisableEvents
        If set Sitecore will use EventDisabler during deserialization, e.g.:
        Deserialize-Item -path "c:\project\data\serialization\master\sitecore\content\articles" -root "c:\project

    .PARAMETER ForceUpdate
        Forces item to be updated even if it has not changed.
    
    .INPUTS        
    
    .OUTPUTS
        System.Void

    .NOTES
        Help Author: Marek Musielak, Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .LINK
        Serialize-Item

    .LINK
        Get-Preset

    .LINK
        http://www.cognifide.com/blogs/sitecore/serialization-and-deserialization-with-sitecore-powershell-extensions/

    .LINK
        https://gist.github.com/AdamNaj/6c86f61510dc3d2d8b2f

    .LINK
        http://stackoverflow.com/questions/20266841/sitecore-powershell-deserialization

    .LINK
        http://stackoverflow.com/questions/20195718/sitecore-serialization-powershell

    .LINK
        http://stackoverflow.com/questions/20283438/sitecore-powershell-deserialization-core-db
        
    .EXAMPLE
        PS master:\> Deserialize-Item -path "c:\project\data\serialization\master\sitecore\content\articles" -root "c:\project\data\serialization\"

    .EXAMPLE
        PS master:\> Deserialize-Item -path "c:\project\data\serialization\master\sitecore\content\articles" -root "c:\project\data\serialization\" -recurse


#>
