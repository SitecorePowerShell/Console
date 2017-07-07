# Wait-ScriptSession 
 
Suppresses script execution command prompt until one or all of the script sessions provided are complete. 
 
## Syntax 
 
Wait-ScriptSession -Id &lt;String[]&gt; [-Timeout &lt;Int32&gt;] [-Any] 
 
Wait-ScriptSession -Session &lt;ScriptSession[]&gt; [-Timeout &lt;Int32&gt;] [-Any] 
 
 
## Detailed Description 
 
The Wait-ScriptSession cmdlet waits for script session to complete before it displays the command prompt or allows the script to continue. You can wait until any script session is complete, or until all script sessions are complete, and you can set a maximum wait time for the script session.
When the commands in the script session are complete, Wait-ScriptSession displays the command prompt and returns a script session object so that you can pipe it to another command.
You can use Wait-ScriptSession cmdlet to wait for script sessions, such as those that were started by using the Start-ScriptSession cmdlet. 
 
© 2010-2017 Adam Najmanowicz, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Timeout&nbsp; &lt;Int32&gt; 
 
The maximum time to wait for all the other running script sessions to complete. 
 
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
 
### -Any&nbsp; &lt;SwitchParameter&gt; 
 
Returns control to the script or displays the command prompt (and returns the ScriptSession object) when any script session completes. By default, Wait-ScriptSession waits until all of the specified jobs are complete before displaying the prompt. 
 
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
 
### -Id&nbsp; &lt;String[]&gt; 
 
Id(s) of the session to be stopped. 
 
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
 
Session(s) to be stopped. 
 
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
 
PS master:\> Wait-ScriptSession -Id "My Background Script Session" 
 
``` 
 
## Related Topics 
 
* [Get-ScriptSession](/appendix/commands/Get-ScriptSession.md)* [Receive-ScriptSession](/appendix/commands/Receive-ScriptSession.md)* [Remove-ScriptSession](/appendix/commands/Remove-ScriptSession.md)* [Start-ScriptSession](/appendix/commands/Start-ScriptSession.md)* [Stop-ScriptSession](/appendix/commands/Stop-ScriptSession.md)* <a href='http://blog.najmanowicz.com/2014/10/26/sitecore-powershell-extensions-persistent-sessions/' target='_blank'>http://blog.najmanowicz.com/2014/10/26/sitecore-powershell-extensions-persistent-sessions/</a><br/>* <a href='https://git.io/spe' target='_blank'>https://git.io/spe</a><br/>
