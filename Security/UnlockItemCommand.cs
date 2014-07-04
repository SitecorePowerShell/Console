using System.Management.Automation;
using Sitecore.Data.Items;
using Sitecore.Exceptions;

namespace Cognifide.PowerShell.Security
{
    [Cmdlet(VerbsCommon.Unlock, "Item")]
    [OutputType(new[] {typeof (bool)}, ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"})]
    public class UnlockItemCommand : BaseGovernanceCommand
    {

        protected override void ProcessItemInUserContext(Item item)
        {
            if (item.Locking.IsLocked())
            {
                if (!item.Access.CanWrite())
                {
                    WriteError(new ErrorRecord(
                        new SecurityException(
                            "Cannot modify item '" + item.Name + "' because of insufficient privileges."),
                        "cannot_lock_item_privileges", ErrorCategory.PermissionDenied, item));
                }
                WriteObject(item.Locking.Unlock());
                return;
            }

            WriteObject(true);
        }

    }
}