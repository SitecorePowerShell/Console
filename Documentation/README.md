The process of generating a maml xml file has never been easier. The following instructions list what dependencies are needed and the steps to generate the help.

### Dependencies:

1. In the Libraries directory inside the project folder copy the PowerShell.MamlGenerator.dll. This should be alongside the referenced Sitecore libraries.

### Generating Help:

1. Assuming the following folder structure:
```
+ Console Project Folder
  + Documentation
  |  + GenerateDocumentation.ps1
  |  + Other ps1 help files
  + Libraries
  |  + PowerShell.MamlGenerator.dll
  + bin
    + Debug
      + Cognifide.PowerShell.dll
      + Required Sitecore files are copied here during compilation
```
2. Ensure the Documentation filder has the ps1 documentation files.
3. The ps1 files should be named after the intended command (i.e. Get-Role.ps1) and the content of the file follows the comment based help like you would write for functions. Execute the following command to learn about the format:
```
help about_Comment_Based_Help
```
4. Open a PowerShell console and dot source the GenerateHelp.ps1, then run.
  * PS C:\Users\Michael> . C:\inetpub\wwwroot\Console72\Documentation\GenerateHelp.ps1
