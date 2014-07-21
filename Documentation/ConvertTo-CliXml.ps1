<#
    .SYNOPSIS
        Returns an XML-based representation of an object or objects.

    .DESCRIPTION
        The ConvertTo-CliXml cmdlet returns an XML-based representation of an object or objects provided as InputObject parameter. You can then use the ConvertFrom-CliXml cmdlet to re-create the saved object based on the contents of that XML.

	This cmdlet is similar to ConvertTo-XML, except that ConvertTo-CliXml stores the resulting XML in a string. ConvertTo-XML returns the XML, so you can continue to process it in Windows PowerShell.


    .PARAMETER InputObject
        Specifies the object to be converted. Enter a variable that contains the objects, or type a command or expression that gets the objects. You can also pipe objects to ConvertTo-CliXml.
    
    .INPUTS
        object
    
    .OUTPUTS
        System.String

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .LINK
        ConvertFrom-CliXml

    .LINK
        ConvertFrom-Xml

    .LINK
        ConvertTo-Xml

    .LINK
        Export-CliXml

    .LINK
        Import-CliXml

    .EXAMPLE
        PS master:\> #Convert original item to xml
        PS master:\> $myCliXmlItem = Get-Item -Path master:\content\home | ConvertTo-CliXml 
        PS master:\> #print the CliXml
        PS master:\> $myCliXmlItem
        PS master:\> #print the Item converted back from CliXml
        PS master:\> $myCliXmlItem | ConvertFrom-CliXml
#>
