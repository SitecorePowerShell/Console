# Sitecore PowerShell Extensions

A command line and scripting tool built for the Sitecore platform.

![Sitecore PowerShell Extensions](docs/readme-console-ise.png)

---

## Notice

> 
* If you are using version 4.2 or older in your environments we recommend you update them to 5.0+ ASAP
* We recommend that you DO NOT install SPE on Content Delivery servers
or run it in setups that face the Internet in an unprotected connections 
(e.g. outside of a VPN protected environment)
* Sitecore versions 7.x and below are no longer supported with the release of SPE 5.0
---

[![License](https://img.shields.io/badge/license-MIT%20License-brightgreen.svg)](https://opensource.org/licenses/MIT)

The Sitecore PowerShell Extensions module (SPE) provides a robust environment for automating tasks within Sitecore.

![Sitecore PowerShell Extensions](docs/readme-ise.gif)

Consider some of the following examples to see how SPE can improve your quality of life as a Sitecore developer/administrator:

- Make changes to a large number of pages:
```powershell
Get-ChildItem -Path master:\content\home -Recurse | 
    ForEach-Object { $_.Text += "<p>Updated with SPE</p>"  }
```

- Find the oldest page on your site:
```powershell
Get-ChildItem -Path master:\content\home -Recurse | 
    Select-Object -Property Name,Id,"__Updated" | 
    Sort-Object -Property "__Updated"
```

- Remove a file from the Data directory:
```powershell
Get-ChildItem -Path $SitecoreDataFolder\packages -Filter "readme.txt" | Remove-Item
```

- Rename items in the Media Library:
```powershell
Get-ChildItem -Path "master:\media library\Images" | 
    ForEach-Object { Rename-Item -Path $_.ItemPath -NewName ($_.Name + "-old") }
```

The idea behind the project is to create a scripting environment to work within Sitecore on a granular level to allow you to apply complex modifications and manipulate not just sites, but files and pages on a large scale or perform statistical analysis of your content using a familiar and well documented query language **Windows PowerShell**.

If you have any questions, comments, or suggegstions with the SPE module, please report them in the Issue Tracker. We'll also gladly respond to any of your questions on Sitecore Shared Source Modules Forum or in the Project Discussion Pages.

Enjoy!

| [![Adam Najmanowicz](https://avatars2.githubusercontent.com/u/1209953?v=3&s=125)](https://github.com/AdamNaj) | [![Michael West](https://gravatar.com/avatar/a2914bafbdf4e967701eb4732bde01c5?s=125)](https://github.com/michaellwest) |
| ---|--- |
| [Adam Najmanowicz](https://blog.najmanowicz.com) | [Michael West](https://michaellwest.blogspot.com) |
| Founder, Architect & Lead Developer | Developer & Documentation Lead |

---

### Resources

* Download from the [Releases page](https://github.com/SitecorePowerShell/Console/releases). Note that the Marketplace site is no longer maintained, and should not be used.
* Read the [SPE user guide](https://doc.sitecorepowershell.com/).
* See a whole [variety of links to SPE material](http://blog.najmanowicz.com/sitecore-powershell-console/).
* Watch some quick start [training videos](http://www.youtube.com/playlist?list=PLph7ZchYd_nCypVZSNkudGwPFRqf1na0b).
