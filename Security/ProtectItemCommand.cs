using System.Management.Automation;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Security
{
    [Cmdlet(VerbsSecurity.Protect, "Item")]
    [OutputType(new[] {typeof (Item)}, ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"})]
    public class ProtectItemCommand : BaseEditItemCommand
    {
        protected override void EditItem(Item item)
        {
            item.Appearance.ReadOnly = true;
        }
    }
}