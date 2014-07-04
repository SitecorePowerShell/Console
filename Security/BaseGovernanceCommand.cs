using System;
using System.Management.Automation;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Security.Accounts;
using Sitecore.SecurityModel;

namespace Cognifide.PowerShell.Security
{
    public abstract class BaseGovernanceCommand : BaseItemRecursiveCommand
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

        protected override void ProcessItem(Item item)
        {
            using (new SecurityStateSwitcher(SecurityState.Enabled))
            {
                if (string.Equals(User.Name, Sitecore.Context.User.Name, StringComparison.OrdinalIgnoreCase))
                {
                    ProcessItemInUserContext(item);
                }
                else
                {
                    using (new UserSwitcher(User))
                    {
                        ProcessItemInUserContext(item);
                    }
                }
            }
        }

        protected abstract void ProcessItemInUserContext(Item item);
    }
}