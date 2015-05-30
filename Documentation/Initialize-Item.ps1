<#
    .SYNOPSIS
        Initializes items with the PowerShell automatic properties for each field.

    .DESCRIPTION
        The Initialize-Item command wraps Sitecore item with PowerShell property equivalents of fields for easy assignment of values to fields and automatic saving.
        This command can also be used to translate the the "Sitecore.ContentSearch.SearchTypes.SearchResultItem" items obtained from the Find-Item command into full Sitecore Items.
        The alias for the command is Wrap-Item.

    .PARAMETER Item
        The item to be wrapped/initialized.

    .PARAMETER SearchResultItem
        The item obtained from Find-Item command to be translated into a sitecore item.
    
    .INPUTS
        Sitecore.Data.Items.Item
        Sitecore.ContentSearch.SearchTypes.SearchResultItem
    
    .OUTPUTS
        Sitecore.Data.Items.Item

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Find-Item

    .LINK
        Get-Item

    .LINK
        Get-ChildItem

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        #Initialize the item obtained directly through the Sitecore API with additional PowerShell properties

        $item = [Sitecore.Configuration.Factory]::GetDatabase("master").GetItem("/sitecore/content/home");
        #So far the item does not have PowerShell instrumentation wrapped around it yet - the following like wraps $item in those additional properties
        $item = Initialize-Item -Item $item
        # The following line will assign text to the field named MyCustomeTextField and persist the item into the database automatically using the added PowerShell property.
        $item.Title = "New Title"
#>
