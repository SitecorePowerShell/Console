# Remove-Rendering 
 
Removes renderings from an item. 
 
## Syntax 
 
Remove-Rendering -Item &lt;Item&gt; -UniqueId &lt;String&gt; [-Device &lt;DeviceItem&gt;] 
 
Remove-Rendering -Item &lt;Item&gt; [-DataSource &lt;String&gt;] [-Rendering &lt;Item&gt;] [-Index &lt;Int32&gt;] [-PlaceHolder &lt;String&gt;] [-Parameter &lt;Hashtable&gt;] [-Device &lt;DeviceItem&gt;] 
 
Remove-Rendering -Item &lt;Item&gt; -Instance &lt;RenderingDefinition&gt; [-Device &lt;DeviceItem&gt;] 
 
Remove-Rendering -Path &lt;String&gt; [-DataSource &lt;String&gt;] [-Rendering &lt;Item&gt;] [-Index &lt;Int32&gt;] [-PlaceHolder &lt;String&gt;] [-Parameter &lt;Hashtable&gt;] [-Device &lt;DeviceItem&gt;] 
 
Remove-Rendering -Path &lt;String&gt; -UniqueId &lt;String&gt; [-Device &lt;DeviceItem&gt;] 
 
Remove-Rendering -Path &lt;String&gt; -Instance &lt;RenderingDefinition&gt; [-Device &lt;DeviceItem&gt;] 
 
Remove-Rendering [-Id &lt;String&gt;] [-Database &lt;Database&gt;] -UniqueId &lt;String&gt; [-Device &lt;DeviceItem&gt;] 
 
Remove-Rendering [-Id &lt;String&gt;] [-Database &lt;Database&gt;] [-DataSource &lt;String&gt;] [-Rendering &lt;Item&gt;] [-Index &lt;Int32&gt;] [-PlaceHolder &lt;String&gt;] [-Parameter &lt;Hashtable&gt;] [-Device &lt;DeviceItem&gt;] 
 
Remove-Rendering [-Id &lt;String&gt;] [-Database &lt;Database&gt;] -Instance &lt;RenderingDefinition&gt; [-Device &lt;DeviceItem&gt;] 
 
 
## Detailed Description 
 
Removes renderings from an item based on a number of qualifying criteria. The search criteria are cumulatice and narrowing the search in an "AND" manner. 
 
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
 
Item representing the sublayout/rendering. If matching the rendering will be removed.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Index&nbsp; &lt;Int32&gt; 
 
Index at which the rendering exists in the layout. The rendering at that index will be removed.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -PlaceHolder&nbsp; &lt;String&gt; 
 
Place holder at which the rendering exists in the layout. Rendering at that placeholder will be removed.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Parameter&nbsp; &lt;Hashtable&gt; 
 
Additional rendering parameter values. If both name and value match - the rendering will be removed. Values support wildcards.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Instance&nbsp; &lt;RenderingDefinition&gt; 
 
Specific instance of rendering that should be removed. The instance coule earlier be obtained through e.g. use of Get-Rendering.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -UniqueId&nbsp; &lt;String&gt; 
 
UniqueID of the rendering to be removed. The instance coule earlier be obtained through e.g. use of OD of rendering retrieved with Get-Rendering.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Device&nbsp; &lt;DeviceItem&gt; 
 
Device for which the rendering should be removed.
 

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
 
### EXAMPLE 1 
 
remove all renderings for "Default" device 
 
```powershell   
 
PS master:\> Remove-Rendering -Path master:\content\home -Device (Get-LayoutDevice "Default") 
 
``` 
 
### EXAMPLE 2 
 
remove all renderings from the "main" placeholder and all of its embedded placeholders. 
 
```powershell   
 
PS master:\> Remove-Rendering -Path master:\content\home -PlaceHolder "main*" 
 
``` 
 
### EXAMPLE 3 
 
remove all renderings from the "main" placeholder and all of its embedded placeholders, but only in the "Default" device 
 
```powershell   
 
PS master:\> Remove-Rendering -Path master:\content\home -PlaceHolder "main*" -Device (Get-LayoutDevice "Default") 
 
``` 
 
## Related Topics 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/> 
 
* Add-Rendering 
 
* New-Rendering 
 
* Set-Rendering 
 
* Get-Rendering 
 
* Get-LayoutDevice 
 
* Get-Layout 
 
* Set-Layout

