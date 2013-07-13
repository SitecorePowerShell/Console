using System.IO;
using System.Management.Automation;
using Sitecore.IO;
using Sitecore.Install;
using Sitecore.Install.Serialization;
using Sitecore.Install.Zip;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Packages
{
    [Cmdlet("Export", "Package", DefaultParameterSetName = "ZipFileName")]
    public class ExportPackageCommand : BasePackageCommand
    {
        [Parameter(Position = 0)]
        public string Path { get; set; }

        [Parameter(Position = 1, ValueFromPipeline = true)]
        public PackageProject Project { get; set; }

        [Parameter]
        public SwitchParameter Zip { get; set; }

        [Parameter]
        public SwitchParameter IncludeProject { get; set; }

        protected override void ProcessRecord()
        {
            string fileName = Path;

            if (!Zip.IsPresent)
            {
                PerformInstallAction(() =>
                {
                    if (fileName == null)
                    {
                        //name of the zip file when not defined
                        fileName = string.Format(
                            "{0}-{1}.xml", Project.Metadata.PackageName, Project.Metadata.Version);
                    }

                    if (!System.IO.Path.IsPathRooted(fileName))
                    {
                        fileName = FullPackageProjectPath(fileName);
                    }

                    FileUtil.WriteToFile(fileName, IOUtils.StoreObject(Project));
                });
            }
            else
            {
                PerformInstallAction(
                    () =>
                        {
                            if (IncludeProject.IsPresent)
                            {
                                Project.SaveProject = true;
                            }

                            if (fileName == null)
                            {
                                //name of the zip file when not defined
                                fileName = string.Format(
                                    "{0}-PS-{1}.zip", Project.Metadata.PackageName, Project.Metadata.Version);
                            }

                            if (!System.IO.Path.IsPathRooted(fileName))
                            {
                                fileName = FullPackagePath(fileName);
                            }

                            using (var writer = new PackageWriter(fileName))
                            {
                                writer.Initialize(Installer.CreateInstallationContext());
                                PackageGenerator.GeneratePackage(Project, writer);
                            }
                            // WriteObject(Project, false);
                        });
            }
        }
    }
}