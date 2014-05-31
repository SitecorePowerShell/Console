using System;
using System.Management.Automation;
using Sitecore.Data.Items;
using Sitecore.Exceptions;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Governance
{
    [Cmdlet(VerbsCommon.Lock, "Item")]
    [OutputType(new[] {typeof (bool)}, ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"}
        )]
    public class LockItemCommand : GovernanceUserBaseCommand
    {
        [Parameter]
        public SwitchParameter Overwrite { get; set; }

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
                if (!item.Access.CanWrite())
                {
                    if (FailSilently)
                    {
                        WriteObject(false);
                        return;
                    }
                    throw new SecurityException("Cannot modify item '" + item.Name +
                                                "' because of insufficient privileges.");
                }

                if (item.Locking.IsLocked())
                {
                    if (Overwrite)
                    {
                        item.Locking.Unlock();
                        WriteObject(item.Locking.Lock());
                        return;
                    }
                    if (FailSilently)
                    {
                        WriteObject(false);
                        return;
                    }
                    throw new InvalidOperationException("Cannot lock item '" + item.Name +
                                                        "' because it is already locked.");
                }

                WriteObject(item.Locking.Lock());
            });
        }
    }
}