using System.Management.Automation;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Security.Items
{
    [Cmdlet(VerbsCommon.Lock, "Item", SupportsShouldProcess = true)]
    [OutputType(typeof (bool), ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"})]
    public class LockItemCommand : BaseEditItemCommand
    {

        // remove from parameters list
        public override string[] Language { get; set; }

        protected override void EditItem(Item item)
        {
            if (ShouldProcess(item.GetProviderPath(), "Lock Item"))
            {
                item.Locking.Lock();
            }
        }
    }
}