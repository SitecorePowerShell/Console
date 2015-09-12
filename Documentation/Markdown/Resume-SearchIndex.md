# Resume-SearchIndex 
 
Resumes the suspended (paused) Sitecore index. 
 
## Syntax 
 
Resume-SearchIndex [[-Name] &lt;String&gt;] 
 
 
## Detailed Description 
 
The Resume-SearchIndex command resumes the suspended (paused) Sitecore index. 
 
© 2010-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Name&nbsp; &lt;String&gt; 
 
The name of the index to resume. 
 
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
 
* Sitecore.ContentSearch.ISearchIndex or System.String 
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* None 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 1 
 
 
 
```powershell   
 
The following stops the indexing process from running.

PS master:\> Resume-SearchIndex -Name sitecore_master_index 
 
``` 
 
### EXAMPLE 2 
 
 
 
```powershell   
 
The following stops the indexing process from running.

PS master:\> Get-SearchIndex -Name sitecore_master_index | Resume-SearchIndex 
 
``` 
 
## Related Topics 
 
* Initialize-SearchIndex* Suspend-SearchIndex* Stop-SearchIndex* Get-SearchIndex* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>
