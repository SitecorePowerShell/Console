Sitecore PowerShell Extensions
=======

The Sitecore PowerShell Extensions module (SPE) provides a robust environment for automating tasks within Sitecore.

Consider some of the following examples to see how SPE can improve your quality of life as a Sitecore developer/administrator:

- Make changes to a large number of pages:
```powershell
Get-ChildItem -Path master:\content\home -Recurse | % { $_.Text += "<p>Updated with SPE</p>"  }
```

- Find the oldest page on your site:
```powershell
gci master:\content\home -Recurse | select Name,Id,"__Updated" | sort "__Updated"
```

- Remove a file from the Data directory:
```powershell
gci $SitecoreDataFolder\packages -Filter "readme.txt" | ri
```

- Rename items in the Media Library:
```powershell
gci "master:\media library\Images" | % { rni $_.ItemPath -NewName ($_.Name + "-old") }
```

**Note:** Aliases and positional parameters were used in the above examples. Use *Get-Alias* to see them all.

* gci = Get-ChildItem
* ri = Remove-Item
* rni = Rename-Item

If you can answer yes to any of those (and more), you will certainly find this module a powerful and necessary tool.

The idea behind the project is to create a scripting environment to work within Sitecore on a granular level to allow you to apply complex modifications and manipulate not just sites, but files and pages on a large scale or perform statistical analysis of your content using a familiar and well documented query language Windows PowerShell.

If you have any questions, comments, or suggegstions with the SPE module, please report them in the Issue Tracker. We'll also gladly respond to any of your questions on Sitecore Shared Source Modules Forum or in the Project Discussion Pages.

Enjoy!

[Adam Najmanowicz](http://blog.najmanowicz.com/) - [Cognifide](http://www.cognifide.com/) -and SPE Senior Developer

[Michael West](http://michaellwest.blogspot.com) - SPE Developer

[Mike Reynolds](http://sitecorejunkie.com/) - SPE Evangelist

---

### Resources

Download the module from the [Sitecore Marketplace](http://marketplace.sitecore.net/en/Modules/Sitecore_PowerShell_console.aspx).

Read the [SPE user guide](http://sitecorepowershell.gitbooks.io/sitecore-powershell-extensions/).

See a whole [variety of links to SPE material](http://blog.najmanowicz.com/sitecore-powershell-console/).

Watch some quick start [training videos](http://www.youtube.com/playlist?list=PLph7ZchYd_nCypVZSNkudGwPFRqf1na0b).
