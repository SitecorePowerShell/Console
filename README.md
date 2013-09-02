Sitecore PowerShell Extensions
=======

The Sitecore PowerShell Extensions module (SPE) provides a robust environment for automating tasks within Sitecore.

Consider some of the following examples of how SPE can improve your quality of life as a Sitecore developer/administrator:
- Make changes to a large number of pages:
```
Get-ChildItem -Path master:\content\home -Recurse | % { $_.Text += "<p>Updated with SPE</p>"  }
```
- Find the oldest page on your site:
```
Get-ChildItem -Path master:\content\home -Recurse | Select-Object -Property Name,Id,"__Updated" | Sort-Object -Property "__Updated"
```
- Remove a file from the Data directory:
```
Get-ChildItem -Path $SitecoreDataFolder\packages -Filter "readme.txt" | Remove-Item
```
- Rename items in the Media Library:
```
Get-ChildItem -Path "master:\media library\Images" | % { Rename-Item -Path $_.ItemPath -NewName ($_.Name + "-old") }
```

If you can answer yes to any of those (and more), you will certainly find this module a powerful and necessary tool.

The idea behind the project is to create a scripting environment to work within Sitecore on a granular level to allow you to apply complex modifications and manipulate not just sites, but files and pages on a large scale or perform statistical analysis of your content using a familiar and well documented query language Windows PowerShell.

If you have any questions, comments, or suggegstions with the SPE module, please report them in the Issue Tracker. We'll also gladly respond to any of your questions on Sitecore Shared Source Modules Forum or in the Project Discussion Pages.

Enjoy!

[Adam Najmanowicz](http://blog.najmanowicz.com/) - [Cognifide](http://www.cognifide.com/)

[Michael West] (http://michaellwest.blogspot.com)


If you simply want to download the module go to The Sitecore Marketplace and download the latest from the 
[Sitecore PowerShell Extensions Page](http://marketplace.sitecore.net/en/Modules/Sitecore_PowerShell_console.aspx)
