# Add-Rendering 
 
Adds Rendering to Item. 
 
## Syntax 
 
Add-Rendering [-Item] &lt;Item&gt; -Instance &lt;RenderingDefinition&gt; [-Parameter &lt;Hashtable&gt;] -PlaceHolder &lt;String&gt; [-DataSource &lt;String&gt;] [-Index &lt;Int32&gt;] [-Device &lt;DeviceItem&gt;] 
 
Add-Rendering [-Path] &lt;String&gt; -Instance &lt;RenderingDefinition&gt; [-Parameter &lt;Hashtable&gt;] -PlaceHolder &lt;String&gt; [-DataSource &lt;String&gt;] [-Index &lt;Int32&gt;] [-Device &lt;DeviceItem&gt;] 
 
Add-Rendering -Id &lt;String&gt; [-Database &lt;Database&gt;] -Instance &lt;RenderingDefinition&gt; [-Parameter &lt;Hashtable&gt;] -PlaceHolder &lt;String&gt; [-DataSource &lt;String&gt;] [-Index &lt;Int32&gt;] [-Device &lt;DeviceItem&gt;] 
 
 
## Detailed Description 
 
Adds rendering to a chosen device for the presentation of an item. 
 
- 
 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Instance&nbsp; &lt;RenderingDefinition&gt; 
 
Rendering definition to be added to the item
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Parameter&nbsp; &lt;Hashtable&gt; 
 
Rendering Parameters to be overriden on the Rendering that is being updated - if not specified the value provided in rendering definition specified in the Instance parameter will be used.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -PlaceHolder&nbsp; &lt;String&gt; 
 
Placeholder path the Rendering should be added to - if not specified the value provided in rendering definition specified in the Instance parameter will be used.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -DataSource&nbsp; &lt;String&gt; 
 
Data source of the Rendering - if not specified the value provided in rendering definition specified in the Instance parameter will be used.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Index&nbsp; &lt;Int32&gt; 
 
Index at which the Rendering should be inserted. If not provided the rendering will be appended at the end of the list.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Device&nbsp; &lt;DeviceItem&gt; 
 
Device the rendering is assigned to. If not specified - default device will be used.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Item&nbsp; &lt;Item&gt; 
 
The item to be processed.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue, ByPropertyName) |
| Accept Wildcard Characters? | false | 
 
### -Path&nbsp; &lt;String&gt; 
 
Path to the item to be processed - can work with Language parameter to narrow the publication scope.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | 1 |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Id&nbsp; &lt;String&gt; 
 
Id of the item to be processed - can work with Language parameter to narrow the publication scope.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Database&nbsp; &lt;Database&gt; 
 
Database containing the item to be processed - can work with Language parameter to narrow the publication scope.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
## Inputs 
 
The input type is the type of the objects that you can pipe to the cmdlet. 
 
* Sitecore.Data.Items.Item 
 
## Outputs 
 
The output type is the type of the objects that the cmdlet emits. 
 
* Sitecore.Data.Items.Item 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 
 
find item defining rendering and create rendering definitio 
 
```powershell   
 
PS master:\> $renderingItem = gi master:\layout\Sublayouts\ZenGarden\Basic\Content | New-Rendering -Placeholder "main"
# find item you want the rendering added to
PS master:\> $item = gi master:\content\Demo\Int\Home
# Add the rendering to the item
PS master:\> Add-Rendering -Item $item -PlaceHolder "main" -Rendering $renderingItem -Parameter @{ FieldName = "Content" } 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/> 
 
* New-Rendering 
 
* Set-Rendering 
 
* Get-Rendering 
 
* Get-LayoutDevice 
 
* Remove-Rendering 
 
* Get-Layout 
 
* Set-Layout

