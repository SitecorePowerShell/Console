using System.Management.Automation;
using Sitecore.Data.Items;
using Sitecore.Exceptions;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Governance
{
    [Cmdlet(VerbsCommon.Unlock, "Item")]
    [OutputType(new[] {typeof (bool)}, ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"}
        )]
    public class UnlockItemCommand : GovernanceUserBaseCommand
    {
        protected override void ProcessRecord()
        {
            Item sourceItem = GetProcessedRecord();
            ProcessItem(sourceItem);
        }

        private void ProcessItem(Item item)
        {
            LockItem(item);
            if (Recurse)
            {
                foreach (Item child in item.Children)
                {
                    ProcessItem(child);
                }
            }
        }

        private void LockItem(Item item)
        {
            SwitchUser(() =>
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
            });
        }
    }
}