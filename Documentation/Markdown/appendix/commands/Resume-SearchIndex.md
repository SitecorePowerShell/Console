# Resume-SearchIndex 
 
Resumes the suspended (paused) Sitecore index. 
 
## Syntax 
 
Resume-SearchIndex -Index &lt;ISearchIndex&gt; 
 
Resume-SearchIndex [-Name &lt;String&gt;] 
 
Resume-SearchIndex [-Name &lt;String&gt;] 
 
 
## Detailed Description 
 
The Resume-SearchIndex command resumes the suspended (paused) Sitecore index. 
 
© 2010-2017 Adam Najmanowicz, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Index&nbsp; &lt;ISearchIndex&gt; 
 
 
 
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
            <td>named</td>
        </tr>
        <tr>
            <td>Default Value</td>
            <td></td>
        </tr>
        <tr>
            <td>Accept Pipeline Input?</td>
            <td>false</td>
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
 
* [Initialize-SearchIndex](/appendix/commands/Initialize-SearchIndex.md)* [Suspend-SearchIndex](/appendix/commands/Suspend-SearchIndex.md)* [Stop-SearchIndex](/appendix/commands/Stop-SearchIndex.md)* [Get-SearchIndex](/appendix/commands/Get-SearchIndex.md)* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>
