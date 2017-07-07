# Stop-ScriptSession 
 
Stops executing script session. 
 
## Syntax 
 
Stop-ScriptSession -Id &lt;String[]&gt; 
 
Stop-ScriptSession -Session &lt;ScriptSession[]&gt; 
 
 
## Detailed Description 
 
Aborts the pipeline of a session that is executing. This will stop the session immediately in its next PowerShell command.
Caution! If your script is running a long operation in the .net code rather than in PowerShell - the session will abort after the code has finished and the control was returned to the script. 
 
© 2010-2017 Adam Najmanowicz, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Id&nbsp; &lt;String[]&gt; 
 
Stops the script session with the specified IDs.
The ID is a string that uniquely identifies the script session within the server. You can type one or more IDs (separated by commas). To find the ID of a script session, type "Get-ScriptSession" without parameters. 
 
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
 
Specifies the script session to be stopped. Enter a variable that contains the script session or a command that gets the script session. You can also pipe a script session object to Receive-ScriptSession. 
 
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
 
* System.String or Cognifide.PowerShell.Core.Host.ScriptSessio 
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* Cognifide.PowerShell.Core.Host.ScriptSessio 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 
 
 
 
```powershell   
 
The following stops the script session with the specified Id.

PS master:\> Stop-ScriptSession -Id "My Background Script Session" 
 
``` 
 
## Related Topics 
 
* [Get-ScriptSession](/appendix/commands/Get-ScriptSession.md)* [Receive-ScriptSession](/appendix/commands/Receive-ScriptSession.md)* [Remove-ScriptSession](/appendix/commands/Remove-ScriptSession.md)* [Start-ScriptSession](/appendix/commands/Start-ScriptSession.md)* [Wait-ScriptSession](/appendix/commands/Wait-ScriptSession.md)* <a href='http://blog.najmanowicz.com/2014/10/26/sitecore-powershell-extensions-persistent-sessions/' target='_blank'>http://blog.najmanowicz.com/2014/10/26/sitecore-powershell-extensions-persistent-sessions/</a><br/>* <a href='https://git.io/spe' target='_blank'>https://git.io/spe</a><br/>
