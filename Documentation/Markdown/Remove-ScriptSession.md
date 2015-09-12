# Remove-ScriptSession 
 
Removes a persistent Script Session from memory. 
 
## Syntax 
 
Remove-ScriptSession -Id &lt;String[]&gt; 
 
Remove-ScriptSession -Session &lt;ScriptSession[]&gt; 
 
 
## Detailed Description 
 
Removes a persistent Script Session from memory. 
 
© 2010-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Id&nbsp; &lt;String[]&gt; 
 
Id of the PowerShell session to be removed from memory. 
 
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
 
### -Session&nbsp; &lt;ScriptSession[]&gt; 
 
Session to be removed. 
 
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
 
* Resident Script Session obtained through Get-ScriptSessio* Id of a resident Script Sessio* Cognifide.PowerShell.Core.Host.ScriptSessio* System.String 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 
 
 
 
```powershell   
 
The following removes the script session using the specified Id.

PS master:\> Remove-ScriptSession -Id "Long running script" 
 
``` 
 
## Related Topics 
 
* Get-ScriptSession* Receive-ScriptSession* Start-ScriptSession* Stop-ScriptSession* Wait-ScriptSession* <a href='https://git.io/spe' target='_blank'>https://git.io/spe</a><br/>* <a href='http://blog.najmanowicz.com/2014/10/26/sitecore-powershell-extensions-persistent-sessions/' target='_blank'>http://blog.najmanowicz.com/2014/10/26/sitecore-powershell-extensions-persistent-sessions/</a><br/>
