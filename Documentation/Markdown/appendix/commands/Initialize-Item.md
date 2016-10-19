# Initialize-Item 
 
Initializes items with the PowerShell automatic properties for each field. 
 
## Syntax 
 
Initialize-Item -Item &lt;Item&gt; 
 
Initialize-Item -SearchResultItem &lt;SearchResultItem&gt; 
 
 
## Detailed Description 
 
The Initialize-Item command wraps Sitecore item with PowerShell property equivalents of fields for easy assignment of values to fields and automatic saving.
This command can also be used to translate the the "Sitecore.ContentSearch.SearchTypes.SearchResultItem" items obtained from the Find-Item command into full Sitecore Items.
The alias for the command is Wrap-Item. 
 
© 2010-2016 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions## Aliases
The following abbreviations are aliases for this cmdlet:  
* Wrap-Item 
 
## Parameters 
 
### -Item&nbsp; &lt;Item&gt; 
 
The item to be wrapped/initialized. 
 
<table>
    <thead></thead>
    <tbody>
        <tr>
            <td>Aliases</td>
            <td></td>
        </tr>
        <tr>
            <td>Required?</td>
            <td>true</td>
        </tr>
        <tr>
            <td>Position?</td>
            <td>named</td>
        </tr>
        <tr>
            <td>Default Value</td>
            <td></td>
        </tr>
        <tr>
            <td>Accept Pipeline Input?</td>
            <td>true (ByValue, ByPropertyName)</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
### -SearchResultItem&nbsp; &lt;SearchResultItem&gt; 
 
The item obtained from Find-Item command to be translated into a sitecore item. 
 
<table>
    <thead></thead>
    <tbody>
        <tr>
            <td>Aliases</td>
            <td></td>
        </tr>
        <tr>
            <td>Required?</td>
            <td>true</td>
        </tr>
        <tr>
            <td>Position?</td>
            <td>named</td>
        </tr>
        <tr>
            <td>Default Value</td>
            <td></td>
        </tr>
        <tr>
            <td>Accept Pipeline Input?</td>
            <td>true (ByValue)</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
## Inputs 
 
The input type is the type of the objects that you can pipe to the cmdlet. 
 
* Sitecore.Data.Items.Item
Sitecore.ContentSearch.SearchTypes.SearchResultItem 
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* Sitecore.Data.Items.Item 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 
 
Initialize the item obtained directly through the Sitecore API with additional PowerShell properties 
 
```powershell   
 
$item = [Sitecore.Configuration.Factory]::GetDatabase("master").GetItem("/sitecore/content/home");
#So far the item does not have PowerShell instrumentation wrapped around it yet - the following like wraps $item in those additional properties
$item = Initialize-Item -Item $item
# The following line will assign text to the field named MyCustomeTextField and persist the item into the database automatically using the added PowerShell property.
$item.Title = "New Title" 
 
``` 
 
## Related Topics 
 
* [Find-Item](/appendix/commands/Find-Item.md)* Get-Item* Get-ChildItem* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>
