# Start-ScriptSession 
 
Starts a new Script Session and executes a script provided in it. 
 
## Syntax 
 
Start-ScriptSession -Id &lt;String[]&gt; -Item &lt;Item&gt; [-JobName &lt;String&gt;] [-ArgumentList &lt;Hashtable&gt;] [-Identity &lt;AccountIdentity&gt;] [-DisableSecurity] [-AutoDispose] [-Interactive] [-ContextItem &lt;Item&gt;] 
 
Start-ScriptSession -Id &lt;String[]&gt; -ScriptBlock &lt;ScriptBlock&gt; [-JobName &lt;String&gt;] [-ArgumentList &lt;Hashtable&gt;] [-Identity &lt;AccountIdentity&gt;] [-DisableSecurity] [-AutoDispose] [-Interactive] [-ContextItem &lt;Item&gt;] 
 
Start-ScriptSession -Id &lt;String[]&gt; -Path &lt;String&gt; [-JobName &lt;String&gt;] [-ArgumentList &lt;Hashtable&gt;] [-Identity &lt;AccountIdentity&gt;] [-DisableSecurity] [-AutoDispose] [-Interactive] [-ContextItem &lt;Item&gt;] 
 
Start-ScriptSession -Session &lt;ScriptSession[]&gt; -Item &lt;Item&gt; [-JobName &lt;String&gt;] [-ArgumentList &lt;Hashtable&gt;] [-Identity &lt;AccountIdentity&gt;] [-DisableSecurity] [-AutoDispose] [-Interactive] [-ContextItem &lt;Item&gt;] 
 
Start-ScriptSession -Session &lt;ScriptSession[]&gt; -ScriptBlock &lt;ScriptBlock&gt; [-JobName &lt;String&gt;] [-ArgumentList &lt;Hashtable&gt;] [-Identity &lt;AccountIdentity&gt;] [-DisableSecurity] [-AutoDispose] [-Interactive] [-ContextItem &lt;Item&gt;] 
 
Start-ScriptSession -Session &lt;ScriptSession[]&gt; -Path &lt;String&gt; [-JobName &lt;String&gt;] [-ArgumentList &lt;Hashtable&gt;] [-Identity &lt;AccountIdentity&gt;] [-DisableSecurity] [-AutoDispose] [-Interactive] [-ContextItem &lt;Item&gt;] 
 
Start-ScriptSession -Item &lt;Item&gt; [-JobName &lt;String&gt;] [-ArgumentList &lt;Hashtable&gt;] [-Identity &lt;AccountIdentity&gt;] [-DisableSecurity] [-AutoDispose] [-Interactive] [-ContextItem &lt;Item&gt;] 
 
Start-ScriptSession -Path &lt;String&gt; [-JobName &lt;String&gt;] [-ArgumentList &lt;Hashtable&gt;] [-Identity &lt;AccountIdentity&gt;] [-DisableSecurity] [-AutoDispose] [-Interactive] [-ContextItem &lt;Item&gt;] 
 
Start-ScriptSession -ScriptBlock &lt;ScriptBlock&gt; [-JobName &lt;String&gt;] [-ArgumentList &lt;Hashtable&gt;] [-Identity &lt;AccountIdentity&gt;] [-DisableSecurity] [-AutoDispose] [-Interactive] [-ContextItem &lt;Item&gt;] 
 
 
## Detailed Description 
 
Starts a new Script Session and executes a script provided in it. 
The session can be a background session or if the caller session is interactive providint the -Interactive switch can open a Windowd for the new sessio 
 
© 2010-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Id&nbsp; &lt;String[]&gt; 
 
Id of the session to be created or retrieved. If the session with the same ID exists - it will be used, unless it's busy - in which case an error will be raised.
If a session with the Id provided does not exist - it will be created.
The Id is a string that uniquely identifies the script session within the server. You can type one or more IDs (separated by commas). To find the ID of a script session, type "Get-ScriptSession" without parameters. 
 
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
            <td>false</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
