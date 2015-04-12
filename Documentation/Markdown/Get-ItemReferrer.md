# Get-ItemReferrer 
 
Get other items linking to the provided item. 
 
## Syntax 
 
Get-ItemReferrer -Item &lt;Item&gt; 
 
Get-ItemReferrer -Item &lt;Item&gt; -ItemLink 
 
Get-ItemReferrer -Path &lt;String&gt; [-Language &lt;String[]&gt;] -ItemLink 
 
Get-ItemReferrer -Path &lt;String&gt; [-Language &lt;String[]&gt;] 
 
Get-ItemReferrer -Id &lt;String&gt; [-Database &lt;Database&gt;] [-Language &lt;String[]&gt;] -ItemLink 
 
Get-ItemReferrer -Id &lt;String&gt; [-Database &lt;Database&gt;] [-Language &lt;String[]&gt;] 
 
 
## Detailed Description 
 
Returns all items that link to the item specified with the commandlet parameters. if -ItemLink parameter is used the Commandlet will return links rather than items. 
 
- 
 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Item&nbsp; &lt;Item&gt; 
 
The item to be analysed.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | true (ByValue, ByPropertyName) |
| Accept Wildcard Characters? | false | 
 
### -Path&nbsp; &lt;String&gt; 
 
Path to the item to be analysed - can work with Language parameter to narrow the publication scope.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Id&nbsp; &lt;String&gt; 
 
Id of the item to be analysed - can work with Language parameter to narrow the publication scope.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Database&nbsp; &lt;Database&gt; 
 
Database containing the item to be analysed - can work with Language parameter to narrow the publication scope.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Language&nbsp; &lt;String[]&gt; 
 
If you need the item in specific Language you can specify it with this parameter. Globbing/wildcard supported.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -ItemLink&nbsp; &lt;SwitchParameter&gt; 
 
Return ItemLink that define both source and target of a link rather than items linking to the specified item.
 

| | |
| - | - |
| Aliases |  |
| Required? | true |
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
 
 
 
```powershell   
 
PS master:\>Get-ItemReferrer -Path master:\content\home
 
Name                             Children Languages                Id                                     TemplateName
----                             -------- ---------                --                                     ------------
Home                             True     {en, de-DE, es-ES, pt... {110D559F-DEA5-42EA-9C1C-8A5DF7E70EF9} Sample Item
Form                             False    {en, de-DE, es-ES, pt... {6D3B4E7D-FEF8-4110-804A-B56605688830} Webcontrol
news                             True     {en, de-DE, es-ES, pt... {DB894F2F-D53F-4A2D-B58F-957BFAC2C848} Article
learn-about-oms                  False    {en, de-DE, es-ES, pt... {79ECF4DF-9DB7-430F-9BFF-D164978C2333} Link 
 
``` 
 
### EXAMPLE 2 
 
 
 
```powershell   
 
PS master:\>Get-Item master:\content\home | Get-ItemReferrer -ItemLink
 
SourceItemLanguage : en
SourceItemVersion  : 1
TargetItemLanguage :
TargetItemVersion  : 0
SourceDatabaseName : master
SourceFieldID      : {F685964D-02E1-4DB6-A0A2-BFA59F5F9806}
SourceItemID       : {110D559F-DEA5-42EA-9C1C-8A5DF7E70EF9}
TargetDatabaseName : master
TargetItemID       : {110D559F-DEA5-42EA-9C1C-8A5DF7E70EF9}
TargetPath         : /sitecore/content/Home 
 
``` 
 
## Related Topics 
 
* Get-ItemReference 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>

