using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Management.Automation;
using System.Xml;
using log4net;
using log4net.Config;
using Sitecore.Update;
using Sitecore.Update.Commands;
using Sitecore.Update.Configuration;
using Sitecore.Update.Data;
using Sitecore.Update.Data.Items;
using Sitecore.Update.Engine;
using Sitecore.Update.Interfaces;
using Sitecore.Update.Installer;
using Sitecore.Update.Installer.Utils;
using Sitecore.Update.Metadata;
using Sitecore.Update.Utils;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Packages
{
    //[Cmdlet(VerbsCommon.New, "UpdatePackageCommand")]
    [OutputType(new[] { typeof(ICommand) })]
    public class NewUpdatePackageCommand : BasePackageCommand
    {
        [Parameter]
        public ICommand Command { get; set; }

        [Parameter(Position = 0)]
        public string Path { get; set; }

        [Parameter(Position = 0)]
        public string Name { get; set; }

        [Parameter]
        public string Readme { get; set; }

        [Parameter]
        public string LicenseFileName { get; set; }

        [Parameter]
        public string Tag { get; set; }

        private List<ICommand> commands;

        protected override void BeginProcessing()
        {
        }

        protected override void ProcessRecord()
        {
        }
    }
}