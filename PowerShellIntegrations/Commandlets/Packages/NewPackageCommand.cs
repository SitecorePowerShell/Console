using System.Management.Automation;
using Sitecore.Install;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Packages
{
    [Cmdlet("New", "Package", DefaultParameterSetName = "FileName")]
    public class NewPackageCommand : BasePackageCommand
    {
        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            PerformInstallAction(
                () =>
                    {
                        var project = new PackageProject {Name = Name, Metadata = {PackageName = Name}};
                        WriteObject(project, false);
                    });
        }
    }
}