# Import-Function 
 
Imports a function script from the script library's "Functions" folder. 
 
## Syntax 
 
 
## Detailed Description 
 
Imports a function script from the script library's "Functions" folder. 
 
- 
 
© 2011-2015 Adam Najmanowicz - Cognifide Limited, Michael West. All rights reserved. Sitecore PowerShell Extensions 
 
## Parameters 
 
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
 
The following imports a Resolve-Error function that you may later use to get a deeper understanding of a problem with script should one occur by xecuting the "Resolve-Error" commandlet 
that was imported as a result of the execution of the following line 
 
```powershell   
 
PS master:\> Import-Function -Name Resolve-Error 
 
``` 
 
## Related Topics 
 
* Invoke-Script 
 
* <a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>https://github.com/SitecorePowerShell/Console/</a><br/>

