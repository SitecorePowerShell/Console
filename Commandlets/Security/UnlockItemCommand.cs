using System.Management.Automation;
using Cognifide.PowerShell.Extensions;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Security
{
    [Cmdlet(VerbsCommon.Unlock, "Item")]
    [OutputType(new[] {typeof (bool)}, ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"})]
    public class UnlockItemCommand : BaseGovernanceCommand
    {
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessItemInUserContext(Item item)
        {
            if (!this.CanWrite(item)) { return; }
            if (!this.CanChangeLock(item)) { return; }

            item.Locking.Unlock();

            if (PassThru) { WriteObject(item); }
        }
    }
}