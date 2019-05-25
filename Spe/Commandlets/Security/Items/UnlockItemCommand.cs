using System.Management.Automation;
using Sitecore.Data.Items;
using Spe.Core.Utility;

namespace Spe.Commandlets.Security.Items
{
    [Cmdlet(VerbsCommon.Unlock, "Item", SupportsShouldProcess = true)]
    [OutputType(typeof (bool), ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"})]
    public class UnlockItemCommand : BaseEditItemCommand
    {
        // remove from parameters list
        public override AccountIdentity Identity { get; set; }
        public override string[] Language { get; set; }

        protected override void EditItem(Item item)
        {
            if (ShouldProcess(item.GetProviderPath(), "Unlock Item"))
            {
                item.Locking.Unlock();
            }
        }
    }
}