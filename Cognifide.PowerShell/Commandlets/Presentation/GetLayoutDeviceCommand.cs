using System.Linq;
using System.Management.Automation;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.Get, "LayoutDevice")]
    [OutputType(typeof (DeviceItem), ParameterSetName = new[] {"By Name", "Default"})]
    public class GetLayoutDeviceCommand : BaseCommand
    {
        [Parameter(Position = 0, ParameterSetName = "By Name", Mandatory = true)]
        public string Name { get; set; }

        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Default")]
        public SwitchParameter Default { get; set; }

        protected override void ProcessRecord()
        {
            if (Default)
            {
                WriteObject(CurrentDatabase.Resources.Devices.GetAll().FirstOrDefault(d => d.IsDefault));
            }
            else
            {
                if (string.IsNullOrEmpty(Name))
                {
                    Name = "*";
                }
                WildcardWrite(Name, CurrentDatabase.Resources.Devices.GetAll(), device => device.Name);
            }
        }
    }
}