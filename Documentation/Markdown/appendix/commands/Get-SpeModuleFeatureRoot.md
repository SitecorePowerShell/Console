# Get-SpeModuleFeatureRoot 
 
Returns the library item or path to the library where scripts for a particular integration point should be located for a specific module. 
 
## Syntax 
 
Get-SpeModuleFeatureRoot [-Module &lt;Module&gt;] [-ReturnPath] [-Feature] &lt;String&gt; 
 
 
## Detailed Description 
 
The Get-SpeModuleFeatureRoot command returns library item or path to the library where scripts for a particular integration point should be located for a specific module. 
 
© 2010-2017 Adam Najmanowicz, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Module&nbsp; &lt;Module&gt; 
 
Module for which the feature root library should be returned. 
If not provided the feature root will be returned for all modules. 
 
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
            <td>true (ByValue, ByPropertyName)</td>
        </tr>
        <tr>
            <td>Accept Wildcard Characters?</td>
            <td>false</td>
        </tr>
    </tbody>
</table> 
 
### -ReturnPath&nbsp; &lt;SwitchParameter&gt; 
 
 
 
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
 
### -Feature&nbsp; &lt;String&gt; 
 
Feature for which the root library should be provided. 
If root item does not exist and -ReturnPath parameter is not specified - nothing will be returned, 
If -ReturnPath parameter is provided the path in which the feature root should be located will be returned

Valid features:
- contentEditorContextMenu 
- contentEditorGutters
- contentEditorRibbon
- controlPanel
- functions
- listViewExport
- listViewRibbon
- pipelineLoggedIn
- pipelineLoggingIn
- pipelineLogout
- toolbox
- startMenuReports
- eventHandlers
- webAPI
- pageEditorNotification
- isePlugi 
 
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
            <td>1</td>
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
 
* Sitecore.Data.Items.Item
System.String 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 1 
 
Return the library item for "Content Editor Context Menu" 
 
```powershell   
 
$module = Get-SpeModule -Name "Copy Renderings"
Get-SpeModuleFeatureRoot -Feature contentEditorContextMenu -Module $module 
 
``` 
 
### EXAMPLE 2 
 
Return the Path to where "List View Export" scripts would be located if this feature was defined 
 
```powershell   
 
$module = Get-SpeModule -Name "Copy Renderings"
Get-SpeModuleFeatureRoot -Module $module -Feature listViewExport -ReturnPath 
 
``` 
 
## Related Topics 
 
* [Get-SpeModule](/appendix/commands/Get-SpeModule.md)* <a href='http://blog.najmanowicz.com/2014/11/01/sitecore-powershell-extensions-3-0-modules-proposal/' target='_blank'>http://blog.najmanowicz.com/2014/11/01/sitecore-powershell-extensions-3-0-modules-proposal/</a><br/>* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>
