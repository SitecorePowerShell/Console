using System.Management.Automation;
using Sitecore.Install;
using Sitecore.Install.Files;
using Sitecore.Install.Framework;
using Sitecore.Install.Items;
using Sitecore.Install.Metadata;
using Sitecore.Install.Utils;
using Sitecore.Install.Zip;

namespace Cognifide.PowerShell.Commandlets.Packages
{
    [Cmdlet(VerbsLifecycle.Install, "Package", SupportsShouldProcess = true)]
    public class InstallPackageCommand : BasePackageCommand
    {
        [Parameter(Position = 0)]
        [Alias("FullName", "FileName")]
        public string Path { get; set; }

        [Parameter(HelpMessage = "Undefined, Overwrite, Merge, Skip, SideBySide")]
        public InstallMode InstallMode { get; set; }

        [Parameter(HelpMessage = "Undefined, Clear, Append, Merge")]
        public MergeMode MergeMode { get; set; }

        [Parameter]
        public SwitchParameter DisableIndexing { get; set; }

        protected override void ProcessRecord()
        {
            var fileName = Path;
            PerformInstallAction(() =>
                {
                    if (!System.IO.Path.IsPathRooted(fileName))
                    {
                        fileName = FullPackagePath(fileName);
                    }

                    if (ShouldProcess(fileName, "Install package"))
                    {
                        var indexSetting = Sitecore.Configuration.Settings.Indexing.Enabled;
                        if (DisableIndexing.IsPresent)
                        {
                            Sitecore.Configuration.Settings.Indexing.Enabled = false;
                        }

                        try
                        {
                            IProcessingContext context = new SimpleProcessingContext();
                            IItemInstallerEvents instance1 =new DefaultItemInstallerEvents(new BehaviourOptions(InstallMode, MergeMode));
                            context.AddAspect(instance1);
                            IFileInstallerEvents instance2 = new DefaultFileInstallerEvents(true);
                            context.AddAspect(instance2);
                            var installer = new Installer();
                            installer.InstallPackage(fileName, context);
                            ISource<PackageEntry> source = new PackageReader(fileName);
                            var previewContext = Installer.CreatePreviewContext();
                            var view = new MetadataView(previewContext);
                            var metadataSink = new MetadataSink(view);
                            metadataSink.Initialize(previewContext);
                            source.Populate(metadataSink);
                            installer.ExecutePostStep(view.PostStep, previewContext);
                        }
                        finally
                        {
                            if (DisableIndexing.IsPresent)
                            {
                                Sitecore.Configuration.Settings.Indexing.Enabled = indexSetting;
                            }
                        }
                    }
                });
        }
    }
}