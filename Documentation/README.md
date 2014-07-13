The process of generating a maml xml file has never been easier. The following instructions list what dependencies are needed and the steps to generate the help.

### Dependencies:

1. In the Libraries directory (possibly the same path defined in the deploy.targets) copy the PowerShell.MamlGenerator.dll. This should be alongside the referenced Sitecore libraries.

### Generating Help:

1. Open Documentation\GenerateHelp.ps1 and ensure the $currentDirectory contains the correct path to your instance of Sitecore.
2. Ensure the Documentation has the ps1 documentation files.
3. The ps1 files should be named after the intended command (i.e. Get-Role.ps1) and the content of the file follows the comment based help like you would write for functions.
  * PS C:\Users\Michael> help about_Comment_Based_Help
4. Open a PowerShell console and dot source the GenerateHelp.ps1, then run.
  * PS C:\Users\Michael> . C:\inetpub\wwwroot\Console72\Documentation\GenerateHelp.ps1
