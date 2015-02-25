using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Security
{
    [Cmdlet(VerbsCommon.Lock, "Item", SupportsShouldProcess = true)]
    [OutputType(typeof (bool), ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"} )]
    public class LockItemCommand : BaseEditItemCommand
    {
        protected override void EditItem(Item item)
        {
            if (ShouldProcess(item.GetProviderPath(), "Lock Item"))
            {
                item.Locking.Lock();
            }
        }
    }
}