### -Session&nbsp; &lt;ScriptSession[]&gt; 
 
Specifies the script session in context of which the script should be executed. Enter a variable that contains the script session or a command that gets the script session. You can also pipe a script session object to Start-ScriptSession.
If the session is busy at the moment of the call - an error will be raised instead of running the script. 
 
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
            <td>false</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
### -Item&nbsp; &lt;Item&gt; 
 
Script item containing the code to be executed. 
 
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
 
### -Path&nbsp; &lt;String&gt; 
 
Path to the script item containing the code to be executed. 
 
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
            <td>false</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
### -ScriptBlock&nbsp; &lt;ScriptBlock&gt; 
 
Script to be executed. 
 
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
            <td>false</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
### -JobName&nbsp; &lt;String&gt; 
 
Name of the Sitecore job that will run the script session. This can be used to monitor the session progress. 
 
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
 
### -ArgumentList&nbsp; &lt;Hashtable&gt; 
 
Hashtable with the additional parameters required by the invoked script. The parameters will be instantiated in the session as variables. 
 
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
 
### -Identity&nbsp; &lt;AccountIdentity&gt; 
 
User name including domain in context of which the script will be executed. If no domain is specified - 'sitecore' will be used as the default value. 
If user is not specified the current user will be the impersonation context. 
 
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
 
### -DisableSecurity&nbsp; &lt;SwitchParameter&gt; 
 
Add this parameter to disable security in the Job running the script session. 
 
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
 
### -AutoDispose&nbsp; &lt;SwitchParameter&gt; 
 
Providing this parameter will cause the session to be automatically destroyed after it has executed. 
Use this parameter if you're not in need of the results of the script execution. 
 
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
 
### -Interactive&nbsp; &lt;SwitchParameter&gt; 
 
If the new session is run from an interactive session (e.g. from desktop, menu item, console or ISE) using this parameter will cause dialog to be shown to the user to monitor the script progress. 
 
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
 
### -ContextItem&nbsp; &lt;Item&gt; 
 
Context item for the script session. The script will start in the location of the item. 
 
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
 
* Sitecore.Data.Items.Item 
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* Cognifide.PowerShell.Core.Host.ScriptSessio 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 1 
 
 
 
```powershell   
 
The following starts the progress demo script in interactive mode (showing dialogs for each script) in 3 different ways.

In the first case script path is used, second case shows the script item beign retrieved and provided to the cmdlet.
The last case shows the script to be provided as a script block (script content)
Script finishes before the sessions that were launched from it end. 
The sessions will be disposed when user presses the "Close" button in their dialogs as the -AutoDispose parameter was provided.

$scriptPath = "master:\system\Modules\PowerShell\Script Library\Getting Started\Script Testing\Long Running Script with Progress Demo"
$scriptItem = Get-Item $scriptPath
$script = [scriptblock]::Create($scriptItem.Script)
Start-ScriptSession -Path $scriptPath -Interactive -AutoDispose
Start-ScriptSession -Item $scriptItem -Interactive -AutoDispose
Start-ScriptSession -ScriptBlock $script -Interactive -AutoDispose 
 
``` 
 
### EXAMPLE 2 
 
 
 
```powershell   
 
The following starts a script that changes its path to "master:\" and sleeps for 4 seconds. The session will persist in memory as no -AutoDispose parameter has been provided.

Start-ScriptSession -ScriptBlock { cd master:\; Start-Sleep -Seconds 4 } -Id "Background Task" 
 
``` 
 
## Related Topics 
 
* Get-ScriptSession* Receive-ScriptSession* Remove-ScriptSession* Stop-ScriptSession* Wait-ScriptSession* <a href='http://blog.najmanowicz.com/2014/10/26/sitecore-powershell-extensions-persistent-sessions/' target='_blank'>http://blog.najmanowicz.com/2014/10/26/sitecore-powershell-extensions-persistent-sessions/</a><br/>* <a href='https://git.io/spe' target='_blank'>https://git.io/spe</a><br/>
