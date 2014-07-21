<#
    .SYNOPSIS
        Wraps Sitecore item with PowerShell property equivalents of fields for easy assignment of values to fields and automatic saving.

    .DESCRIPTION
        Wraps Sitecore item with PowerShell property equivalents of fields for easy assignment of values to fields and automatic saving.


    .PARAMETER Item
        The item to be wrapped.    
    
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Data.Items.Item

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\> $item = Wrap-Item -Path master:\content\home
	# The following line will assign text to the field named MyCustomeTextField and persist the item into the database automatically.
        PS master:\> $item.MyCustomeTextField = "New Text"
#>
