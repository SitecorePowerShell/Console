using System.IO;
using System.Management.Automation;
using Sitecore.IO;
using Sitecore.Install;
using Sitecore.Install.Serialization;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Packages
{
    [Cmdlet("Get", "Package", DefaultParameterSetName = "FileName")]
    [OutputType(new[] { typeof(PackageProject) })]
    public class GetPackageCommand : BasePackageCommand
    {
        [Parameter(Position = 0)]
        [Alias("FullName","FileName")]
        public string Path { get; set; }

        protected override void ProcessRecord()
        {
            PerformInstallAction(() =>
                {
                    PackageProject packageProject = null;
                    if (Path != null)
                    {
                        string fileName = Path;

                        if (!System.IO.Path.IsPathRooted(Path))
                        {
                            fileName = FullPackageProjectPath(Path);
                        }

                        packageProject =
                            IOUtils.LoadObject(
                                FileUtil.ReadFromFile(Path))
                            as PackageProject;
                    }
                    WriteObject(packageProject, false);
                });
        }
    }
}