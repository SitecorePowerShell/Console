using System.Collections.Generic;
using System.Configuration;
using System.Management.Automation;
using System.Xml;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Packages;
using log4net;
using log4net.Config;
using Sitecore.Update;
using Sitecore.Update.Engine;
using Sitecore.Update.Interfaces;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.UpdatePackages
{
    //[Cmdlet(VerbsData.Export, "UpdatePackage")]
    public class ExportUpdatePackageCommand : BasePackageCommand
    {
        private List<ICommand> commands;

        [Parameter(Position = 0)]
        public string Name { get; set; }

        [Parameter(Position = 1, Mandatory = true)]
        public ICommand Command { get; set; }

        [Parameter(Position = 0)]
        public string Path { get; set; }

        [Parameter]
        public string Readme { get; set; }

        [Parameter]
        public string LicenseFileName { get; set; }

        [Parameter]
        public string Tag { get; set; }

        protected override void BeginProcessing()
        {
            commands = new List<ICommand>();
            base.BeginProcessing();
        }

        protected override void EndProcessing()
        {
            // Use default logger
            ILog log = LogManager.GetLogger("root");
            XmlConfigurator.Configure((XmlElement) ConfigurationManager.GetSection("log4net"));

            PerformInstallAction(
                () =>
                {
                    var diff = new DiffInfo(
                        commands,
                        string.IsNullOrEmpty(Name) ? "Sitecore PowerShell Extensions Generated Update Package" : Name,
                        Readme,
                        Tag);

                    var fileName = Path;
                    if (string.IsNullOrEmpty(fileName))
                    {
                        fileName = string.Format("{0}.update",Name);
                    }

                    if (!System.IO.Path.IsPathRooted(fileName))
                    {
                        fileName = FullPackageProjectPath(fileName);
                    }

                    PackageGenerator.GeneratePackage(diff, string.Empty, fileName);
                });
        }

        protected override void ProcessRecord()
        {
            commands.Add(Command);
        }
    }
}