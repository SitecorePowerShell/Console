# New-Rendering 
 
Creates new rendering definition that can later be added to an item. 
 
## Syntax 
 
New-Rendering [-Item] &lt;Item&gt; [-Parameter &lt;Hashtable&gt;] [-PlaceHolder &lt;String&gt;] [-DataSource &lt;Item&gt;] [-Cacheable] [-VaryByData] [-VaryByDevice] [-VaryByLogin] [-VaryByParameters] [-VaryByQueryString] [-VaryByUser] 
 
New-Rendering [-Path] &lt;String&gt; [-Parameter &lt;Hashtable&gt;] [-PlaceHolder &lt;String&gt;] [-DataSource &lt;Item&gt;] [-Cacheable] [-VaryByData] [-VaryByDevice] [-VaryByLogin] [-VaryByParameters] [-VaryByQueryString] [-VaryByUser] 
 
New-Rendering -Id &lt;String&gt; [-Database &lt;Database&gt;] [-Parameter &lt;Hashtable&gt;] [-PlaceHolder &lt;String&gt;] [-DataSource &lt;Item&gt;] [-Cacheable] [-VaryByData] [-VaryByDevice] [-VaryByLogin] [-VaryByParameters] [-VaryByQueryString] [-VaryByUser] 
 
 
## Detailed Description 
 
Creates new rendering definition that can later be added to an item. Most parameters can later be overriden when calling Add-Rendering. 
 
- 
 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Parameter&nbsp; &lt;Hashtable&gt; 
 
Rendering parameters as hashtable
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -PlaceHolder&nbsp; &lt;String&gt; 
 
Placeholder for the rendering to be placed into.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -DataSource&nbsp; &lt;Item&gt; 
 
Datasource for the rendering.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Cacheable&nbsp; &lt;SwitchParameter&gt; 
 
Defined whether the rendering is cacheable.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -VaryByData&nbsp; &lt;SwitchParameter&gt; 
 
Defines whether a data-specific cache version of the rendering should be kept.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -VaryByDevice&nbsp; &lt;SwitchParameter&gt; 
 
Defines whether a device-specific cache version of the rendering should be kept.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -VaryByLogin&nbsp; &lt;SwitchParameter&gt; 
 
Defines whether a login - specific cache version of the rendering should be kept.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -VaryByParameters&nbsp; &lt;SwitchParameter&gt; 
 
Defines whether paremeter - specific cache version of the rendering should be kept.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -VaryByQueryString&nbsp; &lt;SwitchParameter&gt; 
 
Defines whether query string - specific cache version of the rendering should be kept.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -VaryByUser&nbsp; &lt;SwitchParameter&gt; 
 
Defines whether a user - specific cache version of the rendering should be kept.
 

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
 
* Add-Rendering 
 
* Set-Rendering 
 
* Get-Rendering 
 
* Get-LayoutDevice 
 
* Remove-Rendering 
 
* Get-Layout 
 
* Set-Layout

