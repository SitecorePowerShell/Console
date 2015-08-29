using System;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Items;
using Sitecore.Exceptions;

namespace Cognifide.PowerShell.Commandlets.Security.Items
{
    [Cmdlet(VerbsCommon.Lock, "Item", SupportsShouldProcess = true)]
    [OutputType(typeof (bool), ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"})]
    public class LockItemCommand : BaseEditItemCommand
    {

        // remove from parameters list
        public override string[] Language { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override void EditItem(Item item)
        {
            if (!ShouldProcess(item.GetProviderPath(), "Lock Item")) return;

            var itemLock = item.Locking;
            if (itemLock.GetOwner().Is(Identity.Name))
            {
                return;
            }
            if (itemLock.IsLocked())
            {
                if (Force)
                {
                    itemLock.Unlock();
                }
                else
                {
                    WriteError(typeof(SecurityException), $"Cannot modify item '{item.Name}' because it is locked by '{item.Locking.GetOwner()}' - Use the -Force parameter to transfet lock to the new user.", 
                        ErrorIds.InsufficientSecurityRights, ErrorCategory.InvalidData,item);
                    return;
                }
            }

            item.Locking.Lock();
        }
    }
}