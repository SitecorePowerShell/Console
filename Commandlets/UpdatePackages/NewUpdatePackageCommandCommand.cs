using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Packages;
using Sitecore.Update.Interfaces;

namespace Cognifide.PowerShell.Commandlets.UpdatePackages
{
    //[Cmdlet(VerbsCommon.New, "UpdatePackageCommand")]
    [OutputType(typeof (ICommand))]
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

        //private List<ICommand> commands;

        protected override void BeginProcessing()
        {
        }

        protected override void ProcessRecord()
        {
        }
    }
}