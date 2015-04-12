# Get-Rendering 
 
Returns RenderingDefinition from an item based on provided filtering parameters. 
 
## Syntax 
 
Get-Rendering -Item &lt;Item&gt; -UniqueId &lt;String&gt; [-Device &lt;DeviceItem&gt;] 
 
Get-Rendering -Item &lt;Item&gt; [-DataSource &lt;String&gt;] [-Rendering &lt;Item&gt;] [-Index &lt;Int32&gt;] [-PlaceHolder &lt;String&gt;] [-Parameter &lt;Hashtable&gt;] [-Device &lt;DeviceItem&gt;] 
 
Get-Rendering -Item &lt;Item&gt; -Instance &lt;RenderingDefinition&gt; [-Device &lt;DeviceItem&gt;] 
 
Get-Rendering -Path &lt;String&gt; [-DataSource &lt;String&gt;] [-Rendering &lt;Item&gt;] [-Index &lt;Int32&gt;] [-PlaceHolder &lt;String&gt;] [-Parameter &lt;Hashtable&gt;] [-Device &lt;DeviceItem&gt;] 
 
Get-Rendering -Path &lt;String&gt; -UniqueId &lt;String&gt; [-Device &lt;DeviceItem&gt;] 
 
Get-Rendering -Path &lt;String&gt; -Instance &lt;RenderingDefinition&gt; [-Device &lt;DeviceItem&gt;] 
 
Get-Rendering [-Id &lt;String&gt;] [-Database &lt;Database&gt;] -UniqueId &lt;String&gt; [-Device &lt;DeviceItem&gt;] 
 
Get-Rendering [-Id &lt;String&gt;] [-Database &lt;Database&gt;] [-DataSource &lt;String&gt;] [-Rendering &lt;Item&gt;] [-Index &lt;Int32&gt;] [-PlaceHolder &lt;String&gt;] [-Parameter &lt;Hashtable&gt;] [-Device &lt;DeviceItem&gt;] 
 
Get-Rendering [-Id &lt;String&gt;] [-Database &lt;Database&gt;] -Instance &lt;RenderingDefinition&gt; [-Device &lt;DeviceItem&gt;] 
 
 
## Detailed Description 
Returns RenderingDefinition from an item based on provided filtering parameters. 
- 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Item&nbsp; &lt;Item&gt; 
 
The item to be processed.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue, ByPropertyName) |
| Accept Wildcard Characters? | false | 
 
### -Path&nbsp; &lt;String&gt; 
 
Path to the item to be processed - can work with Language parameter to narrow the publication scope.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Id&nbsp; &lt;String&gt; 
 
Id of the item to be processed - can work with Language parameter to narrow the publication scope.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
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
 
### -DataSource&nbsp; &lt;String&gt; 
 
Data source filter - supports wildcards.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Rendering&nbsp; &lt;Item&gt; 
 
Item representing the sublayout/rendering. If matching the rendering will be returned.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Index&nbsp; &lt;Int32&gt; 
 
Index at which the rendering exists in the layout. The rendering at that index will be returned.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -PlaceHolder&nbsp; &lt;String&gt; 
 
Place holder at which the rendering exists in the layout. Renderings at that place holder will be returned.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Parameter&nbsp; &lt;Hashtable&gt; 
 
Additional rendering parameter values. If both name and value match - the rendering will be returned. Values support wildcards.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Instance&nbsp; &lt;RenderingDefinition&gt; 
 
Specific instance of rendering that should be returned. The instance could earlier be obtained through e.g. use of Get-Rendering.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -UniqueId&nbsp; &lt;String&gt; 
 
UniqueID of the rendering to be retrieved.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Device&nbsp; &lt;DeviceItem&gt; 
 
Device for which the renderings will be retrieved.
 

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
 
get all renderings for "Default" device, located in the any placeholder that has name in it or any of its sub-placeholders 
 
```powershell   
 
PS master:\> Get-Item master:\content\home | Get-Rendering -Placeholder "*main*" -Device (Get-LayoutDevice "Default") 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/> 
 
* Add-Rendering 
 
* New-Rendering 
 
* Set-Rendering 
 
* Get-LayoutDevice 
 
* Remove-Rendering 
 
* Get-Layout 
 
* Set-Layout

