using System;
using System.Management.Automation;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Security.Accounts;
using Sitecore.SecurityModel;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Governance
{
    public abstract class GovernanceUserBaseCommand : GovernanceBaseCommand
    {
        [Parameter]
        public User User { get; set; }

        protected override void BeginProcessing()
        {
            if (User == null)
            {
                User = Context.User;
            }
        }

        protected override void ProcessRecord()
        {
            Item sourceItem = GetProcessedRecord();
            ProcessItemRecursive(sourceItem);
        }

        private void ProcessItemRecursive(Item item)
        {
            ProcessItem(item);
            if (Recurse)
            {
                foreach (Item child in item.Children)
                {
                    ProcessItem(child);
                }
            }
        }

        protected abstract void ProcessItem(Item item);

        public void SwitchUser(Action action)
        {
            using (new SecurityStateSwitcher(SecurityState.Enabled))
            {
                if (string.Equals(User.Name, Sitecore.Context.User.Name, StringComparison.OrdinalIgnoreCase))
                {
                    action();
                }
                else
                {
                    using (new UserSwitcher(User))
                    {
                        action();
                    }
                }
            }
        }
    }
}