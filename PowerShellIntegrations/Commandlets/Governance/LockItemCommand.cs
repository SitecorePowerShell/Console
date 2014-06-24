using System;
using System.Management.Automation;
using Sitecore.Data.Items;
using Sitecore.Exceptions;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Governance
{
    [Cmdlet(VerbsCommon.Lock, "Item")]
    [OutputType(new[] {typeof (bool)}, ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"})]
    public class LockItemCommand : BaseGovernanceCommand
    {
        [Parameter]
        public SwitchParameter Overwrite { get; set; }

        protected override void ProcessItemInUserContext(Item item)
        {
            if (!item.Access.CanWrite())
            {
                WriteError(new ErrorRecord(new SecurityException("Cannot modify item '" + item.Name +
                                                                 "' because of insufficient privileges."),
                    "cannot_lock_item_privileges", ErrorCategory.PermissionDenied, item));
            }

            if (item.Locking.IsLocked())
            {
                // item already locked by the lock requesting user
                if (string.Equals(item.Locking.GetOwner(), User.Name, StringComparison.InvariantCultureIgnoreCase))
                {
                    WriteObject(true);
                    return;
                }

                // item requested to be re-locked by the user
                if (Overwrite)
                {
                    item.Locking.Unlock();
                    WriteObject(item.Locking.Lock());
                    return;
                }
                WriteError(new ErrorRecord(new InvalidOperationException("Cannot lock item '" + item.Name +
                                                                         "' because it is already locked."),
                    "cannot_lock_item_locked", ErrorCategory.ResourceBusy, item));
            }

            WriteObject(item.Locking.Lock());
        }
    }
}