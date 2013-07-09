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
using Sitecore.Update.Engine;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Packages
{
    [Cmdlet("Install", "UpdatePackage", DefaultParameterSetName = "ZipFileName")]
    public class InstallUpdatePackageCommand : BasePackageCommand
    {
        [Parameter(Position = 0)]
        public string FileName { get; set; }

        [Parameter(Mandatory = true)]
        public UpgradeAction UpgradeAction { get; set; }

        [Parameter(Mandatory = true)]
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
                    MetadataView view = UpdateHelper.LoadMetadata(FileName);

                    bool hasPostAction;
                    string historyPath;
                    List<ContingencyEntry> entries = installer.InstallPackage(FileName, InstallMode, log,
                        out hasPostAction, out historyPath);
                    installer.ExecutePostInstallationInstructions(FileName, historyPath, InstallMode, view, log,
                        ref entries);
                    UpdateHelper.SaveInstallationMessages(entries, historyPath);
                });
        }
    }
}