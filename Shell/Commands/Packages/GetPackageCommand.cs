using System.IO;
using System.Management.Automation;
using Sitecore.IO;
using Sitecore.Install;
using Sitecore.Install.Serialization;

namespace Cognifide.PowerShell.Shell.Commands.Packages
{
    [Cmdlet("Get", "Package", DefaultParameterSetName = "FileName")]
    public class GetPackageCommand : BasePackageCommand
    {
        [Parameter(Position = 0)]
        public string FileName { get; set; }

        protected override void ProcessRecord()
        {
            PerformInstallAction(() =>
                {
                    PackageProject packageProject = null;
                    if (FileName != null)
                    {
                        string fileName = FileName;

                        if (!Path.IsPathRooted(FileName))
                        {
                            fileName = FullPackageProjectPath(FileName);
                        }

                        packageProject =
                            IOUtils.LoadObject(
                                FileUtil.ReadFromFile(fileName))
                            as PackageProject;
                    }
                    WriteObject(packageProject, false);
                });
        }
    }
}