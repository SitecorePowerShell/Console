# Get-SearchIndex 
 
Returns the available Sitecore indexes. 
 
## Syntax 
 
Get-SearchIndex [[-Name] &lt;String&gt;] 
 
 
## Detailed Description 
 
The Get-SearchIndex command returns the available Sitecore indexes. These are the same as those found in the Control Panel. 
 
© 2010-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Name&nbsp; &lt;String&gt; 
 
Name of the index to return. 
 
<table>
    <thead></thead>
    <tbody>
        <tr>
            <td>Aliases</td>
            <td></td>
        </tr>
        <tr>
            <td>Required?</td>
            <td>false</td>
        </tr>
        <tr>
            <td>Position?</td>
            <td>1</td>
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
 
## Inputs 
 
The input type is the type of the objects that you can pipe to the cmdlet. 
 
* None or System.String 
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* Sitecore.ContentSearch.ISearchIndex 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 1 
 
 
 
```powershell   
 
The following lists all available indexes.

PS master:\>Get-SearchIndex

Name                             IndexingState   IsRebuilding    IsSharded
----                             -------------   ------------    ---------
sitecore_analytics_index         Started         False           False
sitecore_core_index              Started         False           False
sitecore_master_index            Started         True            False
sitecore_web_index               Started         False           False
sitecore_marketing_asset_inde... Started         False           False
sitecore_marketing_asset_inde... Started         False           False
sitecore_testing_index           Started         False           False
sitecore_suggested_test_index    Started         False           False
sitecore_fxm_master_index        Started         False           False
sitecore_fxm_web_index           Started         False           False
sitecore_list_index              Started         False           False
social_messages_master           Started         False           False
social_messages_web              Started         False           False 
 
``` 
 
### EXAMPLE 2 
 
 
 
```powershell   
 
The following lists only the specified index.

PS master:\>Get-SearchIndex -Name sitecore_master_index

Name                             IndexingState   IsRebuilding    IsSharded
----                             -------------   ------------    ---------
sitecore_master_index            Started         True            False 
 
``` 
 
## Related Topics 
 
* Initialize-SearchIndex* Stop-SearchIndex* Resume-SearchIndex* Suspend-SearchIndex* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>
