# Find-Item 
 
Allows to find items using the Sitecore Content Search API. 
 
## Syntax 
 
Find-Item [-Criteria &lt;SearchCriteria[]&gt;] [-Where &lt;String&gt;] [-WhereValues &lt;Object[]&gt;] [-OrderBy &lt;String&gt;] [-First &lt;Int32&gt;] [-Last &lt;Int32&gt;] [-Skip &lt;Int32&gt;] 
 
 
## Detailed Description 
Allows to find items using the Sitecore Content Search API. 
- 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
### -Criteria&nbsp; &lt;SearchCriteria[]&gt; 
 
simple search criteria in the following example form:

@{
    Filter = "Equals"
    Field = "_templatename"
    Value = "PowerShell Script"
}, 
@{
    Filter = "StartsWith"
    Field = "_fullpath"
    Value = "/sitecore/system/Modules/PowerShell/Script Library/System Maintenance"
}

Where "Filter" is one of the following values:
- Equals
- StartsWith,
- Contains,
- EndsWith

Fields by which you can filter can be discovered using the following script:

Find-Item -Index sitecore_master_index `
          -Criteria @{Filter = "StartsWith"; Field = "_fullpath"; Value = "/sitecore/content/" } `
          -First 1 | 
    select -expand "Fields"
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Where&nbsp; &lt;String&gt; 
 
Works on Sitecore 7.5 and later versions only.

Filtering Criteria using Dynamic Linq syntax: http://weblogs.asp.net/scottgu/dynamic-linq-part-1-using-the-linq-dynamic-query-library
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -WhereValues&nbsp; &lt;Object[]&gt; 
 
Works on Sitecore 7.5 and later versions only.

An Array of objects for Dynamic Linq "-Where" parameter as explained in: http://weblogs.asp.net/scottgu/dynamic-linq-part-1-using-the-linq-dynamic-query-library
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -OrderBy&nbsp; &lt;String&gt; 
 
Works on Sitecore 7.5 and later versions only.

Field by which the search results sorting should be performed. 
Dynamic Linq ordering syntax used.
http://weblogs.asp.net/scottgu/dynamic-linq-part-1-using-the-linq-dynamic-query-library
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -First&nbsp; &lt;Int32&gt; 
 
Number of returned search results.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Last&nbsp; &lt;Int32&gt; 
 

 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
### -Skip&nbsp; &lt;Int32&gt; 
 
Number of search results to be skipped skip before returning the results commences.
 

| | |
| - | - |
| Aliases |  |
| Required? | false |
| Position? | named |
| Default Value |  |
| Accept Pipeline Input? | false |
| Accept Wildcard Characters? | false | 
 
## Notes 
 
Help Author: Adam Najmanowicz, Michael West 
 
## Examples 
 
### EXAMPLE 1 
 
Fields by which filtering can be performed using the -Criteria parameter 
 
```powershell   
 
Find-Item -Index sitecore_master_index `
          -Criteria @{Filter = "StartsWith"; Field = "_fullpath"; Value = "/sitecore/content/" } `
          -First 1 | 
    select -expand "Fields" 
 
``` 
 
### EXAMPLE 2 
 
Find all Template Fields using Dynamic LINQ syntax 
 
```powershell   
 
Find-Item `
    -Index sitecore_master_index `
    -Where 'TemplateName = @0 And Language=@1' `
    -WhereValues "Template Field", "en" 
 
``` 
 
### EXAMPLE 3 
 
Find all Template Fields using the -Criteria parameter syntax 
 
```powershell   
 
Find-Item `
        -Index sitecore_master_index `
        -Criteria @{Filter = "Equals"; Field = "_templatename"; Value = "Template Field"},
                  @{Filter = "Equals"; Field = "_language"; Value = "en"} 
 
``` 
 
## Related Topics 
 
* Initialize-Item 
 
* Get-Item 
 
* Get-ChildItem 
 
* <a href='https://gist.github.com/AdamNaj/273458beb3f2b179a0b6' target='_blank'>https://gist.github.com/AdamNaj/273458beb3f2b179a0b6</a><br/> 
 
* <a href='http://weblogs.asp.net/scottgu/dynamic-linq-part-1-using-the-linq-dynamic-query-library' target='_blank'>http://weblogs.asp.net/scottgu/dynamic-linq-part-1-using-the-linq-dynamic-query-library</a><br/> 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>

