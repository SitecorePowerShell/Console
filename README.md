# Sitecore PowerShell Extensions: a CLI and scripting tool

![Sitecore PowerShell Extensions](https://raw.githubusercontent.com/SitecorePowerShell/Console/master/readme-console-ise.png)

```diff
- If you are using version 4.2 or older in your environments, please update them to 4.3 ASAP
- Please be mindful that we recommend that you DO NOT install it on Content Delivery servers
- or run it in setups that face the Internet in an unprotected connections 
- (e.g. outside of a VPN protected environment)
```

The Sitecore PowerShell Extensions module (SPE) provides a robust environment for automating tasks within Sitecore.

![Sitecore PowerShell Extensions](https://raw.githubusercontent.com/SitecorePowerShell/Console/master/readme-ise.gif)

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

* `gci` = `Get-ChildItem`
* `ri` = `Remove-Item`
* `rni` = `Rename-Item`

If you can answer yes to any of those (and more), you will certainly find this module a powerful and necessary tool.

The idea behind the project is to create a scripting environment to work within Sitecore on a granular level to allow you to apply complex modifications and manipulate not just sites, but files and pages on a large scale or perform statistical analysis of your content using a familiar and well documented query language Windows PowerShell.

If you have any questions, comments, or suggegstions with the SPE module, please report them in the Issue Tracker. We'll also gladly respond to any of your questions on Sitecore Shared Source Modules Forum or in the Project Discussion Pages.

Enjoy!

[Adam Najmanowicz](http://blog.najmanowicz.com/) - - Founder, Architect & Lead Developer

[Michael West](http://michaellwest.blogspot.com) - Developer & Documentation Lead

[Mike Reynolds](http://sitecorejunkie.com/) - SPE Evangelist


[![Adam Najmanowicz](https://avatars2.githubusercontent.com/u/1209953?v=3&s=144)](https://github.com/AdamNaj) | [![Michael West](https://gravatar.com/avatar/a2914bafbdf4e967701eb4732bde01c5?s=144)](https://github.com/michaellwest) | [![Mike Reynolds](https://gravatar.com/avatar/cb60f2c25deefe0f05b4157cc638fad5?s=144)](https://github.com/scjunkie)
---|---|---
[Adam Najmanowicz](https://blog.najmanowicz.com) | [Michael West](https://michaellwest.blogspot.com) | [Mike Reynolds](https://sitecorejunkie.com)

---

### Resources

Download the module from the [Sitecore Marketplace](http://marketplace.sitecore.net/en/Modules/Sitecore_PowerShell_console.aspx).

Read the [SPE user guide](http://sitecorepowershell.gitbooks.io/sitecore-powershell-extensions/).

See a whole [variety of links to SPE material](http://blog.najmanowicz.com/sitecore-powershell-console/).

Watch some quick start [training videos](http://www.youtube.com/playlist?list=PLph7ZchYd_nCypVZSNkudGwPFRqf1na0b).

A big "Thank you!" to [Cognifide](http://www.cognifide.com/) for letting Adam spent some of his working time working on the module!
