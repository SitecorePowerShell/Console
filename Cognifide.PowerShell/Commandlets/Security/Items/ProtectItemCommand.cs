using System.Management.Automation;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Security.Items
{
    [Cmdlet(VerbsSecurity.Protect, "Item", SupportsShouldProcess = true)]
    [OutputType(typeof (Item), ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"})]
    public class ProtectItemCommand : BaseEditItemCommand
    {
        // remove from parameters list
        public override AccountIdentity Identity { get; set; }
        public override string[] Language { get; set; }

        protected override void EditItem(Item item)
        {
            if (ShouldProcess(item.GetProviderPath(), "Protect item"))
            {
                item.Appearance.ReadOnly = true;
            }
        }
    }
}