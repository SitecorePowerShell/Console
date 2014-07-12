using System.Management.Automation;
using Cognifide.PowerShell.Extensions;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Security
{
    [Cmdlet(VerbsCommon.Lock, "Item")]
    [OutputType(new[] {typeof (bool)}, ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"} )]
    public class LockItemCommand : BaseGovernanceCommand
    {
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessItemInUserContext(Item item)
        {
            if (!this.CanWrite(item)) { return; }
            if (!this.CanChangeLock(item)) { return; }

            item.Locking.Lock();

            if (PassThru) { WriteObject(item); }
        }
    }
}