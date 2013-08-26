using System.Collections.Generic;
using System.Configuration;
using System.Management.Automation;
using System.Xml;
using log4net;
using log4net.Config;
using Sitecore.Update;
using Sitecore.Update.Installer;
using Sitecore.Update.Installer.Utils;
using Sitecore.Update.Metadata;
using Sitecore.Update.Utils;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Packages
{
    [Cmdlet("Install", "UpdatePackage", DefaultParameterSetName = "ZipFileName")]
    [OutputType(new[] { typeof(ContingencyEntry) })]
    public class InstallUpdatePackageCommand : BasePackageCommand
    {
        [Parameter(Position = 0, Mandatory = true)]
        [Alias("FullName", "FileName")]
        public string Path { get; set; }

        [Parameter(Position = 0)]
        public string RollbackPackagePath { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "Preview / Upgrade")]
        public UpgradeAction UpgradeAction { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "Install / Update")]
        public InstallMode InstallMode { get; set; }

        protected override void ProcessRecord()
        {
            // Use default logger
            ILog log = LogManager.GetLogger("root");
            XmlConfigurator.Configure((XmlElement) ConfigurationManager.GetSection("log4net"));

            PerformInstallAction(
                () =>
                {
                    var installer = new DiffInstaller(UpgradeAction);
                    MetadataView view = UpdateHelper.LoadMetadata(Path);

                    bool hasPostAction;
                    string historyPath;
                    List<ContingencyEntry> entries = new List<ContingencyEntry>();
                    entries = installer.InstallPackage(Path, InstallMode, log,entries, out hasPostAction, out historyPath);
                    installer.ExecutePostInstallationInstructions(Path, historyPath, InstallMode, view, log, ref entries);
                    foreach (var entry in entries)
                    {
                        WriteObject(entry);
                    }
                    //UpdateHelper.SaveInstallationMessages(entries, historyPath);
                });
        }
    }
}