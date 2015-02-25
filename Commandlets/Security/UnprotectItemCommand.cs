using System.Management.Automation;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Security
{
    [Cmdlet(VerbsSecurity.Unprotect, "Item", SupportsShouldProcess = true)]
    [OutputType(typeof (Item), ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"})]
    public class UnprotectItemCommand : BaseEditItemCommand
    {
        protected override void EditItem(Item item)
        {
            if (ShouldProcess(item.GetProviderPath(), "Unprotect item"))
            {
                item.Appearance.ReadOnly = false;
            }
        }
    }
}