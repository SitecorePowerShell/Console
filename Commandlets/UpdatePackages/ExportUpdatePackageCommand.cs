using System.Collections.Generic;
using System.Configuration;
using System.Management.Automation;
using System.Xml;
using Cognifide.PowerShell.Commandlets.Packages;
using log4net;
using log4net.Config;
using Sitecore.Update;
using Sitecore.Update.Engine;
using Sitecore.Update.Interfaces;

namespace Cognifide.PowerShell.Commandlets.UpdatePackages
{
    [Cmdlet(VerbsData.Export, "UpdatePackage")]
    public class ExportUpdatePackageCommand : BasePackageCommand
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public List<ICommand> CommandList { get; set; }

        [Parameter(Position = 1)]
        public string Name { get; set; }

        [Parameter(Position = 2)]
        public string Path { get; set; }

        [Parameter]
        public string Readme { get; set; }

        [Parameter]
        public string LicenseFileName { get; set; }

        [Parameter]
        public string Tag { get; set; }

        protected override void ProcessRecord()
        {
            // Use default logger
            ILog log = LogManager.GetLogger("root");
            XmlConfigurator.Configure((XmlElement) ConfigurationManager.GetSection("log4net"));

            PerformInstallAction(
                () =>
                {
                    var diff = new DiffInfo(
                        CommandList,
                        string.IsNullOrEmpty(Name) ? "Sitecore PowerShell Extensions Generated Update Package" : Name,
                        Readme ?? string.Empty,
                        Tag ?? string.Empty);

                    var fileName = Path;
                    if (string.IsNullOrEmpty(fileName))
                    {
                        fileName = string.Format("{0}.update",Name);
                    }

                    if (!System.IO.Path.IsPathRooted(fileName))
                    {
                        fileName = FullPackageProjectPath(fileName);
                    }

                    PackageGenerator.GeneratePackage(diff, LicenseFileName, fileName);
                });
        }
    }
}