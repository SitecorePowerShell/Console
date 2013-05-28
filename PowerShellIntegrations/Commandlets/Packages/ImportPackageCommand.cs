using System.IO;
using System.Management.Automation;
using Sitecore.Install;
using Sitecore.Install.Files;
using Sitecore.Install.Framework;
using Sitecore.Install.Items;
using Sitecore.Install.Utils;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Packages
{
    [Cmdlet("Import", "Package", DefaultParameterSetName = "ZipFileName")]
    public class ImportPackageCommand : BasePackageCommand
    {
        [Parameter(Position = 0)]
        public string FileName { get; set; }

        [Parameter(Position = 1, ValueFromPipeline = true)]
        public PackageProject Project { get; set; }

        [Parameter]
        public SwitchParameter IncludeProject { get; set; }

        [Parameter]
        public InstallMode InstallMode { get; set; }

        [Parameter]
        public MergeMode MergeMode { get; set; }

        protected override void ProcessRecord()
        {
            string fileName = FileName;

            PerformInstallAction(
                () =>
                    {
                        if (IncludeProject.IsPresent)
                        {
                            Project.SaveProject = true;
                        }

                        if (!Path.IsPathRooted(fileName))
                        {
                            fileName = FullPackagePath(fileName);
                        }

                        IProcessingContext context = new SimpleProcessingContext();
                        IItemInstallerEvents events =
                            new DefaultItemInstallerEvents(new BehaviourOptions(InstallMode, MergeMode));
                        context.AddAspect(events);
                        IFileInstallerEvents events1 = new DefaultFileInstallerEvents(true);
                        context.AddAspect(events1);
                        var installer = new Installer();
                        installer.InstallPackage(fileName, context);
                    });
        }
    }
}