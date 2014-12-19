# How to release Sitecore PowerShell Extensions in 15 simple steps (and counting)

1. In the git issue tracker make sure all closed issues without a milestone have the one that is being released assigned to them.
2. [Close the milestone](https://github.com/SitecorePowerShell/Console/milestones)
3. Take the shortcut to the milestone (closed issues) and assign a Bitly link to it in the following format: http://bit.ly/ScPs##Iss - where ## is the current version number
4. Make sure you've pulled all changes from the repository.
5. Update all changes to items based on the repo changes.
6. Modify the ```Properties\AssemblyInfo.cs``` - to sync with the released version number & rebuild.
7. Modify the ```Internal/PowerShell Extensions Maintenance/Prepare Console Distribution``` to include the new version release date & the bitly link.
8. Run the script to build and download the package.
9. Serialize all changes using the: ```Platform/Development/Internal/PowerShell Extensions Maintenance/Serialize Changes```
10. Put the package in the ```Data\packages``` folder
11. Commit all changes and push to GitHub.
12. [Draft & Publish a GitHub release](https://github.com/SitecorePowerShell/Console/releases)
13. [Upload the package to Marketplace](https://marketplace.sitecore.net/en/Modules/Sitecore_PowerShell_console.aspx)
14. Update the release notes on marketplace. *Documentation* tab *Release notes* section. Including version major changes and link to issues.
15. Oh and don't forget to submit the module for moderation on the Marketplace.

OK.. it's a nightmare...